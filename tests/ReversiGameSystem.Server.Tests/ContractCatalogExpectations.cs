using System.Collections.Generic;

namespace ReversiGameSystem.Server.Tests;

/// <summary>
/// Records the approved catalog independently of the implementation under test.
/// </summary>
internal static class ContractCatalogExpectations
{
    internal static readonly Dictionary<string, string[]> Operations = new()
    {
        ["ISessionService"] = new[]
        {
            "DateTime RequestLoginCode(string loginIdentifier, string password)",
            "AuthenticatedSession CompleteLogin(string loginIdentifier, "
                + "string verificationCode)",
            "GuestSession OpenGuestSession(string displayName)",
            "void CloseSession(string sessionToken)",
        },
        ["IAccountService"] = new[]
        {
            "DateTime RequestRegistrationCode(string username, string emailAddress)",
            "AccountReference CompleteRegistration(NewAccount account)",
            "void ChangePassword(string sessionToken, string currentPassword, "
                + "string newPassword)",
            "void ResetPassword(string emailAddress, string verificationCode, "
                + "string newPassword)",
            "void DeactivateAccount(string sessionToken, string currentPassword)",
        },
        ["IPlayerProfileService"] = new[]
        {
            "PlayerProfile GetPlayerProfile(string sessionToken, long? targetAccountId)",
            "ImageContent? GetProfileImage(string sessionToken, long? targetAccountId)",
            "void ChangeUsername(string sessionToken, string username)",
            "void UpdateProfileImage(string sessionToken, ImageContent image)",
        },
        ["IPlayerPreferencesService"] = new[]
        {
            "PlayerPreferences GetPlayerPreferences(string sessionToken)",
            "void UpdatePlayerPreferences(string sessionToken, "
                + "PlayerPreferences preferences)",
            "ImageContent? GetBoardBackground(string sessionToken)",
            "void ChangeBoardBackground(string sessionToken, ImageContent image)",
        },
        ["ILeaderboardService"] = new[]
        {
            "LeaderboardEntry[] GetLeaderboard(int maximumEntries)",
        },
        ["IFriendshipService"] = new[]
        {
            "void OpenFriendshipChannel(string sessionToken)",
            "PlayerSummary[] FindPlayers(string sessionToken, string searchText)",
            "FriendRequest SendFriendRequest(string sessionToken, long recipientAccountId)",
            "FriendRequest[] GetPendingFriendRequests(string sessionToken)",
            "void RespondToFriendRequest(string sessionToken, string requestId, "
                + "bool accept)",
            "FriendSummary[] GetFriends(string sessionToken)",
            "void RemoveFriend(string sessionToken, long friendAccountId)",
        },
        ["IFriendshipServiceCallback"] = new[]
        {
            "void OnFriendRequestReceived(FriendRequest request)",
            "void OnFriendRequestResponded(string requestId, bool accepted)",
            "void OnFriendChanged(long accountId, FriendChange change)",
        },
        ["IMatchLobbyService"] = new[]
        {
            "void OpenLobbyChannel(string sessionToken)",
            "LobbySnapshot CreateMatch(string sessionToken, "
                + "MatchConfiguration configuration)",
            "AvailableMatch[] GetAvailableMatches(string sessionToken)",
            "LobbySnapshot JoinMatch(string sessionToken, string matchCode)",
            "LobbySnapshot ChangeTurnDuration(string sessionToken, long matchId, "
                + "int turnDurationSeconds)",
            "MatchStateSnapshot StartSystemMatch(string sessionToken, long matchId)",
            "LobbySnapshot InviteToMatch(string sessionToken, long friendAccountId, "
                + "MatchConfiguration configuration)",
            "void CancelMatch(string sessionToken, long matchId)",
        },
        ["IMatchLobbyServiceCallback"] = new[]
        {
            "void OnMatchStarted(MatchStateSnapshot state)",
            "void OnLobbyClosed(long matchId)",
        },
        ["IGameplayMatchService"] = new[]
        {
            "void OpenMatchChannel(string sessionToken, long matchId)",
            "MatchInformation GetMatchInformation(string sessionToken, long matchId)",
            "MatchStateSnapshot GetMatchState(string sessionToken, long matchId)",
            "MatchStateSnapshot PlacePiece(string sessionToken, PiecePlacement placement)",
            "MatchConclusion ForfeitMatch(string sessionToken, long matchId)",
            "MatchStateSnapshot ObserveMatch(string sessionToken, long matchId)",
            "MatchSummary[] GetMatchHistory(string sessionToken)",
            "MatchDetails GetMatchDetails(string sessionToken, long matchId)",
            "MoveRecord[] GetMoveHistory(string sessionToken, long matchId)",
        },
        ["IGameplayMatchServiceCallback"] = new[]
        {
            "void OnMatchStateChanged(MatchStateSnapshot state)",
            "void OnParticipantConnectionChanged(long matchId, int seatNumber, "
                + "bool isConnected)",
        },
        ["IMatchChatService"] = new[]
        {
            "void OpenChatChannel(string sessionToken, long matchId)",
            "MatchChatEntry SendChatMessage(string sessionToken, long matchId, "
                + "string content)",
            "MatchChatEntry[] GetChatHistory(string sessionToken, long matchId)",
        },
        ["IMatchChatServiceCallback"] = new[]
        {
            "void OnChatMessageReceived(MatchChatEntry message)",
            "void OnPlayerMuted(ChatMuteNotification mute)",
        },
    };

    internal static readonly Dictionary<string, string[]> Members = new()
    {
        ["AccountReference"] = new[]
        {
            "long AccountId",
            "string Username",
        },
        ["AuthenticatedSession"] = new[]
        {
            "string SessionToken",
            "AccountReference Account",
            "string EmailAddress",
            "string PreferredLocaleCode",
            "DateTime ExpiresAtUtc",
        },
        ["GuestSession"] = new[]
        {
            "string SessionToken",
            "string DisplayName",
            "DateTime ExpiresAtUtc",
        },
        ["NewAccount"] = new[]
        {
            "string VerificationCode",
            "string Username",
            "string EmailAddress",
            "string Password",
            "string PreferredLocaleCode",
            "ImageContent? ProfileImage",
        },
        ["ImageContent"] = new[]
        {
            "string MediaType",
            "byte[] Content",
        },
        ["PlayerProfile"] = new[]
        {
            "AccountReference Account",
            "bool HasProfileImage",
            "PlayerStatistics Statistics",
        },
        ["PlayerStatistics"] = new[]
        {
            "int MatchesPlayed",
            "int Wins",
            "int Losses",
            "int Draws",
            "double WinRate",
        },
        ["PlayerPreferences"] = new[]
        {
            "string PreferredLocaleCode",
            "string? TimeZoneId",
            "string PieceStyleCode",
            "bool UseCustomBoardBackground",
        },
        ["LeaderboardEntry"] = new[]
        {
            "int Position",
            "AccountReference Account",
            "bool HasProfileImage",
            "int MatchesPlayed",
            "int Wins",
            "double WinRate",
        },
        ["PlayerSummary"] = new[]
        {
            "AccountReference Account",
            "bool HasProfileImage",
        },
        ["FriendRequest"] = new[]
        {
            "string RequestId",
            "AccountReference Sender",
            "AccountReference Recipient",
            "DateTime CreatedAtUtc",
        },
        ["FriendSummary"] = new[]
        {
            "AccountReference Account",
            "bool HasProfileImage",
            "bool IsOnline",
        },
        ["MatchConfiguration"] = new[]
        {
            "int TurnDurationSeconds",
            "OpponentKind OpponentKind",
            "bool CountsTowardStatistics",
        },
        ["AvailableMatch"] = new[]
        {
            "string MatchCode",
            "string CreatorDisplayName",
            "int TurnDurationSeconds",
        },
        ["LobbySnapshot"] = new[]
        {
            "long MatchId",
            "string? MatchCode",
            "MatchParticipantSummary[] Participants",
            "MatchConfiguration Configuration",
            "MatchState State",
        },
        ["MatchParticipantSummary"] = new[]
        {
            "int SeatNumber",
            "ParticipantKind Kind",
            "long? AccountId",
            "string DisplayName",
        },
        ["PiecePlacement"] = new[]
        {
            "long MatchId",
            "int Row",
            "int Column",
            "long ExpectedMatchStateVersion",
        },
        ["MatchInformation"] = new[]
        {
            "long MatchId",
            "MatchParticipantSummary[] Participants",
            "int? AssignedSeatNumber",
            "int TurnDurationSeconds",
        },
        ["MatchStateSnapshot"] = new[]
        {
            "long MatchId",
            "long MatchStateVersion",
            "MatchState State",
            "BoardCellState[] Board",
            "int BlackPieceCount",
            "int WhitePieceCount",
            "int? CurrentTurnSeatNumber",
            "DateTime? TurnStartedAtUtc",
            "MoveRecord? LastMove",
            "int? SkippedSeatNumber",
            "MatchConclusion? Conclusion",
        },
        ["MoveRecord"] = new[]
        {
            "int MoveNumber",
            "int SeatNumber",
            "int Row",
            "int Column",
            "DateTime PlayedAtUtc",
            "int FlippedPieceCount",
            "MoveOrigin Origin",
        },
        ["MatchConclusion"] = new[]
        {
            "long MatchId",
            "long MatchStateVersion",
            "MatchEndReason Reason",
            "int? WinnerSeatNumber",
            "int? AbandoningSeatNumber",
            "int BlackPieceCount",
            "int WhitePieceCount",
            "DateTime EndedAtUtc",
        },
        ["MatchSummary"] = new[]
        {
            "long MatchId",
            "MatchParticipantSummary[] Participants",
            "int AssignedSeatNumber",
            "MatchState State",
            "DateTime RecordedAtUtc",
            "int BlackPieceCount",
            "int WhitePieceCount",
            "MatchConclusion? Conclusion",
        },
        ["MatchDetails"] = new[]
        {
            "long MatchId",
            "long MatchStateVersion",
            "MatchParticipantSummary[] Participants",
            "int TurnDurationSeconds",
            "MatchState State",
            "DateTime RecordedAtUtc",
            "MatchConclusion? Conclusion",
            "MoveRecord[] Moves",
            "BoardCellState[][] BoardFrames",
        },
        ["MatchChatEntry"] = new[]
        {
            "long MatchId",
            "int MessageNumber",
            "int SenderSeatNumber",
            "string Content",
            "DateTime SentAtUtc",
        },
        ["ChatMuteNotification"] = new[]
        {
            "long MatchId",
            "string ReasonCode",
            "DateTime StartsAtUtc",
            "DateTime EndsAtUtc",
        },
        ["SessionFault"] = new[]
        {
            "SessionFailure Reason",
        },
        ["AccountFault"] = new[]
        {
            "AccountFailure Reason",
        },
        ["ProfileFault"] = new[]
        {
            "ProfileFailure Reason",
        },
        ["PreferencesFault"] = new[]
        {
            "PreferencesFailure Reason",
        },
        ["LeaderboardFault"] = new[]
        {
            "LeaderboardFailure Reason",
        },
        ["FriendshipFault"] = new[]
        {
            "FriendshipFailure Reason",
        },
        ["MatchFault"] = new[]
        {
            "MatchFailure Reason",
        },
        ["ChatFault"] = new[]
        {
            "ChatFailure Reason",
            "DateTime? MuteEndsAtUtc",
        },
        ["ServiceFault"] = new[]
        {
            "string FailureCode",
            "string CorrelationId",
            "bool IsTransient",
        },
    };

    internal static readonly Dictionary<string, string[]> EnumValues = new()
    {
        ["SessionFailure"] = new[]
        {
            "InvalidCredentials",
            "InactiveAccount",
            "InvalidVerificationCode",
            "ExpiredVerificationCode",
            "InvalidGuestName",
            "GuestNameUnavailable",
            "MissingSessionToken",
            "InvalidSession",
        },
        ["AccountFailure"] = new[]
        {
            "MissingRegistrationData",
            "InvalidUsername",
            "InvalidEmailAddress",
            "InvalidPassword",
            "InvalidLocale",
            "InvalidImage",
            "UsernameUnavailable",
            "EmailUnavailable",
            "InvalidVerificationCode",
            "ExpiredVerificationCode",
            "IncorrectCurrentPassword",
            "InactiveAccount",
            "ActiveMatchInProgress",
        },
        ["ProfileFailure"] = new[]
        {
            "InvalidAccountId",
            "AccountNotFound",
            "InvalidUsername",
            "UsernameUnavailable",
            "InvalidImage",
            "InactiveAccount",
        },
        ["PreferencesFailure"] = new[]
        {
            "MissingPreferences",
            "InvalidLocale",
            "InvalidTimeZone",
            "InvalidPieceStyle",
            "InvalidImage",
            "InactiveAccount",
        },
        ["LeaderboardFailure"] = new[]
        {
            "InvalidMaximumEntries",
        },
        ["FriendshipFailure"] = new[]
        {
            "InvalidAccountId",
            "CannotSendRequestToSelf",
            "AccountUnavailable",
            "FriendshipAlreadyExists",
            "RequestAlreadyPending",
            "MissingRequestId",
            "RequestUnavailable",
            "UnauthorizedRequest",
            "FriendshipNotFound",
            "InactiveAccount",
        },
        ["MatchFailure"] = new[]
        {
            "InvalidMatchId",
            "InvalidConfiguration",
            "InvalidTurnDuration",
            "MatchNotFound",
            "CannotJoinOwnMatch",
            "InvalidMatchCode",
            "MatchCodeUnavailable",
            "Unauthorized",
            "MatchNotWaiting",
            "InactiveAccount",
            "MissingPlacement",
            "InvalidMatchStateVersion",
            "StaleMatchStateVersion",
            "MatchNotInProgress",
            "NotPlayersTurn",
            "InvalidPosition",
            "SpectatingUnavailable",
        },
        ["ChatFailure"] = new[]
        {
            "EmptyMessage",
            "MessageTooLong",
            "MessageBlocked",
            "PlayerMuted",
        },
        ["OpponentKind"] = new[]
        {
            "Human",
            "System",
        },
        ["ParticipantKind"] = new[]
        {
            "Registered",
            "Guest",
            "System",
        },
        ["MatchState"] = new[]
        {
            "Waiting",
            "InProgress",
            "Finished",
            "Cancelled",
        },
        ["BoardCellState"] = new[]
        {
            "Empty",
            "Black",
            "White",
        },
        ["MoveOrigin"] = new[]
        {
            "Manual",
            "Timeout",
            "System",
        },
        ["MatchEndReason"] = new[]
        {
            "NoLegalMoves",
            "Inactivity",
            "Abandonment",
        },
        ["FriendChange"] = new[]
        {
            "Connected",
            "Disconnected",
            "Removed",
        },
    };
}
