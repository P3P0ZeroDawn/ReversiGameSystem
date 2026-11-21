namespace ReversiGameSystem.Chat;

internal enum ChatStatus
{
    Success,
    InvalidSession,
    InactiveAccount,
    Unauthorized,
    MatchNotFound,
    MatchNotInProgress,
    EmptyMessage,
    MessageTooLong,
    MessageBlocked,
    PlayerMuted,
    RateLimitExceeded,
    ServiceUnavailable
}

internal sealed class ChatMessage
{
    internal ChatMessage(long matchId, int number, int senderSeatNumber, string content)
    {
        MatchId = matchId;
        Number = number;
        SenderSeatNumber = senderSeatNumber;
        Content = content;
    }

    internal long MatchId { get; }
    internal int Number { get; }
    internal int SenderSeatNumber { get; }
    internal string Content { get; }
}

