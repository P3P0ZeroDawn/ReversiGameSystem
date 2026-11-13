using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.ServiceApi;
using ReversiGameSystem.Server.Persistence.Sessions;

namespace ReversiGameSystem.Server.Tests;

/// <summary>Verifies application rejections and technical failures at the match boundary.</summary>
[TestClass]
public sealed class MatchBoundaryTests
{
    [TestMethod]
    public void Gameplay_UnauthorizedParticipant_PreservesDeclaredRejection()
    {
        BoundaryStore store = new BoundaryStore();
        using GameplayMatchService service = CreateGameplay(store);
        FaultException<MatchFault> failure = TestAssert.Throws<FaultException<MatchFault>>(
            () => service.GetMatchInformation("valid", 7));
        TestAssert.AreEqual(MatchFailure.Unauthorized, failure.Detail.Reason);
    }

    [TestMethod]
    public void Gameplay_InitialState_UsesApplicationSnapshot()
    {
        BoundaryStore store = new BoundaryStore { AllowRead = true };
        using GameplayMatchService service = CreateGameplay(store);
        MatchStateSnapshot state = service.GetMatchState("valid", 7);
        TestAssert.AreEqual(7L, state.MatchId);
        TestAssert.AreEqual(0L, state.MatchStateVersion);
        TestAssert.AreEqual(64, state.Board.Length);
        TestAssert.AreEqual(BoardCellState.White, state.Board[27]);
        TestAssert.AreEqual(BoardCellState.Black, state.Board[28]);
        TestAssert.AreEqual<int?>(1, state.CurrentTurnSeatNumber);
        TestAssert.AreEqual(2, state.BlackPieceCount);
        TestAssert.AreEqual(2, state.WhitePieceCount);
    }

    [TestMethod]
    public void Gameplay_StoreFailure_PropagatesToGlobalHandler()
    {
        BoundaryStore store = new BoundaryStore { FailStorage = true };
        using GameplayMatchService service = CreateGameplay(store);
        InvalidOperationException failure = TestAssert.Throws<InvalidOperationException>(
            () => service.PlacePiece("valid", new PiecePlacement { MatchId = 7 }));
        TestAssert.IsTrue(
            failure.Message.Contains("private storage failure"),
            "The service must preserve the technical failure for ServiceErrorBehavior.");
    }

    [TestMethod]
    public void Lobby_StoreFailure_PropagatesToGlobalHandler()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        MatchApplicationService handler = new MatchApplicationService(new BoundarySessions(), store, new BoundaryClock());
        using MatchLobbyService service = new MatchLobbyService(handler, NullLogger<MatchLobbyService>.Instance);

        // Act & Assert
        InvalidOperationException failure = TestAssert.Throws<InvalidOperationException>(
            () => service.GetAvailableMatches("valid"));
        TestAssert.IsTrue(
            failure.Message.Contains("private storage failure"),
            "The service must preserve the technical failure for ServiceErrorBehavior.");
    }

    [TestMethod]
    public void Lobby_InviteToMatch_GuestSession_ReturnsMatchFaultUnauthorized()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        BoundaryClock clock = new BoundaryClock();
        SessionRepository sessions = new SessionRepository(clock);
        GuestSession guest = sessions.CreateGuest("Guest-GuestBoundary").GetValue();
        MatchApplicationService handler = new MatchApplicationService(sessions, store, clock, new BoundaryMailer());
        using MatchLobbyService service = new MatchLobbyService(handler, NullLogger<MatchLobbyService>.Instance);

        // Act & Assert
        FaultException<MatchFault> failure = TestAssert.Throws<FaultException<MatchFault>>(
            () => service.InviteToMatch(guest.SessionToken, 10, new MatchConfiguration { TurnDurationSeconds = 40 }));
        TestAssert.AreEqual(MatchFailure.Unauthorized, failure.Detail.Reason);
    }

    [TestMethod]
    public void Lobby_InviteToMatch_SelfInvitation_ReturnsMatchFaultUnauthorized()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        BoundarySessions sessions = new BoundarySessions();
        MatchApplicationService handler = new MatchApplicationService(sessions, store, new BoundaryClock(), new BoundaryMailer());
        using MatchLobbyService service = new MatchLobbyService(handler, NullLogger<MatchLobbyService>.Instance);

        // Act & Assert
        FaultException<MatchFault> failure = TestAssert.Throws<FaultException<MatchFault>>(
            () => service.InviteToMatch("valid", 1, new MatchConfiguration { TurnDurationSeconds = 40 }));
        TestAssert.AreEqual(MatchFailure.Unauthorized, failure.Detail.Reason);
    }

    [TestMethod]
    public void Lobby_InviteToMatch_InvalidFriendId_ReturnsFriendshipFaultInvalidAccountId()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        BoundarySessions sessions = new BoundarySessions();
        MatchApplicationService handler = new MatchApplicationService(sessions, store, new BoundaryClock(), new BoundaryMailer());
        using MatchLobbyService service = new MatchLobbyService(handler, NullLogger<MatchLobbyService>.Instance);

        // Act & Assert
        FaultException<FriendshipFault> failure = TestAssert.Throws<FaultException<FriendshipFault>>(
            () => service.InviteToMatch("valid", 0, new MatchConfiguration { TurnDurationSeconds = 40 }));
        TestAssert.AreEqual(FriendshipFailure.InvalidAccountId, failure.Detail.Reason);
    }

    [TestMethod]
    public void Lobby_InviteToMatch_AccountUnavailable_ReturnsFriendshipFaultAccountUnavailable()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        BoundarySessions sessions = new BoundarySessions();
        MatchApplicationService handler = new MatchApplicationService(sessions, store, new BoundaryClock(), new BoundaryMailer());
        using MatchLobbyService service = new MatchLobbyService(handler, NullLogger<MatchLobbyService>.Instance);

        // Act & Assert
        FaultException<FriendshipFault> failure = TestAssert.Throws<FaultException<FriendshipFault>>(
            () => service.InviteToMatch("valid", 2, new MatchConfiguration { TurnDurationSeconds = 40 }));
        TestAssert.AreEqual(FriendshipFailure.AccountUnavailable, failure.Detail.Reason);
    }

    [TestMethod]
    public void Lobby_InviteToMatch_FriendshipNotFound_ReturnsFriendshipFaultFriendshipNotFound()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        BoundarySessions sessions = new BoundarySessions();
        MatchApplicationService handler = new MatchApplicationService(sessions, store, new BoundaryClock(), new BoundaryMailer());
        using MatchLobbyService service = new MatchLobbyService(handler, NullLogger<MatchLobbyService>.Instance);

        // Act & Assert
        FaultException<FriendshipFault> failure = TestAssert.Throws<FaultException<FriendshipFault>>(
            () => service.InviteToMatch("valid", 3, new MatchConfiguration { TurnDurationSeconds = 40 }));
        TestAssert.AreEqual(FriendshipFailure.FriendshipNotFound, failure.Detail.Reason);
    }

    [TestMethod]
    public void Lobby_InviteToMatch_StoreFailure_PropagatesToGlobalHandler()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        BoundarySessions sessions = new BoundarySessions();
        MatchApplicationService handler = new MatchApplicationService(sessions, store, new BoundaryClock(), new BoundaryMailer());
        using MatchLobbyService service = new MatchLobbyService(handler, NullLogger<MatchLobbyService>.Instance);

        // Act & Assert
        InvalidOperationException failure = TestAssert.Throws<InvalidOperationException>(
            () => service.InviteToMatch("valid", 999, new MatchConfiguration { TurnDurationSeconds = 40 }));
        TestAssert.IsTrue(
            failure.Message.Contains("private storage failure"),
            "The service must preserve the technical failure for ServiceErrorBehavior.");
    }

    private static GameplayMatchService CreateGameplay(BoundaryStore store)
    {
        BoundarySessions sessions = new BoundarySessions();
        GameplayMatchApplicationService applicationService =
            new GameplayMatchApplicationService(sessions, store, new BoundaryClock());
        return new GameplayMatchService(applicationService, sessions, NullLogger<GameplayMatchService>.Instance);
    }

    private sealed class BoundaryMailer : IMatchInvitationEmailSender
    {
        public void Send(MatchInvitationEmail email)
        {
        }
    }

    private sealed class BoundaryClock : IUtcClock
    {
        public DateTime UtcNow => new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class BoundarySessions : ISessionRepository
    {
        public AuthenticatedSession Create(LoginAccount account) => throw new NotSupportedException();
        public SessionOperationResult<GuestSession> CreateGuest(string displayName) =>
            throw new NotSupportedException();
        public void Revoke(string sessionToken) => throw new NotSupportedException();
        public bool TryGet(string sessionToken, out AuthenticatedSession? session)
        {
            session = new AuthenticatedSession("valid", 1, "BoundaryPlayer", "test@example.invalid", "en-US",
                DateTime.UtcNow.AddDays(1));
            return sessionToken == "valid";
        }

        public SessionOperationResult<AuthenticatedSession> FindAuthenticated(
            string sessionToken)
        {
            AuthenticatedSession? session;
            if (TryGet(sessionToken, out session) && session is not null)
            {
                return SessionOperationResult<AuthenticatedSession>.CreateSuccess(session);
            }

            return SessionOperationResult<AuthenticatedSession>.CreateFailure(
                SessionOperationStatus.InvalidSession);
        }

        public bool TryGetGuest(string sessionToken, out GuestSession? session)
        {
            session = null;
            return false;
        }
    }

    private sealed class BoundaryStore : IMatchRepository, IGameplayRepository
    {
        public MatchStatus EvaluateLobbyAccess(long accountId)
        {
            throw new InvalidOperationException("private storage failure");
        }
        internal bool AllowRead { get; set; }
        internal bool FailStorage { get; set; }
        public MatchResult<Match> Create(AuthenticatedSession player, MatchSettings settings, DateTime now)
        {
            throw new NotSupportedException();
        }
        public MatchResult<Match[]> Available(long accountId)
        {
            throw new InvalidOperationException("private storage failure");
        }
        public MatchResult<Match> Join(AuthenticatedSession player, string code, DateTime now)
        {
            throw new NotSupportedException();
        }
        public MatchResult<Match> ChangeTurnDuration(long accountId, long matchId, int seconds)
        {
            throw new NotSupportedException();
        }
        public MatchStatus Cancel(long accountId, long matchId)
        {
            throw new NotSupportedException();
        }
        public MatchResult<Match> LoadParticipantMatch(long accountId, long matchId)
        {
            if (FailStorage)
            {
                throw new InvalidOperationException("private storage failure");
            }

            if (!AllowRead)
            {
                return new MatchResult<Match>(MatchStatus.Unauthorized);
            }

            Match match = new Match
            {
                Id = matchId,
                State = "InProgress",
                TurnDurationSeconds = 40,
                StartedAtUtc = new BoundaryClock().UtcNow,
                Participants = new[] { new MatchParticipant { AccountId = 1, SeatNumber = 1 } }
            };
            return new MatchResult<Match>(match);
        }

        public MatchResult<MatchInvitationPreparation> PreparePrivateInvitation(
            AuthenticatedSession creator,
            long friendAccountId,
            MatchSettings settings,
            DateTime now)
        {
            if (friendAccountId == 999)
            {
                throw new InvalidOperationException("private storage failure");
            }
            if (friendAccountId == 2)
            {
                return new MatchResult<MatchInvitationPreparation>(MatchStatus.FriendAccountUnavailable);
            }
            if (friendAccountId == 3)
            {
                return new MatchResult<MatchInvitationPreparation>(MatchStatus.FriendshipNotFound);
            }
            if (friendAccountId == 4)
            {
                return new MatchResult<MatchInvitationPreparation>(MatchStatus.FriendAccountUnavailable);
            }
            if (friendAccountId == 5)
            {
                return new MatchResult<MatchInvitationPreparation>(MatchStatus.InactiveAccount);
            }
            Match match = new Match
            {
                Id = 77,
                Code = "REV123",
                State = "Waiting",
                TurnDurationSeconds = settings.TurnDurationSeconds,
                IsPrivate = true,
                Participants = new[]
                {
                    new MatchParticipant
                    {
                        AccountId = creator.AccountId,
                        SeatNumber = 1,
                        DisplayName = creator.Username
                    }
                }
            };
            MatchInvitationPreparation preparation = new MatchInvitationPreparation(match, "friend@example.com");
            return new MatchResult<MatchInvitationPreparation>(preparation);
        }

        public MatchResult<Match> MarkInvitationDelivered(
            long accountId,
            long matchId,
            DateTime now)
        {
            Match match = new Match
            {
                Id = matchId,
                Code = "REV123",
                State = "Waiting",
                TurnDurationSeconds = 40,
                IsPrivate = true,
                Participants = new[]
                {
                    new MatchParticipant
                    {
                        AccountId = accountId,
                        SeatNumber = 1,
                        DisplayName = "BoundaryPlayer"
                    }
                }
            };
            return new MatchResult<Match>(match);
        }

        public MatchStatus ValidateGuestJoin(string matchCode)
        {
            if (matchCode == "REV123")
            {
                return MatchStatus.Unauthorized;
            }

            return MatchStatus.Success;
        }

        public MatchStatus SaveMove(
            long matchId,
            MatchMove move,
            int consecutiveTimeouts,
            int nextTurnSeatNumber,
            DateTime turnStartedAtUtc)
        {
            throw new NotSupportedException();
        }

        public MatchStatus SaveMoveAndConclude(
            long matchId,
            MatchMove move,
            int consecutiveTimeouts,
            string endReason,
            int? winnerSeatNumber,
            int blackPieceCount,
            int whitePieceCount,
            DateTime endedAtUtc)
        {
            throw new NotSupportedException();
        }

        public MatchStatus ConcludeMatch(
            long matchId,
            string endReason,
            int? winnerSeatNumber,
            int? abandoningSeatNumber,
            int blackPieceCount,
            int whitePieceCount,
            DateTime endedAtUtc)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match> LoadMatch(long matchId)
        {
            return LoadParticipantMatch(0, matchId);
        }

        public bool SynchronizeMatch(Match match)
        {
            return true;
        }

        public MatchResult<Match[]> LoadMatchHistory(long accountId)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match> LoadMatchForObservation(long accountId, long matchId)
        {
            throw new NotSupportedException();
        }
    }
}
