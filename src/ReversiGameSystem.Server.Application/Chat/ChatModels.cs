using System;

namespace ReversiGameSystem.Server.Application.Chat;

/// <summary>Contains one confirmed conversation message.</summary>
public class ChatMessage
{
    /// <summary>Gets or sets the durable match identifier.</summary>
    public long MatchId { get; set; }

    /// <summary>Gets or sets the match-local sequence number.</summary>
    public int MessageNumber { get; set; }

    /// <summary>Gets or sets the sender's confirmed seat number.</summary>
    public int SenderSeatNumber { get; set; }

    /// <summary>Gets or sets the accepted trimmed message content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the durable UTC send instant.</summary>
    public DateTime SentAtUtc { get; set; }
}

/// <summary>Contains a confirmed account sanction interval.</summary>
public class ChatMute
{
    /// <summary>Gets or sets the stable reason for the sanction.</summary>
    public string ReasonCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC instant at which the sanction began.</summary>
    public DateTime StartsAtUtc { get; set; }

    /// <summary>Gets or sets the UTC instant at which the sanction ends.</summary>
    public DateTime EndsAtUtc { get; set; }
}

/// <summary>Identifies an expected result of a chat operation.</summary>
public enum ChatStatus
{
    /// <summary>The operation completed successfully.</summary>
    Success,
    /// <summary>The required session token is absent.</summary>
    MissingSessionToken,
    /// <summary>The session is absent, expired, revoked, or belongs to a guest.</summary>
    InvalidSession,
    /// <summary>The registered account is no longer active.</summary>
    InactiveAccount,
    /// <summary>The match identifier is not positive.</summary>
    InvalidMatchId,
    /// <summary>The requested match does not exist.</summary>
    MatchNotFound,
    /// <summary>The account is not a confirmed participant.</summary>
    Unauthorized,
    /// <summary>The match is not currently in progress.</summary>
    MatchNotInProgress,
    /// <summary>The trimmed message is empty.</summary>
    EmptyMessage,
    /// <summary>The trimmed message exceeds the configured maximum length.</summary>
    MessageTooLong,
    /// <summary>Moderation rejected the message.</summary>
    MessageBlocked,
    /// <summary>The account has an active moderation sanction.</summary>
    PlayerMuted
}

/// <summary>Describes access to a live chat callback channel.</summary>
public class ChatChannelAccess
{
    /// <summary>Creates a rejected channel access.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The status represents successful access.
    /// </exception>
    public ChatChannelAccess(ChatStatus status)
    {
        if (status == ChatStatus.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Status = status;
    }

    /// <summary>Creates successful channel access with an optional active mute.</summary>
    public ChatChannelAccess(ChatMute? activeMute)
    {
        Status = ChatStatus.Success;
        ActiveMute = activeMute;
    }

    /// <summary>Gets the expected access status.</summary>
    public ChatStatus Status { get; }

    /// <summary>Gets the active mute when the account is currently sanctioned.</summary>
    public ChatMute? ActiveMute { get; }
}

/// <summary>Describes the processing of one submitted chat message.</summary>
public class ChatMessageSubmission
{
    /// <summary>Creates a rejected message submission.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The status requires a message or mute.
    /// </exception>
    public ChatMessageSubmission(ChatStatus status)
    {
        if (status == ChatStatus.Success || status == ChatStatus.PlayerMuted)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Status = status;
    }

    /// <summary>Creates a successful message submission.</summary>
    public ChatMessageSubmission(ChatMessage message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Status = ChatStatus.Success;
    }

    /// <summary>Creates a submission rejected by an active or newly created mute.</summary>
    public ChatMessageSubmission(ChatMute mute)
    {
        Mute = mute ?? throw new ArgumentNullException(nameof(mute));
        Status = ChatStatus.PlayerMuted;
    }

    /// <summary>Gets the expected processing status.</summary>
    public ChatStatus Status { get; }

    /// <summary>Gets the persisted message after successful processing.</summary>
    public ChatMessage? Message { get; }

    /// <summary>Gets the confirmed mute associated with the rejection.</summary>
    public ChatMute? Mute { get; }
}

/// <summary>Describes one authorized chat history query.</summary>
public class ChatHistory
{
    /// <summary>Creates a rejected history query.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The status represents a successful history query.
    /// </exception>
    public ChatHistory(ChatStatus status)
    {
        if (status == ChatStatus.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Status = status;
        Messages = Array.Empty<ChatMessage>();
    }

    /// <summary>Creates a successful ordered chat history.</summary>
    public ChatHistory(ChatMessage[] messages)
    {
        Messages = messages ?? throw new ArgumentNullException(nameof(messages));
        Status = ChatStatus.Success;
    }

    /// <summary>Gets the expected query status.</summary>
    public ChatStatus Status { get; }

    /// <summary>Gets the ordered messages, or an empty array after rejection.</summary>
    public ChatMessage[] Messages { get; }
}
