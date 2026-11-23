using System;
using ReversiGameSystem.Chat;
using ReversiGameSystem.Matches;

namespace ReversiGameSystem.Services;

internal static class RemoteMatchMessages
{
    private const string GuestSessionUnavailableKey = "RemoteMatch_GuestUnavailable";
    private const string SessionExpiredKey = "RemoteMatch_SessionExpired";

    internal static string GetMessageResourceKey(MatchStatus status, bool guest = false)
    {
        string resourceKey = status switch
        {
            MatchStatus.Success => string.Empty,
            MatchStatus.InvalidSession => guest ? GuestSessionUnavailableKey : SessionExpiredKey,
            MatchStatus.InvalidMatchId => "RemoteMatch_InvalidMatchId",
            MatchStatus.InvalidConfiguration => "RemoteMatch_InvalidConfiguration",
            MatchStatus.InvalidTurnDuration => "MatchCreation_InvalidTurnTimeMessage",
            MatchStatus.MatchNotFound => "RemoteMatch_NotFound",
            MatchStatus.CannotJoinOwnMatch => "JoinMatch_CannotJoinOwnMatchMessage",
            MatchStatus.InvalidMatchCode => "JoinMatch_InvalidMatchCodeMessage",
            MatchStatus.MatchCodeUnavailable => "JoinMatch_MatchCodeNotAvailableMessage",
            MatchStatus.Unauthorized => "RemoteMatch_Unauthorized",
            MatchStatus.MatchNotWaiting => "RemoteMatch_NotWaiting",
            MatchStatus.MatchNotInProgress => "RemoteMatch_NotInProgress",
            MatchStatus.MissingPlacement => "RemoteMatch_MissingPlacement",
            MatchStatus.InvalidMatchStateVersion => "RemoteMatch_InvalidStateVersion",
            MatchStatus.StaleMatchStateVersion => "RemoteMatch_StaleStateVersion",
            MatchStatus.NotPlayersTurn => "RemoteMatch_NotPlayersTurn",
            MatchStatus.InvalidPosition => "Game_InvalidPositionMessage",
            MatchStatus.SpectatingUnavailable => "RemoteMatch_SpectatingUnavailable",
            MatchStatus.InactiveAccount => "RemoteMatch_InactiveAccount",
            MatchStatus.AccountUnavailable => "InviteMatch_AccountUnavailableMessage",
            MatchStatus.FriendshipNotFound => "InviteMatch_FriendshipNotFoundMessage",
            MatchStatus.InvalidAccountId => "InviteMatch_InvalidAccountIdMessage",
            MatchStatus.ServiceUnavailable => "RemoteMatch_ConnectionFailed",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status,
                "The status has no message resource.")
        };
        return resourceKey;
    }

    internal static string GetMessageResourceKey(ChatStatus status, bool guest = false)
    {
        string resourceKey = status switch
        {
            ChatStatus.Success => string.Empty,
            ChatStatus.InvalidSession => guest ? GuestSessionUnavailableKey : SessionExpiredKey,
            ChatStatus.InactiveAccount => "RemoteMatch_InactiveAccount",
            ChatStatus.Unauthorized => "Chat_UnauthorizedMessage",
            ChatStatus.MatchNotFound => "RemoteMatch_NotFound",
            ChatStatus.MatchNotInProgress => "RemoteMatch_NotInProgress",
            ChatStatus.EmptyMessage => "Chat_EmptyMessageMessage",
            ChatStatus.MessageTooLong => "Chat_MessageTooLongMessage",
            ChatStatus.MessageBlocked => "Chat_MessageBlockedMessage",
            ChatStatus.PlayerMuted => "Chat_PlayerMutedMessage",
            ChatStatus.RateLimitExceeded => "Chat_RateLimitExceeded",
            ChatStatus.ServiceUnavailable => "RemoteMatch_ConnectionFailed",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status,
                "The status has no message resource.")
        };
        return resourceKey;
    }
}
