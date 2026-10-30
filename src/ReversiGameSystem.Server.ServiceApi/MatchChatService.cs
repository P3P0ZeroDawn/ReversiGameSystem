using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Chat;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Publishes committed participant messages and account moderation outcomes.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class MatchChatService : IMatchChatService, IDisposable
{
    private readonly ChatHandler _handler;
    private readonly IAuthenticatedSessionStore _sessions;
    private readonly MatchBoundary _boundary;
    private readonly MatchChannels<IMatchChatServiceCallback> _channels;

    /// <summary>Creates the durable chat boundary.</summary>
    public MatchChatService(ChatHandler handler, IAuthenticatedSessionStore sessions,
        ILogger<MatchChatService> logger)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _boundary = new MatchBoundary(logger);
        _channels = new MatchChannels<IMatchChatServiceCallback>((token, id) =>
        {
            try
            {
                _handler.Authorize(token, id); return true;
            }
            catch (SessionRejectedException)
            {
                return false;
            }
            catch (MatchRejectedException)
            {
                return false;
            }
        }, _ => { }, _boundary);
    }

    /// <inheritdoc />
    public void OpenChatChannel(string sessionToken, long matchId) =>
        _boundary.Execute(nameof(OpenChatChannel), () =>
        {
            _handler.Authorize(sessionToken, matchId);
            _channels.Register(sessionToken, matchId);
            if (_handler.TryGetMute(sessionToken, matchId, out ChatMuteRecord? mute) && mute != null)
            {
                PublishMute(sessionToken, mute);
            }
            return true;
        });

    /// <inheritdoc />
    public MatchChatEntry SendChatMessage(string sessionToken, long matchId, string content) =>
        _boundary.Execute(nameof(SendChatMessage), () =>
        {
            ChatEntryRecord message;
            try
            {
                message = _handler.Send(sessionToken, matchId, content);
            }
            catch (ChatRejectedException exception)
            {
                if (exception.Mute != null)
                {
                    PublishMute(sessionToken, exception.Mute);
                }
                throw new FaultException<ChatFault>(new ChatFault
                {
                    Reason = (ChatFailure)Enum.Parse(typeof(ChatFailure), exception.Reason.ToString()),
                    MuteEndsAtUtc = exception.Mute?.EndsAtUtc
                }, "The chat message was rejected.");
            }
            MatchChatEntry result = Map(message);
            _channels.Publish(matchId, (token, callback) =>
            {
                _handler.Authorize(token, matchId);
                callback.OnChatMessageReceived(Map(message));
            });
            return result;
        });

    /// <inheritdoc />
    public MatchChatEntry[] GetChatHistory(string sessionToken, long matchId) =>
        _boundary.Execute(nameof(GetChatHistory), () => _handler.History(sessionToken, matchId)
            .Select(Map).ToArray());

    /// <summary>Removes chat subscriptions for a revoked session.</summary>
    public void RemoveSession(string sessionToken) => _channels.RemoveSession(sessionToken);
    /// <inheritdoc />
    public void Dispose() => _channels.Dispose();

    private void PublishMute(string token, ChatMuteRecord mute) =>
        _channels.PublishAll((recipient, recipientMatchId, callback) =>
        {
            if (!_sessions.TryGet(token, out AuthenticatedSessionRecord? sender) || sender == null
                || !_sessions.TryGet(recipient, out AuthenticatedSessionRecord? receiver) || receiver == null
                || sender.AccountId != receiver.AccountId)
            {
                return;
            }
            _handler.Authorize(recipient, recipientMatchId);
            callback.OnPlayerMuted(new ChatMuteNotification
            {
                MatchId = recipientMatchId, StartsAtUtc = mute.StartsAtUtc, EndsAtUtc = mute.EndsAtUtc,
                ReasonCode = "REPEATED_VIOLATIONS"
            });
        });

    private static MatchChatEntry Map(ChatEntryRecord entry) => new MatchChatEntry
    {
        MatchId = entry.MatchId, MessageNumber = entry.MessageNumber,
        SenderSeatNumber = entry.SenderSeatNumber, Content = entry.Content, SentAtUtc = entry.SentAtUtc
    };
}
