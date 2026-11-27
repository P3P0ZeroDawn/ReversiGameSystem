using System;
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

namespace ReversiGameSystem.Server.Tests;

/// <summary>Verifies application rejections and technical failures at the match boundary.</summary>
[TestClass]
public sealed class MatchBoundaryTests
{
    [TestMethod]
    public void Gameplay_UnauthorizedParticipant_PreservesDeclaredRejection()
    {
        BoundaryStore store = new BoundaryStore();
        GameplayMatchService service = CreateGameplay(store);
        FaultException<MatchFault> failure = Assert.Throws<FaultException<MatchFault>>(
            () => service.GetMatchInformation("valid", 7));
        Assert.AreEqual(MatchFailure.Unauthorized, failure.Detail.Reason);
    }

    [TestMethod]
    public void Gameplay_InitialState_UsesApplicationSnapshot()
    {
        BoundaryStore store = new BoundaryStore { AllowRead = true };
        GameplayMatchService service = CreateGameplay(store);
        MatchStateSnapshot state = service.GetMatchState("valid", 7);
        Assert.AreEqual(7L, state.MatchId);
        Assert.AreEqual(0L, state.MatchStateVersion);
        Assert.AreEqual(64, state.Board.Length);
        Assert.AreEqual(BoardCellState.White, state.Board[27]);
        Assert.AreEqual(BoardCellState.Black, state.Board[28]);
        Assert.AreEqual<int?>(1, state.CurrentTurnSeatNumber);
        Assert.AreEqual(2, state.BlackPieceCount);
        Assert.AreEqual(2, state.WhitePieceCount);
    }

    [TestMethod]
    public void Gameplay_StoreFailure_PropagatesToGlobalHandler()
    {
        BoundaryStore store = new BoundaryStore { FailStorage = true };
        GameplayMatchService service = CreateGameplay(store);
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => service.PlacePiece("valid", new PiecePlacement { MatchId = 7 }));
        Assert.IsTrue(
            failure.Message.Contains("private storage failure"),
            "The service must preserve the technical failure for ServiceErrorBehavior.");
    }

    [TestMethod]
    public void Lobby_StoreFailure_PropagatesToGlobalHandler()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        MatchApplicationService handler = new MatchApplicationService(CreateValidSessionStore(), store, new BoundaryClock());
        MatchLobbyService service = CreateLobby(handler);

        // Act & Assert
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => service.GetAvailableMatches("valid"));
        Assert.IsTrue(
            failure.Message.Contains("private storage failure"),
            "The service must preserve the technical failure for ServiceErrorBehavior.");
    }

    [TestMethod]
    public void Lobby_InviteToMatch_GuestSession_ReturnsMatchFaultUnauthorized()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        BoundaryClock clock = new BoundaryClock();
        SessionStore sessions = new SessionStore(clock);
        GuestSession guest = new GuestSession(
            "guest-token",
            "Guest-GuestBoundary",
            sessions.CalculateExpirationUtc());
        sessions.TryReserveGuest(guest);
        MatchApplicationService handler = new MatchApplicationService(sessions, store, clock, new BoundaryMailer());
        MatchLobbyService service = CreateLobby(handler);

        // Act & Assert
        FaultException<MatchFault> failure = Assert.Throws<FaultException<MatchFault>>(
            () => service.InviteToMatch(guest.SessionToken, 10, new MatchConfiguration { TurnDurationSeconds = 40 }));
        Assert.AreEqual(MatchFailure.Unauthorized, failure.Detail.Reason);
    }

    [TestMethod]
    public void Lobby_InviteToMatch_SelfInvitation_ReturnsMatchFaultUnauthorized()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        SessionStore sessions = CreateValidSessionStore();
        MatchApplicationService handler = new MatchApplicationService(sessions, store, new BoundaryClock(), new BoundaryMailer());
        MatchLobbyService service = CreateLobby(handler);

        // Act & Assert
        FaultException<MatchFault> failure = Assert.Throws<FaultException<MatchFault>>(
            () => service.InviteToMatch("valid", 1, new MatchConfiguration { TurnDurationSeconds = 40 }));
        Assert.AreEqual(MatchFailure.Unauthorized, failure.Detail.Reason);
    }

    [TestMethod]
    public void Lobby_InviteToMatch_InvalidFriendId_ReturnsFriendshipFaultInvalidAccountId()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        SessionStore sessions = CreateValidSessionStore();
        MatchApplicationService handler = new MatchApplicationService(sessions, store, new BoundaryClock(), new BoundaryMailer());
        MatchLobbyService service = CreateLobby(handler);

        // Act & Assert
        FaultException<FriendshipFault> failure = Assert.Throws<FaultException<FriendshipFault>>(
            () => service.InviteToMatch("valid", 0, new MatchConfiguration { TurnDurationSeconds = 40 }));
        Assert.AreEqual(FriendshipFailure.InvalidAccountId, failure.Detail.Reason);
    }

    [TestMethod]
    public void Lobby_InviteToMatch_AccountUnavailable_ReturnsFriendshipFaultAccountUnavailable()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        SessionStore sessions = CreateValidSessionStore();
        MatchApplicationService handler = new MatchApplicationService(sessions, store, new BoundaryClock(), new BoundaryMailer());
        MatchLobbyService service = CreateLobby(handler);

        // Act & Assert
        FaultException<FriendshipFault> failure = Assert.Throws<FaultException<FriendshipFault>>(
            () => service.InviteToMatch("valid", 2, new MatchConfiguration { TurnDurationSeconds = 40 }));
        Assert.AreEqual(FriendshipFailure.AccountUnavailable, failure.Detail.Reason);
    }

    [TestMethod]
    public void Lobby_InviteToMatch_FriendshipNotFound_ReturnsFriendshipFaultFriendshipNotFound()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        SessionStore sessions = CreateValidSessionStore();
        MatchApplicationService handler = new MatchApplicationService(sessions, store, new BoundaryClock(), new BoundaryMailer());
        MatchLobbyService service = CreateLobby(handler);

        // Act & Assert
        FaultException<FriendshipFault> failure = Assert.Throws<FaultException<FriendshipFault>>(
            () => service.InviteToMatch("valid", 3, new MatchConfiguration { TurnDurationSeconds = 40 }));
        Assert.AreEqual(FriendshipFailure.FriendshipNotFound, failure.Detail.Reason);
    }

    [TestMethod]
    public void Lobby_InviteToMatch_StoreFailure_PropagatesToGlobalHandler()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        SessionStore sessions = CreateValidSessionStore();
        MatchApplicationService handler = new MatchApplicationService(sessions, store, new BoundaryClock(), new BoundaryMailer());
        MatchLobbyService service = CreateLobby(handler);

        // Act & Assert
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => service.InviteToMatch("valid", 999, new MatchConfiguration { TurnDurationSeconds = 40 }));
        Assert.IsTrue(
            failure.Message.Contains("private storage failure"),
            "The service must preserve the technical failure for ServiceErrorBehavior.");
    }

    private static GameplayMatchService CreateGameplay(BoundaryStore store)
    {
        SessionStore sessions = CreateValidSessionStore();
        GameplayMatchApplicationService applicationService =
            new GameplayMatchApplicationService(sessions, store, new BoundaryClock());
        MatchCallbackSubscriptions<IGameplayMatchServiceCallback> subscriptions =
            new MatchCallbackSubscriptions<IGameplayMatchServiceCallback>(
                (_, _) => true,
                NullLogger.Instance);
        return new GameplayMatchService(
            applicationService,
            new GameplayMatchPresence(),
            subscriptions);
    }

    private static MatchLobbyService CreateLobby(MatchApplicationService handler)
    {
        MatchCallbackSubscriptions<IMatchLobbyServiceCallback> subscriptions =
            new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
                (_, _) => true,
                NullLogger.Instance);
        return new MatchLobbyService(handler, subscriptions);
    }

    private sealed class BoundaryMailer : ITemplatedEmailSender
    {
        public void SendTemplatedEmail(
            string recipientAddress,
            string templateName,
            System.Collections.Generic.IReadOnlyDictionary<string, string> templateValues)
        {
        }
    }

    private sealed class BoundaryClock : IUtcClock
    {
        public DateTime UtcNow => new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);
    }

    private static SessionStore CreateValidSessionStore()
    {
        SessionStore sessions = new SessionStore(new BoundaryClock());
        sessions.TryAddAuthenticated(new AuthenticatedSession(
            "valid",
            1,
            "BoundaryPlayer",
            "test@example.invalid",
            "en-US",
            DateTime.UtcNow.AddDays(1)));
        return sessions;
    }

    private sealed class BoundaryStore : IMatchRepository, IGameplayRepository
    {
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

            throw new InvalidOperationException("private storage failure");
        }
        internal bool AllowRead { get; set; }
        internal bool FailStorage { get; set; }
        public MatchResult<Match> CreateMatch(MatchPlayer player, MatchSettings settings, DateTime now)
        {
            throw new NotSupportedException();
        }
        public MatchResult<Match[]> GetObservableMatches(long accountId)
        {
            throw new InvalidOperationException("private storage failure");
        }

        public MatchResult<Match[]> GetAvailableMatches(MatchPlayer player)
        {
            throw new InvalidOperationException("private storage failure");
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
