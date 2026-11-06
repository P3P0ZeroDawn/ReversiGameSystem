using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Chat;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Publishes committed participant messages and moderation outcomes.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class MatchChatService : IMatchChatService, IDisposable
{
    private readonly ChatApplicationService _applicationService;
    private readonly ISessionRepository _sessions;
    private readonly MatchChannels<IMatchChatServiceCallback> _channels;

    /// <summary>Creates the durable chat service boundary.</summary>
    /// <param name="applicationService">The internal chat application service.</param>
    /// <param name="sessions">The authenticated session repository.</param>
    /// <param name="logger">The logger used by callback infrastructure.</param>
    public MatchChatService(
        ChatApplicationService applicationService,
        ISessionRepository sessions,
        ILogger<MatchChatService> logger)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _channels = new MatchChannels<IMatchChatServiceCallback>(
            AuthorizeChannel,
            IgnoreRemovedChannel,
            logger);
    }

    /// <inheritdoc />
    public void OpenChatChannel(string sessionToken, long matchId)
    {
        ChatOpenResult result = _applicationService.Open(
            sessionToken,
            matchId);
        ThrowFailure(result.Status);
        _channels.Register(sessionToken, matchId);

        if (result.HasActiveMute)
        {
            PublishMute(sessionToken, result.GetActiveMute());
        }
    }

    /// <inheritdoc />
    public MatchChatEntry SendChatMessage(
        string sessionToken,
        long matchId,
        string content)
    {
        ChatOperationResult<ChatMessage> result = _applicationService.Send(
            sessionToken,
            matchId,
            content);
        if (result.Status == ChatOperationStatus.PlayerMuted && result.HasMute)
        {
            PublishMute(sessionToken, result.GetMute());
        }

        ThrowFailure(result.Status, result);
        ChatMessage message = result.GetValue();
        MatchChatEntry contractMessage = Map(message);
        _channels.Publish(matchId, (token, callback) =>
        {
            if (_applicationService.Authorize(token, matchId)
                == ChatOperationStatus.Success)
            {
                callback.OnChatMessageReceived(Map(message));
            }
        });
        return contractMessage;
    }

    /// <inheritdoc />
    public MatchChatEntry[] GetChatHistory(string sessionToken, long matchId)
    {
        ChatOperationResult<ChatMessage[]> result = _applicationService.GetHistory(
            sessionToken,
            matchId);
        ThrowFailure(result.Status, result);

        ChatMessage[] messages = result.GetValue();
        MatchChatEntry[] entries = new MatchChatEntry[messages.Length];
        for (int index = 0; index < messages.Length; index++)
        {
            entries[index] = Map(messages[index]);
        }

        return entries;
    }

    /// <summary>Removes chat subscriptions for a revoked session.</summary>
    public void RemoveSession(string sessionToken)
    {
        _channels.RemoveSession(sessionToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _channels.Dispose();
    }

    private bool AuthorizeChannel(string token, long matchId)
    {
        return _applicationService.Authorize(token, matchId)
            == ChatOperationStatus.Success;
    }

    private static void IgnoreRemovedChannel(string token, long matchId)
    {
    }

    private void PublishMute(string sessionToken, ChatMute mute)
    {
        _channels.PublishAll((recipient, recipientMatchId, callback) =>
        {
            if (!_sessions.TryGet(sessionToken, out AuthenticatedSession? sender)
                || sender == null
                || !_sessions.TryGet(recipient, out AuthenticatedSession? receiver)
                || receiver == null
                || sender.AccountId != receiver.AccountId)
            {
                return;
            }
            if (_applicationService.Authorize(recipient, recipientMatchId)
                != ChatOperationStatus.Success)
            {
                return;
            }

            callback.OnPlayerMuted(new ChatMuteNotification
            {
                MatchId = recipientMatchId,
                StartsAtUtc = mute.StartsAtUtc,
                EndsAtUtc = mute.EndsAtUtc,
                ReasonCode = "REPEATED_VIOLATIONS"
            });
        });
    }

    private static void ThrowFailure<T>(
        ChatOperationStatus status,
        ChatOperationResult<T> result)
    {
        ChatMute? mute = null;
        if (result.HasMute)
        {
            mute = result.GetMute();
        }

        ThrowFailure(status, mute);
    }

    private static void ThrowFailure(ChatOperationStatus status)
    {
        ThrowFailure(status, null);
    }

    private static void ThrowFailure(ChatOperationStatus status, ChatMute? mute)
    {
        if (status == ChatOperationStatus.Success)
        {
            return;
        }
        if (status == ChatOperationStatus.MissingSessionToken)
        {
            throw SessionFaultFactory.Create(SessionFailure.MissingSessionToken);
        }
        if (status == ChatOperationStatus.InvalidSession)
        {
            throw SessionFaultFactory.Create(SessionFailure.InvalidSession);
        }
        if (status == ChatOperationStatus.InactiveAccount)
        {
            throw CreateMatchFault(MatchFailure.InactiveAccount);
        }
        if (status == ChatOperationStatus.InvalidMatchId)
        {
            throw CreateMatchFault(MatchFailure.InvalidMatchId);
        }
        if (status == ChatOperationStatus.MatchNotFound)
        {
            throw CreateMatchFault(MatchFailure.MatchNotFound);
        }
        if (status == ChatOperationStatus.Unauthorized)
        {
            throw CreateMatchFault(MatchFailure.Unauthorized);
        }
        if (status == ChatOperationStatus.MatchNotInProgress)
        {
            throw CreateMatchFault(MatchFailure.MatchNotInProgress);
        }

        ChatFailure failure = GetChatFailure(status);
        throw new FaultException<ChatFault>(new ChatFault
        {
            Reason = failure,
            MuteEndsAtUtc = mute == null ? null : mute.EndsAtUtc
        }, "The chat message was rejected.");
    }

    private static FaultException<MatchFault> CreateMatchFault(MatchFailure failure)
    {
        return new FaultException<MatchFault>(new MatchFault
        {
            Reason = failure
        }, "The match request was rejected.");
    }

    private static ChatFailure GetChatFailure(ChatOperationStatus status)
    {
        ChatFailure failure = ChatFailure.MessageBlocked;
        if (status == ChatOperationStatus.EmptyMessage)
        {
            failure = ChatFailure.EmptyMessage;
        }
        else if (status == ChatOperationStatus.MessageTooLong)
        {
            failure = ChatFailure.MessageTooLong;
        }
        else if (status == ChatOperationStatus.PlayerMuted)
        {
            failure = ChatFailure.PlayerMuted;
        }

        return failure;
    }

    private static MatchChatEntry Map(ChatMessage message)
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
