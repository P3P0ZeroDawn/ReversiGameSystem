using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Chat;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Publishes committed participant messages and moderation outcomes.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public class MatchChatService : IMatchChatService, IDisposable
{
    private readonly ChatApplicationService _applicationService;
    private readonly MatchCallbackSubscriptions<IMatchChatServiceCallback>
        _callbackSubscriptions;

    /// <summary>Creates the durable chat service boundary.</summary>
    /// <param name="applicationService">The internal chat application service.</param>
    /// <param name="logger">The logger used by callback infrastructure.</param>
    public MatchChatService(
        ChatApplicationService applicationService,
        ILogger<MatchChatService> logger)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
        _callbackSubscriptions =
            new MatchCallbackSubscriptions<IMatchChatServiceCallback>(
            (sessionToken, matchId) =>
                _applicationService.EvaluateParticipantAccess(
                    sessionToken,
                    matchId) == ChatStatus.Success,
            logger);
    }

    /// <inheritdoc />
    public void OpenChatChannel(string sessionToken, long matchId)
    {
        ChatChannelAccess access = _applicationService.PrepareChatChannel(
            sessionToken,
            matchId);
        ThrowExpectedFault(access.Status);
        _callbackSubscriptions.Register(sessionToken, matchId);

        if (access.ActiveMute is not null)
        {
            PublishMuteNotification(sessionToken, access.ActiveMute);
        }
    }

    /// <inheritdoc />
    public MatchChatEntry SendChatMessage(
        string sessionToken,
        long matchId,
        string content)
    {
        ChatMessageSubmission submission = _applicationService.SubmitChatMessage(
            sessionToken,
            matchId,
            content);
        if (submission.Status == ChatStatus.PlayerMuted
            && submission.Mute is not null)
        {
            PublishMuteNotification(sessionToken, submission.Mute);
        }

        ThrowExpectedFault(submission.Status, submission.Mute);
        ChatMessage message = submission.Message
            ?? throw new InvalidOperationException(
                "A successful chat submission must contain its message.");
        MatchChatEntry contractMessage = MapChatEntry(message);
        _callbackSubscriptions.Publish(
            matchId,
            (_, callback) => callback.OnChatMessageReceived(contractMessage));
        return contractMessage;
    }

    /// <inheritdoc />
    public MatchChatEntry[] GetChatHistory(string sessionToken, long matchId)
    {
        ChatHistory history = _applicationService.LoadChatHistory(
            sessionToken,
            matchId);
        ThrowExpectedFault(history.Status);

        ChatMessage[] messages = history.Messages;
        MatchChatEntry[] entries = new MatchChatEntry[messages.Length];
        for (int index = 0; index < messages.Length; index++)
        {
            entries[index] = MapChatEntry(messages[index]);
        }

        return entries;
    }

    /// <summary>Removes chat subscriptions for a revoked session.</summary>
    public void RemoveSession(string sessionToken)
    {
        _callbackSubscriptions.RemoveSession(sessionToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _callbackSubscriptions.Dispose();
    }

    private void PublishMuteNotification(string sessionToken, ChatMute mute)
    {
        _callbackSubscriptions.PublishAll(
            (recipient, matchId, callback) => SendMuteNotification(
                sessionToken,
                recipient,
                matchId,
                callback,
                mute));
    }

    private void SendMuteNotification(
        string mutedSessionToken,
        string recipientSessionToken,
        long matchId,
        IMatchChatServiceCallback callback,
        ChatMute mute)
    {
        if (!_applicationService.CanReceiveMuteNotification(
            mutedSessionToken,
            recipientSessionToken,
            matchId))
        {
            return;
        }

        callback.OnPlayerMuted(new ChatMuteNotification
        {
            MatchId = matchId,
            StartsAtUtc = mute.StartsAtUtc,
            EndsAtUtc = mute.EndsAtUtc,
            ReasonCode = mute.ReasonCode
        });
    }

    private static void ThrowExpectedFault(
        ChatStatus status,
        ChatMute? mute = null)
    {
        switch (status)
        {
            case ChatStatus.Success:
                return;
            case ChatStatus.MissingSessionToken:
                throw SessionFaultFactory.Create(SessionFailure.MissingSessionToken);
            case ChatStatus.InvalidSession:
                throw SessionFaultFactory.Create(SessionFailure.InvalidSession);
            case ChatStatus.InactiveAccount:
                throw CreateMatchFault(MatchFailure.InactiveAccount);
            case ChatStatus.InvalidMatchId:
                throw CreateMatchFault(MatchFailure.InvalidMatchId);
            case ChatStatus.MatchNotFound:
                throw CreateMatchFault(MatchFailure.MatchNotFound);
            case ChatStatus.Unauthorized:
                throw CreateMatchFault(MatchFailure.Unauthorized);
            case ChatStatus.MatchNotInProgress:
                throw CreateMatchFault(MatchFailure.MatchNotInProgress);
            case ChatStatus.EmptyMessage:
                throw CreateChatFault(ChatFailure.EmptyMessage, mute);
            case ChatStatus.MessageTooLong:
                throw CreateChatFault(ChatFailure.MessageTooLong, mute);
            case ChatStatus.PlayerMuted:
                throw CreateChatFault(ChatFailure.PlayerMuted, mute);
            case ChatStatus.MessageBlocked:
                throw CreateChatFault(ChatFailure.MessageBlocked, mute);
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }

    private static FaultException<MatchFault> CreateMatchFault(MatchFailure failure)
    {
        return new FaultException<MatchFault>(new MatchFault
        {
            Reason = failure
        }, "The match request was rejected.");
    }

    private static FaultException<ChatFault> CreateChatFault(
        ChatFailure failure,
        ChatMute? mute)
    {
        return new FaultException<ChatFault>(new ChatFault
        {
            Reason = failure,
            MuteEndsAtUtc = mute?.EndsAtUtc
        }, "The chat message was rejected.");
    }

    private static MatchChatEntry MapChatEntry(ChatMessage message)
    {
        return new MatchChatEntry
        {
            MatchId = message.MatchId,
            MessageNumber = message.MessageNumber,
            SenderSeatNumber = message.SenderSeatNumber,
            Content = message.Content,
            SentAtUtc = message.SentAtUtc
        };
    }
}
