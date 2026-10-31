using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

/// <summary>Verifies application rejections and technical failures at the match boundary.</summary>
internal sealed class MatchBoundaryTests
{
    internal void Gameplay_UnauthorizedParticipant_PreservesDeclaredRejection()
    {
        BoundaryStore store = new BoundaryStore();
        using GameplayMatchService service = CreateGameplay(store);
        FaultException<MatchFault> failure = TestAssert.Throws<FaultException<MatchFault>>(
            () => service.GetMatchInformation("valid", 7));
        TestAssert.AreEqual(MatchFailure.Unauthorized, failure.Detail.Reason);
    }

    internal void Gameplay_InitialState_UsesApplicationSnapshot()
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

    internal void Gameplay_UnsupportedWrite_ReturnsDeclaredServiceFault()
    {
        using GameplayMatchService service = CreateGameplay(new BoundaryStore());
        FaultException<ServiceFault> failure = TestAssert.Throws<FaultException<ServiceFault>>(
            () => service.PlacePiece("valid", new PiecePlacement { MatchId = 7 }));
        TestAssert.AreEqual("GAMEPLAY_OPERATION_FAILURE", failure.Detail.FailureCode);
        TestAssert.IsTrue(!string.IsNullOrWhiteSpace(failure.Detail.CorrelationId), "Missing correlation.");
    }

    internal void Lobby_StoreFailure_ReturnsSanitizedServiceFault()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        MatchHandler handler = new MatchHandler(new BoundarySessions(), store, new BoundaryClock());
        using MatchLobbyService service = new MatchLobbyService(handler, NullLogger<MatchLobbyService>.Instance);

        // Act & Assert
        FaultException<ServiceFault> failure = TestAssert.Throws<FaultException<ServiceFault>>(
            () => service.GetAvailableMatches("valid"));
        TestAssert.AreEqual("MATCH_LOBBY_OPERATION_FAILURE", failure.Detail.FailureCode);
        TestAssert.IsTrue(!failure.Message.Contains("private storage failure"), "Leaked storage details.");
    }

    internal void Lobby_InviteToMatch_GuestSession_ReturnsMatchFaultUnauthorized()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        BoundarySessions sessions = new BoundarySessions();
        SessionRegistry registry = new SessionRegistry(sessions, new BoundaryClock());
        GuestSessionRecord guest = registry.CreateGuest("GuestBoundary");
        MatchHandler handler = new MatchHandler(sessions, store, new BoundaryClock(), registry, new BoundaryMailer());
        using MatchLobbyService service = new MatchLobbyService(handler, NullLogger<MatchLobbyService>.Instance);

        // Act & Assert
        FaultException<MatchFault> failure = TestAssert.Throws<FaultException<MatchFault>>(
            () => service.InviteToMatch(guest.SessionToken, 10, new MatchConfiguration { TurnDurationSeconds = 40 }));
        TestAssert.AreEqual(MatchFailure.Unauthorized, failure.Detail.Reason);
    }

    internal void Lobby_InviteToMatch_SelfInvitation_ReturnsMatchFaultUnauthorized()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        BoundarySessions sessions = new BoundarySessions();
        MatchHandler handler = new MatchHandler(sessions, store, new BoundaryClock(), null, new BoundaryMailer());
        using MatchLobbyService service = new MatchLobbyService(handler, NullLogger<MatchLobbyService>.Instance);

        // Act & Assert
        FaultException<MatchFault> failure = TestAssert.Throws<FaultException<MatchFault>>(
            () => service.InviteToMatch("valid", 1, new MatchConfiguration { TurnDurationSeconds = 40 }));
        TestAssert.AreEqual(MatchFailure.Unauthorized, failure.Detail.Reason);
    }

    internal void Lobby_InviteToMatch_InvalidFriendId_ReturnsFriendshipFaultInvalidAccountId()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        BoundarySessions sessions = new BoundarySessions();
        MatchHandler handler = new MatchHandler(sessions, store, new BoundaryClock(), null, new BoundaryMailer());
        using MatchLobbyService service = new MatchLobbyService(handler, NullLogger<MatchLobbyService>.Instance);

        // Act & Assert
        FaultException<FriendshipFault> failure = TestAssert.Throws<FaultException<FriendshipFault>>(
            () => service.InviteToMatch("valid", 0, new MatchConfiguration { TurnDurationSeconds = 40 }));
        TestAssert.AreEqual(FriendshipFailure.InvalidAccountId, failure.Detail.Reason);
    }

    internal void Lobby_InviteToMatch_AccountUnavailable_ReturnsFriendshipFaultAccountUnavailable()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        BoundarySessions sessions = new BoundarySessions();
        MatchHandler handler = new MatchHandler(sessions, store, new BoundaryClock(), null, new BoundaryMailer());
        using MatchLobbyService service = new MatchLobbyService(handler, NullLogger<MatchLobbyService>.Instance);

        // Act & Assert
        FaultException<FriendshipFault> failure = TestAssert.Throws<FaultException<FriendshipFault>>(
            () => service.InviteToMatch("valid", 2, new MatchConfiguration { TurnDurationSeconds = 40 }));
        TestAssert.AreEqual(FriendshipFailure.AccountUnavailable, failure.Detail.Reason);
    }

    internal void Lobby_InviteToMatch_FriendshipNotFound_ReturnsFriendshipFaultFriendshipNotFound()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        BoundarySessions sessions = new BoundarySessions();
        MatchHandler handler = new MatchHandler(sessions, store, new BoundaryClock(), null, new BoundaryMailer());
        using MatchLobbyService service = new MatchLobbyService(handler, NullLogger<MatchLobbyService>.Instance);

        // Act & Assert
        FaultException<FriendshipFault> failure = TestAssert.Throws<FaultException<FriendshipFault>>(
            () => service.InviteToMatch("valid", 3, new MatchConfiguration { TurnDurationSeconds = 40 }));
        TestAssert.AreEqual(FriendshipFailure.FriendshipNotFound, failure.Detail.Reason);
    }

    internal void Lobby_InviteToMatch_StoreFailure_ReturnsSanitizedServiceFault()
    {
        // Arrange
        BoundaryStore store = new BoundaryStore();
        BoundarySessions sessions = new BoundarySessions();
        MatchHandler handler = new MatchHandler(sessions, store, new BoundaryClock(), null, new BoundaryMailer());
        using MatchLobbyService service = new MatchLobbyService(handler, NullLogger<MatchLobbyService>.Instance);

        // Act & Assert
        FaultException<ServiceFault> failure = TestAssert.Throws<FaultException<ServiceFault>>(
            () => service.InviteToMatch("valid", 999, new MatchConfiguration { TurnDurationSeconds = 40 }));
        TestAssert.AreEqual("MATCH_LOBBY_OPERATION_FAILURE", failure.Detail.FailureCode);
        TestAssert.IsTrue(!failure.Message.Contains("private storage failure"), "Leaked storage details.");
    }

    private static GameplayMatchService CreateGameplay(BoundaryStore store)
    {
        BoundarySessions sessions = new BoundarySessions();
        return new GameplayMatchService(new MatchHandler(sessions, store, new BoundaryClock()),
            sessions, NullLogger<GameplayMatchService>.Instance);
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

    private sealed class BoundarySessions : IAuthenticatedSessionStore
    {
        public AuthenticatedSessionRecord Create(LoginAccount account) => throw new NotSupportedException();
        public void Revoke(string sessionToken) => throw new NotSupportedException();
        public bool TryGet(string sessionToken, out AuthenticatedSessionRecord? session)
        {
            session = new AuthenticatedSessionRecord("valid", 1, "BoundaryPlayer", "test@example.invalid", "en-US",
                DateTime.UtcNow.AddDays(1));
            return sessionToken == "valid";
        }
    }

    private sealed class BoundaryStore : IMatchStore
    {
        public void Authorize(long accountId) =>
            throw new InvalidOperationException("private storage failure");
        internal bool AllowRead { get; set; }
        public MatchRecord Create(AuthenticatedSessionRecord player, MatchSettings settings, DateTime now) => throw new NotSupportedException();
        public MatchRecord[] Available(long accountId) => throw new InvalidOperationException("private storage failure");
        public MatchRecord Join(AuthenticatedSessionRecord player, string code, DateTime now) => throw new NotSupportedException();
        public MatchRecord ChangeTurnDuration(long accountId, long matchId, int seconds) => throw new NotSupportedException();
        public void Cancel(long accountId, long matchId) => throw new NotSupportedException();
        public MatchRecord Get(long accountId, long matchId)
        {
            if (!AllowRead)
            {
                throw new MatchRejectedException(MatchRejection.Unauthorized);
            }
            return new MatchRecord
            {
                Id = matchId, State = "InProgress", TurnDurationSeconds = 40,
                StartedAtUtc = new BoundaryClock().UtcNow,
                Participants = new[] { new ParticipantRecord { AccountId = 1, SeatNumber = 1 } }
            };
        }

        public PrivateMatchInvitationPreparation PreparePrivate(
            AuthenticatedSessionRecord creator,
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
                throw new FriendshipRejectedException(FriendshipRejection.AccountUnavailable);
            }
            if (friendAccountId == 3)
            {
                throw new FriendshipRejectedException(FriendshipRejection.FriendshipNotFound);
            }
            if (friendAccountId == 4)
            {
                throw new FriendshipRejectedException(FriendshipRejection.InvalidAccountId);
            }
            if (friendAccountId == 5)
            {
                throw new FriendshipRejectedException(FriendshipRejection.InactiveAccount);
            }
            MatchRecord match = new MatchRecord
            {
                Id = 77,
                Code = "REV123",
                State = "Waiting",
                TurnDurationSeconds = settings.TurnDurationSeconds,
                IsPrivate = true,
                Participants = new[]
                {
                    new ParticipantRecord
                    {
                        AccountId = creator.AccountId,
                        SeatNumber = 1,
                        DisplayName = creator.Username
                    }
                }
            };
            return new PrivateMatchInvitationPreparation(match, "friend@example.com");
        }

        public MatchRecord ConfirmPrivate(
            long accountId,
            long matchId,
            DateTime now)
        {
            return new MatchRecord
            {
                Id = matchId,
                Code = "REV123",
                State = "Waiting",
                TurnDurationSeconds = 40,
                IsPrivate = true,
                Participants = new[]
                {
                    new ParticipantRecord
                    {
                        AccountId = accountId,
                        SeatNumber = 1,
                        DisplayName = "BoundaryPlayer"
                    }
                }
            };
        }

        public void ValidateJoinForGuest(string matchCode)
        {
            if (matchCode == "REV123")
            {
                throw new MatchRejectedException(MatchRejection.Unauthorized);
            }
        }
    }
}
