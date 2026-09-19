using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ReversiGameSystem.Services;

internal enum ChatSendResult
{
    Success,
    EmptyMessage,
    MessageTooLong,
    Unauthorized,
    PlayerMuted,
    MessageBlocked,
    ServerConnectionFailed,
    MessagePersistenceError,
    ModerationPersistenceError,
    MutePersistenceError
}

internal enum ChatReadResult
{
    Success,
    Unauthorized,
    ServerConnectionFailed,
    RetrievalError
}

internal sealed class ChatMessage
{
    internal ChatMessage(
        string id,
        string conversationId,
        string senderUsername,
        string content,
        DateTime sentAtUtc)
    {
        Id = id;
        ConversationId = conversationId;
        SenderUsername = senderUsername;
        Content = content;
        SentAtUtc = sentAtUtc;
    }

    internal string Id { get; }

    internal string ConversationId { get; }

    internal string SenderUsername { get; }

    internal string Content { get; }

    internal DateTime SentAtUtc { get; }
}

internal sealed class ChatInfringement
{
    internal ChatInfringement(
        string conversationId,
        string username,
        string rule,
        DateTime recordedAtUtc)
    {
        ConversationId = conversationId;
        Username = username;
        Rule = rule;
        RecordedAtUtc = recordedAtUtc;
    }

    internal string ConversationId { get; }

    internal string Username { get; }

    internal string Rule { get; }

    internal DateTime RecordedAtUtc { get; }
}

internal sealed class ChatMute
{
    internal ChatMute(
        string username,
        string reason,
        DateTime startsAtUtc,
        DateTime endsAtUtc)
    {
        Username = username;
        Reason = reason;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
    }

    internal string Username { get; }

    internal string Reason { get; }

    internal DateTime StartsAtUtc { get; }

    internal DateTime EndsAtUtc { get; }
}

/// <summary>
/// Stores prototype chat data and applies moderation atomically.
/// </summary>
internal sealed class ChatStore
{
    internal const int MaximumMessageLength = 200;
    internal const int InfractionsBeforeMute = 3;
    internal static readonly TimeSpan InfringementWindow = TimeSpan.FromHours(24);
    internal static readonly TimeSpan MuteDuration = TimeSpan.FromMinutes(10);

    private static readonly string[] ProhibitedExpressions =
    {
        "callate",
        "dumbass",
        "estupida",
        "estupido",
        "fuck",
        "idiot",
        "idiota",
        "imbecil",
        "mierda",
        "moron",
        "pendeja",
        "pendejo",
        "puta",
        "puto",
        "shut up",
        "stupid"
    };

    private readonly object _syncRoot = new object();
    private readonly Func<DateTime> _utcNow;
    private readonly List<ChatMessage> _messages = new List<ChatMessage>();
    private readonly List<ChatInfringement> _infringements =
        new List<ChatInfringement>();
    private readonly List<ChatMute> _mutes = new List<ChatMute>();

    internal ChatStore(Func<DateTime>? utcNow = null)
    {
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    internal event Action<ChatMessage>? MessageStored;

    internal ChatSendResult TryStoreMessage(
        string conversationId,
        string senderUsername,
        string content,
        out ChatMessage? message,
        out DateTime? muteEndsAtUtc)
    {
        lock (_syncRoot)
        {
            message = null;
            DateTime now = _utcNow();
            ChatMute? activeMute = _mutes.LastOrDefault(mute =>
                string.Equals(
                    mute.Username,
                    senderUsername,
                    StringComparison.OrdinalIgnoreCase)
                && mute.EndsAtUtc > now);
            if (activeMute != null)
            {
                muteEndsAtUtc = activeMute.EndsAtUtc;
                return ChatSendResult.PlayerMuted;
            }

            if (TryFindProhibitedExpression(content, out string rule))
            {
                _infringements.Add(new ChatInfringement(
                    conversationId,
                    senderUsername,
                    rule,
                    now));
                DateTime windowStart = now.Subtract(InfringementWindow);
                int recentInfractions = _infringements.Count(infringement =>
                    string.Equals(
                        infringement.Username,
                        senderUsername,
                        StringComparison.OrdinalIgnoreCase)
                    && infringement.RecordedAtUtc >= windowStart);
                if (recentInfractions >= InfractionsBeforeMute)
                {
                    muteEndsAtUtc = now.Add(MuteDuration);
                    _mutes.Add(new ChatMute(
                        senderUsername,
                        rule,
                        now,
                        muteEndsAtUtc.Value));
                    return ChatSendResult.PlayerMuted;
                }

                muteEndsAtUtc = null;
                return ChatSendResult.MessageBlocked;
            }

            message = new ChatMessage(
                Guid.NewGuid().ToString("N"),
                conversationId,
                senderUsername,
                content,
                now);
            _messages.Add(message);
            muteEndsAtUtc = null;
        }

        MessageStored?.Invoke(message);
        return ChatSendResult.Success;
    }

    internal IReadOnlyList<ChatMessage> GetMessages(string conversationId)
    {
        lock (_syncRoot)
        {
            return _messages
                .Where(message => string.Equals(
                    message.ConversationId,
                    conversationId,
                    StringComparison.Ordinal))
                .OrderBy(message => message.SentAtUtc)
                .ThenBy(message => message.Id, StringComparer.Ordinal)
                .ToArray();
        }
    }

    internal int GetInfringementCount(string username)
    {
        lock (_syncRoot)
        {
            return _infringements.Count(infringement => string.Equals(
                infringement.Username,
                username,
                StringComparison.OrdinalIgnoreCase));
        }
    }

    private static bool TryFindProhibitedExpression(string content, out string rule)
    {
        string normalizedContent = " " + Regex.Replace(
            RemoveDiacritics(content).ToLowerInvariant(),
            @"[^\p{L}\p{Nd}]+",
            " ").Trim() + " ";
        foreach (string expression in ProhibitedExpressions)
        {
            if (normalizedContent.Contains(" " + expression + " "))
            {
                rule = expression;
                return true;
            }
        }

        rule = string.Empty;
        return false;
    }

    private static string RemoveDiacritics(string value)
    {
        string decomposed = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(decomposed.Length);
        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character)
                != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

/// <summary>
/// Validates chat access and delegates storage and moderation as one operation.
/// </summary>
internal sealed class ChatService
{
    private readonly MatchSession _session;
    private readonly ChatStore _store;

    internal ChatService(MatchSession session, ChatStore store)
    {
        _session = session;
        _store = store;
    }

    internal event Action<ChatMessage> MessageReceived
    {
        add { _store.MessageStored += value; }
        remove { _store.MessageStored -= value; }
    }

    internal ChatSendResult Send(
        string content,
        out ChatMessage? message,
        out DateTime? muteEndsAtUtc)
    {
        message = null;
        muteEndsAtUtc = null;
        string normalizedContent = content.Trim();
        if (normalizedContent.Length == 0)
        {
            return ChatSendResult.EmptyMessage;
        }

        if (normalizedContent.Length > ChatStore.MaximumMessageLength)
        {
            return ChatSendResult.MessageTooLong;
        }

        if (!IsAuthorized())
        {
            return ChatSendResult.Unauthorized;
        }

        return _store.TryStoreMessage(
            _session.Code,
            _session.LocalPlayerName,
            normalizedContent,
            out message,
            out muteEndsAtUtc);
    }

    internal ChatReadResult TryGetMessages(
        out IReadOnlyList<ChatMessage> messages)
    {
        if (!IsAuthorized())
        {
            messages = Array.Empty<ChatMessage>();
            return ChatReadResult.Unauthorized;
        }

        messages = _store.GetMessages(_session.Code);
        return ChatReadResult.Success;
    }

    internal bool CanReadMessage(ChatMessage message)
    {
        return IsAuthorized()
            && string.Equals(
                message.ConversationId,
                _session.Code,
                StringComparison.Ordinal);
    }

    private bool IsAuthorized()
    {
        if (_session.GuestName != null
            || _session.State != MatchState.InProgress
            || string.IsNullOrWhiteSpace(_session.LocalPlayerName))
        {
            return false;
        }

        return string.Equals(
                _session.LocalPlayerName,
                _session.CreatorName,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                _session.LocalPlayerName,
                _session.OpponentName,
                StringComparison.OrdinalIgnoreCase);
    }
}

internal static class ChatPreviewState
{
    private static readonly ChatStore Store = new ChatStore();

    internal static ChatStore GetStore()
    {
        return Store;
    }
}
