using System;
using System.Collections.Generic;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.ServiceApi;
using AppGameplayMatchService =
    ReversiGameSystem.Server.Application.Matches.GameplayMatchApplicationService;
using AppMatchService = ReversiGameSystem.Server.Application.Matches.MatchApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application;

/// <summary>Verifies application rejections and technical failures at the match boundary.</summary>
[TestClass]
[TestCategory("Unit")]
public sealed class MatchBoundaryTests
{
    private const string StorageFailureMessage = "private storage failure";
    private const string ValidToken = "valid";

    [TestMethod]
    public void Gameplay_UnauthorizedParticipant_PreservesDeclaredRejection()
    {
        BoundaryStore store = new BoundaryStore();
        GameplayMatchService service = CreateGameplay(store);
        MatchFailure expectedReason = MatchFailure.Unauthorized;

        FaultException<MatchFault> failure = Assert.Throws<FaultException<MatchFault>>(
            () => service.GetMatchInformation(ValidToken, 7));

        Assert.AreEqual(expectedReason, failure.Detail.Reason);
    }

    [TestMethod]
    public void Gameplay_InitialState_UsesApplicationSnapshot()
    {
        BoundaryStore store = new BoundaryStore { AllowRead = true };
        GameplayMatchService service = CreateGameplay(store);
        long expectedMatchId = 7L;
        long expectedVersion = 0L;
        int expectedBoardLength = 64;
        BoardCellState expectedWhiteCell = BoardCellState.White;
        BoardCellState expectedBlackCell = BoardCellState.Black;
        int? expectedTurnSeat = 1;
        int expectedPieceCount = 2;

        MatchStateSnapshot state = service.GetMatchState(ValidToken, expectedMatchId);

        Assert.AreEqual(expectedMatchId, state.MatchId);
        Assert.AreEqual(expectedVersion, state.MatchStateVersion);
        Assert.AreEqual(expectedBoardLength, state.Board.Length);
        Assert.AreEqual(expectedWhiteCell, state.Board[27]);
        Assert.AreEqual(expectedBlackCell, state.Board[28]);
        Assert.AreEqual(expectedTurnSeat, state.CurrentTurnSeatNumber);
        Assert.AreEqual(expectedPieceCount, state.BlackPieceCount);
        Assert.AreEqual(expectedPieceCount, state.WhitePieceCount);
    }

    [TestMethod]
    public void Gameplay_StoreFailure_PropagatesToGlobalHandler()
    {
        BoundaryStore store = new BoundaryStore { FailStorage = true };
        GameplayMatchService service = CreateGameplay(store);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => service.PlacePiece(ValidToken, new PiecePlacement { MatchId = 7 }));

        Assert.IsTrue(
            failure.Message.Contains(StorageFailureMessage),
            "The service must preserve the technical failure for ServiceErrorBehavior.");
    }

    [TestMethod]
    public void Lobby_StoreFailure_PropagatesToGlobalHandler()
    {
        BoundaryStore store = new BoundaryStore();
        AppMatchService handler = new AppMatchService(
            CreateValidSessionStore(),
            store,
            new BoundaryClock());
        MatchLobbyService service = CreateLobby(handler);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => service.GetAvailableMatches(ValidToken));

        Assert.IsTrue(
            failure.Message.Contains(StorageFailureMessage),
            "The service must preserve the technical failure for ServiceErrorBehavior.");
    }

    [TestMethod]
    public void InviteToMatch_GuestSession_ReturnsMatchFaultUnauthorized()
    {
        BoundaryStore store = new BoundaryStore();
        BoundaryClock clock = new BoundaryClock();
        SessionStore sessions = new SessionStore(clock, NullLogger<SessionStore>.Instance);
        GuestSession guest = new GuestSession(
            "guest-token",
            "Guest-GuestBoundary",
            sessions.CalculateExpirationUtc());
        sessions.TryReserveGuest(guest);
        AppMatchService handler = new AppMatchService(
            sessions,
            store,
            clock,
            new BoundaryMailer());
        MatchLobbyService service = CreateLobby(handler);
        MatchFailure expectedReason = MatchFailure.Unauthorized;

        FaultException<MatchFault> failure = Assert.Throws<FaultException<MatchFault>>(
            () => service.InviteToMatch(
                guest.SessionToken,
                10,
                new MatchConfiguration { TurnDurationSeconds = 40 }));

        Assert.AreEqual(expectedReason, failure.Detail.Reason);
    }

    [TestMethod]
    public void InviteToMatch_SelfInvitation_ReturnsMatchFaultUnauthorized()
    {
        BoundaryStore store = new BoundaryStore();
        SessionStore sessions = CreateValidSessionStore();
        AppMatchService handler = new AppMatchService(
            sessions,
            store,
            new BoundaryClock(),
            new BoundaryMailer());
        MatchLobbyService service = CreateLobby(handler);
        MatchFailure expectedReason = MatchFailure.Unauthorized;

        FaultException<MatchFault> failure = Assert.Throws<FaultException<MatchFault>>(
            () => service.InviteToMatch(
                ValidToken,
                1,
                new MatchConfiguration { TurnDurationSeconds = 40 }));

        Assert.AreEqual(expectedReason, failure.Detail.Reason);
    }

    [TestMethod]
    public void InviteToMatch_InvalidFriendId_ReturnsFriendshipFaultInvalidAccountId()
    {
        BoundaryStore store = new BoundaryStore();
        SessionStore sessions = CreateValidSessionStore();
        AppMatchService handler = new AppMatchService(
            sessions,
            store,
            new BoundaryClock(),
            new BoundaryMailer());
        MatchLobbyService service = CreateLobby(handler);
        FriendshipFailure expectedReason = FriendshipFailure.InvalidAccountId;

        FaultException<FriendshipFault> failure = Assert.Throws<FaultException<FriendshipFault>>(
            () => service.InviteToMatch(
                ValidToken,
                0,
                new MatchConfiguration { TurnDurationSeconds = 40 }));

        Assert.AreEqual(expectedReason, failure.Detail.Reason);
    }

    [TestMethod]
    public void InviteToMatch_AccountUnavailable_ReturnsFriendshipFaultAccountUnavailable()
    {
        BoundaryStore store = new BoundaryStore();
        SessionStore sessions = CreateValidSessionStore();
        AppMatchService handler = new AppMatchService(
            sessions,
            store,
            new BoundaryClock(),
            new BoundaryMailer());
        MatchLobbyService service = CreateLobby(handler);
        FriendshipFailure expectedReason = FriendshipFailure.AccountUnavailable;

        FaultException<FriendshipFault> failure = Assert.Throws<FaultException<FriendshipFault>>(
            () => service.InviteToMatch(
                ValidToken,
                2,
                new MatchConfiguration { TurnDurationSeconds = 40 }));

        Assert.AreEqual(expectedReason, failure.Detail.Reason);
    }

    [TestMethod]
    public void InviteToMatch_FriendshipNotFound_ReturnsFriendshipFaultFriendshipNotFound()
    {
        BoundaryStore store = new BoundaryStore();
        SessionStore sessions = CreateValidSessionStore();
        AppMatchService handler = new AppMatchService(
            sessions,
            store,
            new BoundaryClock(),
            new BoundaryMailer());
        MatchLobbyService service = CreateLobby(handler);
        FriendshipFailure expectedReason = FriendshipFailure.FriendshipNotFound;

        FaultException<FriendshipFault> failure = Assert.Throws<FaultException<FriendshipFault>>(
            () => service.InviteToMatch(
                ValidToken,
                3,
                new MatchConfiguration { TurnDurationSeconds = 40 }));

        Assert.AreEqual(expectedReason, failure.Detail.Reason);
    }

    [TestMethod]
    public void InviteToMatch_StoreFailure_PropagatesToGlobalHandler()
    {
        BoundaryStore store = new BoundaryStore();
        SessionStore sessions = CreateValidSessionStore();
        AppMatchService handler = new AppMatchService(
            sessions,
            store,
            new BoundaryClock(),
            new BoundaryMailer());
        MatchLobbyService service = CreateLobby(handler);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => service.InviteToMatch(
                ValidToken,
                999,
                new MatchConfiguration { TurnDurationSeconds = 40 }));

        Assert.IsTrue(
            failure.Message.Contains(StorageFailureMessage),
            "The service must preserve the technical failure for ServiceErrorBehavior.");
    }

    private static GameplayMatchService CreateGameplay(BoundaryStore store)
    {
        SessionStore sessions = CreateValidSessionStore();
        AppGameplayMatchService applicationService =
            new AppGameplayMatchService(sessions, store, new BoundaryClock());
        MatchCallbackSubscriptions<IGameplayMatchServiceCallback> subscriptions =
            new MatchCallbackSubscriptions<IGameplayMatchServiceCallback>(
                (_, _) => true,
                NullLogger.Instance);
        return new GameplayMatchService(
            applicationService,
            new GameplayMatchPresence(),
            subscriptions);
    }

    private static MatchLobbyService CreateLobby(AppMatchService handler)
    {
        MatchCallbackSubscriptions<IMatchLobbyServiceCallback> subscriptions =
            new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
                (_, _) => true,
                NullLogger.Instance);
        return new MatchLobbyService(handler, subscriptions);
    }

    private static SessionStore CreateValidSessionStore()
    {
        SessionStore sessions = new SessionStore(
            new BoundaryClock(),
            NullLogger<SessionStore>.Instance);
        sessions.TryAddAuthenticated(new AuthenticatedSession(
            ValidToken,
            1,
            "BoundaryPlayer",
            "test@example.invalid",
            "en-US",
            DateTime.UtcNow.AddDays(1)));
        return sessions;
    }

    private sealed class BoundaryMailer : ITemplatedEmailSender
    {
        public void SendTemplatedEmail(
            string recipientAddress,
            string templateName,
            IReadOnlyDictionary<string, string> templateValues)
        {
        }
    }

    private sealed class BoundaryClock : IUtcClock
    {
        public DateTime UtcNow
        {
            get { return new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc); }
        }
    }

    private sealed class BoundaryStore : IMatchRepository, IGameplayRepository
    {
        internal bool AllowRead { get; set; }

        internal bool FailStorage { get; set; }

        public MatchResult<Match> StartSystemMatch(MatchPlayer player, long matchId, DateTime now)
        {
            throw new NotSupportedException();
        }

        public MatchStatus EvaluateLobbyAccess(long accountId)
        {
            if (AllowRead)
            {
                return MatchStatus.Success;
            }

            throw new InvalidOperationException(StorageFailureMessage);
        }

        public MatchResult<Match> CreateMatch(
            MatchPlayer player,
            MatchSettings settings,
            DateTime now)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match[]> GetObservableMatches(long accountId)
        {
            throw new InvalidOperationException(StorageFailureMessage);
        }

        public MatchResult<Match[]> GetAvailableMatches(MatchPlayer player)
        {
            throw new InvalidOperationException(StorageFailureMessage);
        }

        public MatchResult<Match> JoinMatch(MatchPlayer player, string code, DateTime now)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match> ChangeTurnDuration(MatchPlayer player, long matchId, int seconds)
        {
            throw new NotSupportedException();
        }

        public MatchStatus CancelMatch(MatchPlayer player, long matchId)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match> LoadParticipantMatch(MatchPlayer player, long matchId)
        {
            if (FailStorage)
            {
                throw new InvalidOperationException(StorageFailureMessage);
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
                throw new InvalidOperationException(StorageFailureMessage);
            }

            if (friendAccountId == 2 || friendAccountId == 4)
            {
                return new MatchResult<MatchInvitationPreparation>(
                    MatchStatus.FriendAccountUnavailable);
            }

            if (friendAccountId == 3)
            {
                return new MatchResult<MatchInvitationPreparation>(
                    MatchStatus.FriendshipNotFound);
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
            MatchInvitationPreparation preparation = new MatchInvitationPreparation(
                match,
                "friend@example.com");
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
            if (FailStorage)
            {
                throw new InvalidOperationException(StorageFailureMessage);
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

        public bool SynchronizeMatch(Match match)
        {
            return true;
        }

        public MatchResult<Match[]> LoadMatchHistory(long accountId)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match> GetForObservation(long accountId, long matchId)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match> LoadMatchForObservation(long accountId, long matchId)
        {
            return LoadMatch(matchId);
        }
    }
}
