using System.Collections.Generic;
using ReversiGameSystem.Chat;
using ReversiGameSystem.Matches;

namespace ReversiGameSystem.Services;

internal static class RemoteMatchMessages
{
    private static readonly IReadOnlyDictionary<MatchStatus, string> s_messageKeys =
        new Dictionary<MatchStatus, string>
        {
            { MatchStatus.CannotJoinOwnMatch, "JoinMatch_CannotJoinOwnMatchMessage" },
            { MatchStatus.InvalidMatchCode, "JoinMatch_InvalidMatchCodeMessage" },
            { MatchStatus.MatchCodeUnavailable, "JoinMatch_MatchCodeNotAvailableMessage" },
            { MatchStatus.InvalidTurnDuration, "MatchCreation_InvalidTurnTimeMessage" },
            { MatchStatus.MatchNotWaiting, "InviteMatch_MatchNotWaitingMessage" },
            { MatchStatus.Unauthorized, "InviteMatch_UnauthorizedMessage" },
            { MatchStatus.AccountUnavailable, "InviteMatch_AccountUnavailableMessage" },
            { MatchStatus.FriendshipNotFound, "InviteMatch_FriendshipNotFoundMessage" },
            { MatchStatus.InvalidAccountId, "InviteMatch_InvalidAccountIdMessage" },
            { MatchStatus.InactiveAccount, "InviteMatch_InactiveAccountMessage" },
            { MatchStatus.InvalidPosition, "Game_InvalidPositionMessage" },
            { MatchStatus.ServiceUnavailable, "RemoteMatch_ConnectionFailed" }
        };

    internal static string Key(MatchStatus status, bool guest = false)
    {
        if (status == MatchStatus.Success)
        {
            return string.Empty;
        }
        if (status == MatchStatus.InvalidSession)
        {
            return guest ? "RemoteMatch_GuestUnavailable" : "RemoteMatch_SessionExpired";
        }
        if (s_messageKeys.ContainsKey(status))
        {
            return s_messageKeys[status];
        }
        return "RemoteMatch_AccessRejected";
    }
    internal static string Key(ChatStatus status, bool guest = false)
    {
        string key = status switch
        {
            ChatStatus.Success => string.Empty,
            ChatStatus.InvalidSession => guest
                ? "RemoteMatch_GuestUnavailable" : "RemoteMatch_SessionExpired",
            ChatStatus.EmptyMessage => "Chat_EmptyMessageMessage",
            ChatStatus.MessageTooLong => "Chat_MessageTooLongMessage",
            ChatStatus.MessageBlocked => "Chat_MessageBlockedMessage",
            ChatStatus.PlayerMuted => "Chat_PlayerMutedMessage",
            ChatStatus.RateLimitExceeded => "Chat_RateLimitExceeded",
            ChatStatus.ServiceUnavailable => "RemoteMatch_ConnectionFailed",
            _ => "RemoteMatch_AccessRejected"
        };
        return key;
    }
}
