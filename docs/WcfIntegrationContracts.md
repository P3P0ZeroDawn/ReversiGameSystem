# Catálogo de contratos WCF

## Namespaces

| Uso | Namespace |
| --- | --- |
| Servicios y callbacks | `urn:reversi:services:v1` |
| Datos y faults funcionales | `urn:reversi:contracts:v1` |
| Fault técnico | `urn:reversi:diagnostics:v1` |

## Servicios

### `ISessionService`

```csharp
DateTime RequestLoginCode(string loginIdentifier, string password);
AuthenticatedSession CompleteLogin(string loginIdentifier, string verificationCode);
GuestSession OpenGuestSession(string displayName);
void CloseSession(string sessionToken);
```

Todas las operaciones declaran `SessionFault` y `ServiceFault`.

### `IAccountService`

```csharp
DateTime RequestRegistrationCode(string username, string emailAddress);
AccountReference CompleteRegistration(NewAccount account);
void ChangePassword(string sessionToken, string currentPassword, string newPassword);
DateTime RequestPasswordResetCode(string emailAddress);
void ResetPassword(string emailAddress, string verificationCode, string newPassword);
void DeactivateAccount(string sessionToken, string currentPassword);
```

| Operación | Faults funcionales |
| --- | --- |
| `RequestRegistrationCode` | `AccountFault` |
| `CompleteRegistration` | `AccountFault` |
| `ChangePassword` | `AccountFault`, `SessionFault` |
| `RequestPasswordResetCode` | `AccountFault` |
| `ResetPassword` | `AccountFault` |
| `DeactivateAccount` | `AccountFault`, `SessionFault` |

Todas las operaciones declaran `ServiceFault`.

`RequestPasswordResetCode` devuelve un vencimiento plausible para todo correo con formato
válido. El resultado no revela si existe una cuenta activa.

### `IPlayerProfileService`

```csharp
PlayerProfile GetPlayerProfile(string sessionToken, long? targetAccountId);
ImageContent? GetProfileImage(string sessionToken, long? targetAccountId);
void ChangeUsername(string sessionToken, string username);
void UpdateProfileImage(string sessionToken, ImageContent image);
```

Todas las operaciones declaran `ProfileFault`, `SessionFault` y `ServiceFault`.

### `IPlayerPreferencesService`

```csharp
PlayerPreferences GetPlayerPreferences(string sessionToken);
void UpdatePlayerPreferences(string sessionToken, PlayerPreferences preferences);
ImageContent? GetBoardBackground(string sessionToken);
void ChangeBoardBackground(string sessionToken, ImageContent image);
```

Todas las operaciones declaran `PreferencesFault`, `SessionFault` y `ServiceFault`.

### `ILeaderboardService`

```csharp
LeaderboardEntry[] GetLeaderboard(int maximumEntries);
```

La operación declara `LeaderboardFault` y `ServiceFault`.

### `IFriendshipService`

Callback: `IFriendshipServiceCallback`.

```csharp
void OpenFriendshipChannel(string sessionToken);
PlayerSummary[] FindPlayers(string sessionToken, string searchText);
FriendRequest SendFriendRequest(string sessionToken, long recipientAccountId);
FriendRequest[] GetPendingFriendRequests(string sessionToken);
void RespondToFriendRequest(string sessionToken, string requestId, bool accept);
FriendSummary[] GetFriends(string sessionToken);
void RemoveFriend(string sessionToken, long friendAccountId);
```

Todas las operaciones declaran `FriendshipFault`, `SessionFault` y `ServiceFault`.

### `IMatchLobbyService`

Callback: `IMatchLobbyServiceCallback`.

```csharp
void OpenLobbyChannel(string sessionToken);
LobbySnapshot CreateMatch(string sessionToken, MatchConfiguration configuration);
LobbySnapshot InviteToMatch(
    string sessionToken,
    long friendAccountId,
    MatchConfiguration configuration);
AvailableMatch[] GetAvailableMatches(string sessionToken);
LobbySnapshot JoinMatch(string sessionToken, string matchCode);
LobbySnapshot ChangeTurnDuration(
    string sessionToken,
    long matchId,
    int turnDurationSeconds);
MatchStateSnapshot StartSystemMatch(string sessionToken, long matchId);
void CancelMatch(string sessionToken, long matchId);
```

Todas las operaciones declaran `MatchFault`, `SessionFault` y `ServiceFault`.
`InviteToMatch` declara además `FriendshipFault`.

### `IGameplayMatchService`

Callback: `IGameplayMatchServiceCallback`.

```csharp
void OpenMatchChannel(string sessionToken, long matchId);
MatchInformation GetMatchInformation(string sessionToken, long matchId);
MatchStateSnapshot GetMatchState(string sessionToken, long matchId);
MatchStateSnapshot PlacePiece(string sessionToken, PiecePlacement placement);
MatchConclusion ForfeitMatch(string sessionToken, long matchId);
MatchStateSnapshot ObserveMatch(string sessionToken, long matchId);
MatchSummary[] GetMatchHistory(string sessionToken);
MatchDetails GetMatchDetails(string sessionToken, long matchId);
MoveRecord[] GetMoveHistory(string sessionToken, long matchId);
```

Todas las operaciones declaran `MatchFault`, `SessionFault` y `ServiceFault`.

### `IMatchChatService`

Callback: `IMatchChatServiceCallback`.

```csharp
void OpenChatChannel(string sessionToken, long matchId);
MatchChatEntry SendChatMessage(string sessionToken, long matchId, string content);
MatchChatEntry[] GetChatHistory(string sessionToken, long matchId);
```

Todas las operaciones declaran `ChatFault`, `MatchFault`, `SessionFault` y `ServiceFault`.

## Callbacks

Todas las operaciones de callback son one-way.

```csharp
public interface IFriendshipServiceCallback
{
    void OnFriendRequestReceived(FriendRequest request);
    void OnFriendRequestResponded(string requestId, bool accepted);
    void OnFriendChanged(long accountId, FriendChange change);
}

public interface IMatchLobbyServiceCallback
{
    void OnMatchStarted(MatchStateSnapshot state);
    void OnLobbyClosed(long matchId);
}

public interface IGameplayMatchServiceCallback
{
    void OnMatchStateChanged(MatchStateSnapshot state);
    void OnParticipantConnectionChanged(long matchId, int seatNumber, bool isConnected);
}

public interface IMatchChatServiceCallback
{
    void OnChatMessageReceived(MatchChatEntry message);
    void OnPlayerMuted(ChatMuteNotification mute);
}
```

## Datos serializados

Los miembros aparecen en su orden contractual.

| Tipo | Miembros |
| --- | --- |
| `AccountReference` | `long AccountId`, `string Username` |
| `AuthenticatedSession` | `string SessionToken`, `AccountReference Account`, `string EmailAddress`, `string PreferredLocaleCode`, `DateTime ExpiresAtUtc` |
| `GuestSession` | `string SessionToken`, `string DisplayName`, `DateTime ExpiresAtUtc` |
| `NewAccount` | `string VerificationCode`, `string Username`, `string EmailAddress`, `string Password`, `string PreferredLocaleCode`, `ImageContent? ProfileImage` |
| `ImageContent` | `string MediaType`, `byte[] Content` |
| `PlayerProfile` | `AccountReference Account`, `bool HasProfileImage`, `PlayerStatistics Statistics` |
| `PlayerStatistics` | `int MatchesPlayed`, `int Wins`, `int Losses`, `int Draws`, `double WinRate` |
| `PlayerPreferences` | `string PreferredLocaleCode`, `string? TimeZoneId`, `string PieceStyleCode`, `bool UseCustomBoardBackground` |
| `LeaderboardEntry` | `int Position`, `AccountReference Account`, `bool HasProfileImage`, `int MatchesPlayed`, `int Wins`, `double WinRate` |
| `PlayerSummary` | `AccountReference Account`, `bool HasProfileImage` |
| `FriendRequest` | `string RequestId`, `AccountReference Sender`, `AccountReference Recipient`, `DateTime CreatedAtUtc` |
| `FriendSummary` | `AccountReference Account`, `bool HasProfileImage`, `bool IsOnline` |
| `MatchConfiguration` | `int TurnDurationSeconds`, `OpponentKind OpponentKind`, `bool CountsTowardStatistics` |
| `AvailableMatch` | `string MatchCode`, `string CreatorDisplayName`, `int TurnDurationSeconds` |
| `LobbySnapshot` | `long MatchId`, `string? MatchCode`, `MatchParticipantSummary[] Participants`, `MatchConfiguration Configuration`, `MatchState State` |
| `MatchParticipantSummary` | `int SeatNumber`, `ParticipantKind Kind`, `long? AccountId`, `string DisplayName` |
| `PiecePlacement` | `long MatchId`, `int Row`, `int Column`, `long ExpectedMatchStateVersion` |
| `MatchInformation` | `long MatchId`, `MatchParticipantSummary[] Participants`, `int? AssignedSeatNumber`, `int TurnDurationSeconds` |
| `MatchStateSnapshot` | `long MatchId`, `long MatchStateVersion`, `MatchState State`, `BoardCellState[] Board`, `int BlackPieceCount`, `int WhitePieceCount`, `int? CurrentTurnSeatNumber`, `DateTime? TurnStartedAtUtc`, `MoveRecord? LastMove`, `int? SkippedSeatNumber`, `MatchConclusion? Conclusion` |
| `MoveRecord` | `int MoveNumber`, `int SeatNumber`, `int Row`, `int Column`, `DateTime PlayedAtUtc`, `int FlippedPieceCount`, `MoveOrigin Origin` |
| `MatchConclusion` | `long MatchId`, `long MatchStateVersion`, `MatchEndReason Reason`, `int? WinnerSeatNumber`, `int? AbandoningSeatNumber`, `int BlackPieceCount`, `int WhitePieceCount`, `DateTime EndedAtUtc` |
| `MatchSummary` | `long MatchId`, `MatchParticipantSummary[] Participants`, `int AssignedSeatNumber`, `MatchState State`, `DateTime RecordedAtUtc`, `int BlackPieceCount`, `int WhitePieceCount`, `MatchConclusion? Conclusion` |
| `MatchDetails` | `long MatchId`, `long MatchStateVersion`, `MatchParticipantSummary[] Participants`, `int TurnDurationSeconds`, `MatchState State`, `DateTime RecordedAtUtc`, `MatchConclusion? Conclusion`, `MoveRecord[] Moves`, `BoardCellState[][] BoardFrames` |
| `MatchChatEntry` | `long MatchId`, `int MessageNumber`, `int SenderSeatNumber`, `string Content`, `DateTime SentAtUtc` |
| `ChatMuteNotification` | `long MatchId`, `string ReasonCode`, `DateTime StartsAtUtc`, `DateTime EndsAtUtc` |

Las colecciones correctas sin elementos se serializan como arrays vacíos. Los miembros y
retornos opcionales están marcados con `?`. Las fechas que representan instantes usan UTC.

## Faults

| Fault | Miembros |
| --- | --- |
| `SessionFault` | `SessionFailure Reason` |
| `AccountFault` | `AccountFailure Reason` |
| `ProfileFault` | `ProfileFailure Reason` |
| `PreferencesFault` | `PreferencesFailure Reason` |
| `LeaderboardFault` | `LeaderboardFailure Reason` |
| `FriendshipFault` | `FriendshipFailure Reason` |
| `MatchFault` | `MatchFailure Reason` |
| `ChatFault` | `ChatFailure Reason`, `DateTime? MuteEndsAtUtc` |
| `ServiceFault` | `string FailureCode`, `string CorrelationId`, `bool IsTransient` |

## Enumeraciones

| Enumeración | Valores en orden contractual |
| --- | --- |
| `SessionFailure` | `InvalidCredentials`, `InactiveAccount`, `InvalidVerificationCode`, `ExpiredVerificationCode`, `InvalidGuestName`, `GuestNameUnavailable`, `MissingSessionToken`, `InvalidSession` |
| `AccountFailure` | `MissingRegistrationData`, `InvalidUsername`, `InvalidEmailAddress`, `InvalidPassword`, `InvalidLocale`, `InvalidImage`, `UsernameUnavailable`, `EmailUnavailable`, `InvalidVerificationCode`, `ExpiredVerificationCode`, `IncorrectCurrentPassword`, `InactiveAccount`, `ActiveMatchInProgress` |
| `ProfileFailure` | `InvalidAccountId`, `AccountNotFound`, `InvalidUsername`, `UsernameUnavailable`, `InvalidImage`, `InactiveAccount` |
| `PreferencesFailure` | `MissingPreferences`, `InvalidLocale`, `InvalidTimeZone`, `InvalidPieceStyle`, `InvalidImage`, `InactiveAccount` |
| `LeaderboardFailure` | `InvalidMaximumEntries` |
| `FriendshipFailure` | `InvalidAccountId`, `CannotSendRequestToSelf`, `AccountUnavailable`, `FriendshipAlreadyExists`, `RequestAlreadyPending`, `MissingRequestId`, `RequestUnavailable`, `UnauthorizedRequest`, `FriendshipNotFound`, `InactiveAccount` |
| `MatchFailure` | `InvalidMatchId`, `InvalidConfiguration`, `InvalidTurnDuration`, `MatchNotFound`, `CannotJoinOwnMatch`, `InvalidMatchCode`, `MatchCodeUnavailable`, `Unauthorized`, `MatchNotWaiting`, `InactiveAccount`, `MissingPlacement`, `InvalidMatchStateVersion`, `StaleMatchStateVersion`, `MatchNotInProgress`, `NotPlayersTurn`, `InvalidPosition`, `SpectatingUnavailable` |
| `ChatFailure` | `EmptyMessage`, `MessageTooLong`, `MessageBlocked`, `PlayerMuted` |
| `OpponentKind` | `Human`, `System` |
| `ParticipantKind` | `Registered`, `Guest`, `System` |
| `MatchState` | `Waiting`, `InProgress`, `Finished`, `Cancelled` |
| `BoardCellState` | `Empty`, `Black`, `White` |
| `MoveOrigin` | `Manual`, `Timeout`, `System` |
| `MatchEndReason` | `NoLegalMoves`, `Inactivity`, `Abandonment` |
| `FriendChange` | `Connected`, `Disconnected`, `Removed` |
