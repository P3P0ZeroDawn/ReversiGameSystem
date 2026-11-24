using System;

namespace ReversiGameSystem.Server.Application.Chat;

public class ChatMessage
{
    public long MatchId { get; set; }

    public int MessageNumber { get; set; }

    public int SenderSeatNumber { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; }
}

public class ChatMute
{
    public string ReasonCode { get; set; } = string.Empty;

    public DateTime StartsAtUtc { get; set; }

    public DateTime EndsAtUtc { get; set; }
}

public enum ChatStatus
{
    Success,
    MissingSessionToken,
    InvalidSession,
    InactiveAccount,
    InvalidMatchId,
    MatchNotFound,
    Unauthorized,
    MatchNotInProgress,
    EmptyMessage,
    MessageTooLong,
    MessageBlocked,
    PlayerMuted,
    RateLimitExceeded
}

public class ChatChannelAccess
{
    public ChatChannelAccess(ChatStatus status)
    {
        if (status == ChatStatus.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Status = status;
    }

    public ChatChannelAccess(ChatMute? activeMute)
    {
        Status = ChatStatus.Success;
        ActiveMute = activeMute;
    }

    public ChatStatus Status { get; }

    public ChatMute? ActiveMute { get; }
}

public class ChatMessageSubmission
{
    public ChatMessageSubmission(ChatStatus status)
    {
        if (status == ChatStatus.Success || status == ChatStatus.PlayerMuted)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Status = status;
    }

    public ChatMessageSubmission(ChatMessage message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Status = ChatStatus.Success;
    }

    public ChatMessageSubmission(ChatMute mute)
    {
        Mute = mute ?? throw new ArgumentNullException(nameof(mute));
        Status = ChatStatus.PlayerMuted;
    }

    public ChatStatus Status { get; }

    public ChatMessage? Message { get; }

    public ChatMute? Mute { get; }
}

public class ChatHistory
{
    public ChatHistory(ChatStatus status)
    {
        if (status == ChatStatus.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Status = status;
        Messages = Array.Empty<ChatMessage>();
    }

    public ChatHistory(ChatMessage[] messages)
    {
        Messages = messages ?? throw new ArgumentNullException(nameof(messages));
        Status = ChatStatus.Success;
    }

    public ChatStatus Status { get; }

    public ChatMessage[] Messages { get; }
}
