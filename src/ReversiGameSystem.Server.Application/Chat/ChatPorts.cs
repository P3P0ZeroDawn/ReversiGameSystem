using System;

namespace ReversiGameSystem.Server.Application.Chat;

/// <summary>Confirmed conversation message.</summary>
public sealed class ChatEntryRecord
{
    /// <summary>Gets or sets the stable identifier of the conversation match.</summary>
    public long MatchId { get; set; }
    /// <summary>Gets or sets the positive durable sequence within the conversation.</summary>
    public int MessageNumber { get; set; }
    /// <summary>Gets or sets the server-resolved sender seat.</summary>
    public int SenderSeatNumber { get; set; }
    /// <summary>Gets or sets the trimmed accepted text, preserving its original spelling.</summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>Gets or sets the server UTC storage time.</summary>
    public DateTime SentAtUtc { get; set; }
}

/// <summary>Durable account sanction interval.</summary>
public sealed class ChatMuteRecord
{
    /// <summary>Gets or sets the confirmed UTC sanction start.</summary>
    public DateTime StartsAtUtc { get; set; }
    /// <summary>Gets or sets the confirmed UTC sanction expiry.</summary>
    public DateTime EndsAtUtc { get; set; }
}

/// <summary>Identifies an expected message rejection.</summary>
public enum ChatRejection
{
    /// <summary>Identifies the EmptyMessage rejection.</summary>
    EmptyMessage,
    /// <summary>Identifies the MessageTooLong rejection.</summary>
    MessageTooLong,
    /// <summary>Identifies the MessageBlocked rejection.</summary>
    MessageBlocked,
    /// <summary>Identifies the PlayerMuted rejection.</summary>
    PlayerMuted,
}

/// <summary>Expected moderation rejection; sanctions are already committed.</summary>
public sealed class ChatRejectedException : Exception
{
    /// <summary>Creates an expected chat rejection with its optional confirmed sanction.</summary>
    public ChatRejectedException(ChatRejection reason, ChatMuteRecord? mute = null)
        : base("Chat request rejected.")
    {
        Reason = reason;
        Mute = mute;
    }
    /// <summary>Gets the actionable expected rejection reason.</summary>
    public ChatRejection Reason { get; }
    /// <summary>Gets the confirmed sanction interval when moderation applies a mute.</summary>
    public ChatMuteRecord? Mute { get; }
}

/// <summary>Authoritative chat access and atomic account moderation.</summary>
public interface IChatStore
{
    /// <summary>Requires an active registered participant in an in-progress match.</summary>
    void Authorize(long accountId, long matchId);
    /// <summary>Tries to retrieve the account sanction currently in effect.</summary>
    bool TryGetMute(long accountId, DateTime now, out ChatMuteRecord? mute);
    /// <summary>Commits either the accepted message or its moderation outcome before returning.</summary>
    ChatEntryRecord Send(ChatSubmission submission);
    /// <summary>Loads the authorized conversation in ascending durable message order.</summary>
    ChatEntryRecord[] History(long accountId, long matchId);
}

/// <summary>Validated server-resolved message submission.</summary>
public sealed class ChatSubmission
{
    /// <summary>Gets or sets the stable server-resolved account identifier.</summary>
    public long AccountId { get; set; }
    /// <summary>Gets or sets the stable identifier of the conversation match.</summary>
    public long MatchId { get; set; }
    /// <summary>Gets or sets the trimmed accepted text, preserving its original spelling.</summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>Gets or sets the authoritative UTC submission time.</summary>
    public DateTime Now { get; set; }
}
