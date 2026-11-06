using System;

namespace ReversiGameSystem.Server.Application.Chat;

/// <summary>Contains one confirmed conversation message.</summary>
public sealed class ChatMessage
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
public sealed class ChatMute
{
    /// <summary>Gets or sets the UTC instant at which the sanction began.</summary>
    public DateTime StartsAtUtc { get; set; }

    /// <summary>Gets or sets the UTC instant at which the sanction ends.</summary>
    public DateTime EndsAtUtc { get; set; }
}

/// <summary>Contains validated server-resolved data for one message attempt.</summary>
public sealed class ChatMessageSubmission
{
    /// <summary>Gets or sets the server-resolved account identifier.</summary>
    public long AccountId { get; set; }

    /// <summary>Gets or sets the validated durable match identifier.</summary>
    public long MatchId { get; set; }

    /// <summary>Gets or sets the validated and trimmed content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the server-resolved current UTC instant.</summary>
    public DateTime NowUtc { get; set; }
}

/// <summary>Identifies an expected result of a chat operation.</summary>
public enum ChatOperationStatus
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
    /// <summary>The trimmed message exceeds 200 UTF-16 code units.</summary>
    MessageTooLong,
    /// <summary>Moderation rejected the message.</summary>
    MessageBlocked,
    /// <summary>The account has an active moderation sanction.</summary>
    PlayerMuted
}

/// <summary>Contains the expected status and successful value of a chat operation.</summary>
public sealed class ChatOperationResult<T>
{
    private readonly T? _value;
    private readonly ChatMute? _mute;

    private ChatOperationResult(
        ChatOperationStatus status,
        T? value,
        ChatMute? mute)
    {
        Status = status;
        _value = value;
        _mute = mute;
    }

    /// <summary>Gets the expected operation status.</summary>
    public ChatOperationStatus Status { get; }

    /// <summary>Gets whether the result contains a confirmed mute.</summary>
    public bool HasMute
    {
        get
        {
            return _mute != null;
        }
    }

    /// <summary>Creates a successful operation result containing a value.</summary>
    /// <param name="value">The required successful value.</param>
    /// <returns>A successful chat operation result.</returns>
    /// <exception cref="ArgumentNullException">The value is absent.</exception>
    public static ChatOperationResult<T> CreateSuccess(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new ChatOperationResult<T>(ChatOperationStatus.Success, value, null);
    }

    /// <summary>Creates an expected failed result and optional confirmed mute.</summary>
    /// <param name="status">The expected failure status.</param>
    /// <param name="mute">The required mute for a muted-player result.</param>
    /// <returns>A failed chat operation result.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The status represents success.</exception>
    /// <exception cref="ArgumentNullException">A muted result lacks its mute.</exception>
    public static ChatOperationResult<T> CreateFailure(
        ChatOperationStatus status,
        ChatMute? mute = null)
    {
        if (status == ChatOperationStatus.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }
        if (status == ChatOperationStatus.PlayerMuted && mute == null)
        {
            throw new ArgumentNullException(nameof(mute));
        }

        return new ChatOperationResult<T>(status, default, mute);
    }

    /// <summary>Gets the value contained in a successful result.</summary>
    /// <returns>The successful operation value.</returns>
    /// <exception cref="InvalidOperationException">The operation did not succeed.</exception>
    public T GetValue()
    {
        if (Status != ChatOperationStatus.Success || _value is null)
        {
            throw new InvalidOperationException(
                "A successful chat result must contain a value.");
        }

        return _value;
    }

    /// <summary>Gets the confirmed mute contained in this result.</summary>
    /// <returns>The confirmed moderation sanction.</returns>
    /// <exception cref="InvalidOperationException">The result contains no mute.</exception>
    public ChatMute GetMute()
    {
        if (_mute == null)
        {
            throw new InvalidOperationException(
                "This chat result does not contain a confirmed mute.");
        }

        return _mute;
    }
}

/// <summary>Contains the expected result of opening an authorized chat channel.</summary>
public sealed class ChatOpenResult
{
    private readonly ChatMute? _activeMute;

    private ChatOpenResult(ChatOperationStatus status, ChatMute? activeMute)
    {
        Status = status;
        _activeMute = activeMute;
    }

    /// <summary>Gets the expected channel operation status.</summary>
    public ChatOperationStatus Status { get; }

    /// <summary>Gets whether the account has an active mute.</summary>
    public bool HasActiveMute
    {
        get
        {
            return _activeMute != null;
        }
    }

    /// <summary>Creates successful channel access with an optional active mute.</summary>
    /// <param name="activeMute">The confirmed active mute, when one exists.</param>
    /// <returns>A successful chat channel result.</returns>
    public static ChatOpenResult CreateSuccess(ChatMute? activeMute)
    {
        return new ChatOpenResult(ChatOperationStatus.Success, activeMute);
    }

    /// <summary>Creates an expected failed channel result.</summary>
    /// <param name="status">The expected failure status.</param>
    /// <returns>A failed chat channel result.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The status represents success.</exception>
    public static ChatOpenResult CreateFailure(ChatOperationStatus status)
    {
        if (status == ChatOperationStatus.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        return new ChatOpenResult(status, null);
    }

    /// <summary>Gets the active mute contained in this access state.</summary>
    /// <returns>The confirmed active mute.</returns>
    /// <exception cref="InvalidOperationException">No active mute exists.</exception>
    public ChatMute GetActiveMute()
    {
        if (_activeMute == null)
        {
            throw new InvalidOperationException("The chat channel has no active mute.");
        }

        return _activeMute;
    }
}

/// <summary>Identifies expected message failures in persistence awaiting migration.</summary>
public enum ChatRejection
{
    /// <summary>The trimmed message is empty.</summary>
    EmptyMessage,
    /// <summary>The trimmed message exceeds the accepted length.</summary>
    MessageTooLong,
    /// <summary>Moderation rejected the message.</summary>
    MessageBlocked,
    /// <summary>The account has an active mute.</summary>
    PlayerMuted
}

/// <summary>Reports a moderation failure in persistence awaiting result migration.</summary>
public sealed class ChatRejectedException : Exception
{
    /// <summary>Creates a temporary persistence rejection.</summary>
    /// <param name="reason">The expected rejection reason.</param>
    /// <param name="mute">The confirmed mute, when applicable.</param>
    public ChatRejectedException(ChatRejection reason, ChatMute? mute = null)
        : base("Chat request rejected.")
    {
        Reason = reason;
        Mute = mute;
    }

    /// <summary>Gets the expected rejection reason.</summary>
    public ChatRejection Reason { get; }

    /// <summary>Gets the confirmed mute, when one applies.</summary>
    public ChatMute? Mute { get; }
}
