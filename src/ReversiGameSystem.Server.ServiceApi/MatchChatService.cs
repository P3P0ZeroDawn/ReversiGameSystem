using System;
using System.ServiceModel;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Chat;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Publishes committed participant messages and moderation outcomes.</summary>
[ServiceBehavior(
    InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple,
    UseSynchronizationContext = false)]
public class MatchChatService : IMatchChatService
{
    private readonly ChatApplicationService _applicationService;
    private readonly MatchCallbackSubscriptions<IMatchChatServiceCallback>
        _callbackSubscriptions;

    /// <summary>Creates the durable chat service boundary.</summary>
    /// <param name="applicationService">The internal chat application service.</param>
    /// <param name="callbackSubscriptions">The chat duplex callback subscriptions.</param>
    /// <exception cref="ArgumentNullException">A required dependency is null.</exception>
    public MatchChatService(
        ChatApplicationService applicationService,
        MatchCallbackSubscriptions<IMatchChatServiceCallback> callbackSubscriptions)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
        _callbackSubscriptions = callbackSubscriptions
            ?? throw new ArgumentNullException(nameof(callbackSubscriptions));
    }

    /// <inheritdoc />
    public void OpenChatChannel(string sessionToken, long matchId)
    {
        ChatChannelAccess access = _applicationService.PrepareChatChannel(
            sessionToken,
            matchId);
        ThrowFaultIfFailed(access.Status);
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

        ThrowFaultIfFailed(submission.Status, submission.Mute);
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
        ThrowFaultIfFailed(history.Status);

        ChatMessage[] messages = history.Messages;
        MatchChatEntry[] entries = new MatchChatEntry[messages.Length];
        for (int index = 0; index < messages.Length; index++)
        {
            entries[index] = MapChatEntry(messages[index]);
        }

        return entries;
    }

    private void PublishMuteNotification(string sessionToken, ChatMute mute)
    {
        _callbackSubscriptions.PublishAll((recipient, matchId, callback) =>
        {
            if (_applicationService.CanReceiveMuteNotification(
                sessionToken,
                recipient,
                matchId))
            {
                callback.OnPlayerMuted(MapMuteNotification(matchId, mute));
            }
        });
    }

    private static void ThrowFaultIfFailed(
        ChatStatus status,
        ChatMute? mute = null)
    {
        switch (status)
        {
            case ChatStatus.Success:
                return;
            case ChatStatus.MissingSessionToken:
                throw ServiceFaultFactory.CreateSessionFault(SessionFailure.MissingSessionToken);
            case ChatStatus.InvalidSession:
                throw ServiceFaultFactory.CreateSessionFault(SessionFailure.InvalidSession);
            case ChatStatus.InactiveAccount:
                throw ServiceFaultFactory.CreateMatchFault(MatchFailure.InactiveAccount);
            case ChatStatus.InvalidMatchId:
                throw ServiceFaultFactory.CreateMatchFault(MatchFailure.InvalidMatchId);
            case ChatStatus.MatchNotFound:
                throw ServiceFaultFactory.CreateMatchFault(MatchFailure.MatchNotFound);
            case ChatStatus.Unauthorized:
                throw ServiceFaultFactory.CreateMatchFault(MatchFailure.Unauthorized);
            case ChatStatus.MatchNotInProgress:
                throw ServiceFaultFactory.CreateMatchFault(MatchFailure.MatchNotInProgress);
            case ChatStatus.EmptyMessage:
                throw CreateChatFault(ChatFailure.EmptyMessage, mute);
            case ChatStatus.MessageTooLong:
                throw CreateChatFault(ChatFailure.MessageTooLong, mute);
            case ChatStatus.PlayerMuted:
                throw CreateChatFault(ChatFailure.PlayerMuted, mute);
            case ChatStatus.RateLimitExceeded:
                throw CreateChatFault(ChatFailure.RateLimitExceeded, null);
            case ChatStatus.MessageBlocked:
                throw CreateChatFault(ChatFailure.MessageBlocked, mute);
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
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

    private static ChatMuteNotification MapMuteNotification(long matchId, ChatMute mute)
    {
        return new ChatMuteNotification
        {
            MatchId = matchId,
            StartsAtUtc = mute.StartsAtUtc,
            EndsAtUtc = mute.EndsAtUtc,
            ReasonCode = mute.ReasonCode
        };
    }

    internal static FaultException<ChatFault> CreateChatFault(
        ChatFailure failure,
        ChatMute? mute)
    {
        return new FaultException<ChatFault>(new ChatFault
        {
            Reason = failure,
            MuteEndsAtUtc = mute?.EndsAtUtc
        }, "The chat message was rejected.");
    }
}
