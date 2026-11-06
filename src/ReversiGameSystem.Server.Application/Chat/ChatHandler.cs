using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Application.Chat;

/// <summary>Resolves callers and validates messages before durable processing.</summary>
public sealed class ChatHandler
{
    private readonly ISessionRepository _sessions;
    private readonly IChatStore _store;
    private readonly IUtcClock _clock;

    /// <summary>Creates a chat handler with server sessions, durable storage, and a clock.</summary>
    public ChatHandler(ISessionRepository sessions, IChatStore store, IUtcClock clock)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Requires an active registered participant in an in-progress match.</summary>
    public void Authorize(string token, long id) => _store.Authorize(RequirePlayer(token), RequireId(id));

    /// <summary>Tries to retrieve the account sanction currently in effect.</summary>
    public bool TryGetMute(string token, long id, out ChatMuteRecord? mute)
    {
        long accountId = RequirePlayer(token);
        _store.Authorize(accountId, RequireId(id));
        return _store.TryGetMute(accountId, _clock.UtcNow, out mute);
    }

    /// <summary>Commits either the accepted message or its moderation outcome before returning.</summary>
    public ChatEntryRecord Send(string token, long id, string content)
    {
        long accountId = RequirePlayer(token);
        _store.Authorize(accountId, RequireId(id));
        string trimmed = (content ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new ChatRejectedException(ChatRejection.EmptyMessage);
        }
        if (trimmed.Length > 200)
        {
            throw new ChatRejectedException(ChatRejection.MessageTooLong);
        }
        return _store.Send(new ChatSubmission
        {
            AccountId = accountId, MatchId = id, Content = trimmed, Now = _clock.UtcNow
        });
    }

    /// <summary>Loads the authorized conversation in ascending durable message order.</summary>
    public ChatEntryRecord[] History(string token, long id) => _store.History(RequirePlayer(token), RequireId(id));

    private long RequirePlayer(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new SessionRejectedException(SessionRejection.MissingSessionToken);
        }
        if (!_sessions.TryGet(token, out AuthenticatedSession? player)
            || player == null || player.ExpiresAtUtc <= _clock.UtcNow)
        {
            throw new SessionRejectedException();
        }
        return player.AccountId;
    }

    private static long RequireId(long id)
    {
        if (id <= 0)
        {
            throw new MatchRejectedException(MatchRejection.InvalidMatchId);
        }
        return id;
    }
}
