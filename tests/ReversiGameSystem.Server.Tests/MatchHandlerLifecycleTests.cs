using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Tests;

internal sealed class MatchApplicationServiceLifecycleTests
{
    internal void CancelExpiredWaitingSessions_ChannelCleanupFailure_RetriesWhileSessionIsValid()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore { FailCancel = true };
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context);
        _ = handler.Create(player.SessionToken, new MatchSettings()).Value;

        // Act
        TestAssert.Throws<InvalidOperationException>(
            () => handler.CancelWaitingForSession(player.SessionToken));
        store.FailCancel = false;
        long[] cancelled = handler.CancelExpiredWaitingSessions();

        // Assert
        TestAssert.AreEqual(1, cancelled.Length);
        TestAssert.AreEqual(7L, cancelled[0]);
        TestAssert.AreEqual(0, handler.CancelExpiredWaitingSessions().Length);
    }

    internal void CancelWaitingForSession_TransientFailure_RetainsLobbyForRetry()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore { FailCancel = true };
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context);
        _ = handler.Create(player.SessionToken, new MatchSettings()).Value;
        context.Sessions.Revoke(player.SessionToken);

        // Act
        TestAssert.Throws<InvalidOperationException>(() => handler.CancelWaitingForSession(player.SessionToken));
        store.FailCancel = false;
        long[] cancelled = handler.CancelWaitingForSession(player.SessionToken);

        // Assert
        TestAssert.AreEqual(1, cancelled.Length);
        TestAssert.AreEqual(7L, cancelled[0]);
        TestAssert.AreEqual(0, handler.CancelWaitingForSession(player.SessionToken).Length);
    }

    internal void CancelWaitingForSession_JoinAlreadyStarted_PreservesStartedMatch()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore { Started = true };
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context);
        _ = handler.Create(player.SessionToken, new MatchSettings()).Value;

        // Act
        long[] cancelled = handler.CancelWaitingForSession(player.SessionToken);

        // Assert
        TestAssert.AreEqual(0, cancelled.Length);
        TestAssert.AreEqual(0, handler.CancelWaitingForSession(player.SessionToken).Length);
        TestAssert.AreEqual(1, store.CancelAttempts);
    }

    internal void Create_ExpiredSession_DoesNotCreateLobby()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context);
        context.UtcNow = player.ExpiresAtUtc;

        // Act
        MatchResult<Match> result = handler.Create(
            player.SessionToken,
            new MatchSettings());

        // Assert
        TestAssert.AreEqual(0, store.CreateAttempts);
        TestAssert.AreEqual(MatchStatus.InvalidSession, result.Status);
    }

    internal void Invite_DeliveryFailure_CancelsMatchAndThrowsServiceFault()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore();
        FailingInvitationEmailSender mailer = new FailingInvitationEmailSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        TestAssert.Throws<InvalidOperationException>(() =>
            handler.Invite(player.SessionToken, 100, new MatchSettings()));

        // Assert
        TestAssert.AreEqual(1, store.CancelAttempts);
    }

    internal void Invite_GuestSession_ThrowsUnauthorizedRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        GuestSession guest = context.Sessions.CreateGuest("Guest-GuestPlayer").GetValue();
        LifecycleStore store = new LifecycleStore();
        FailingInvitationEmailSender mailer = new FailingInvitationEmailSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        MatchResult<Match> result = handler.Invite(
            guest.SessionToken,
            100,
            new MatchSettings());

        // Assert
        TestAssert.AreEqual(MatchStatus.Unauthorized, result.Status);
    }

    private static AuthenticatedSession CreatePlayer(SessionTestContext context)
    {
        return context.Sessions.Create(new LoginAccount(42, "PlayerOne", "player@example.com",
            "test-hash", "ACTIVE", "en-US"));
    }

    private sealed class FailingInvitationEmailSender : IMatchInvitationEmailSender
    {
        public void Send(MatchInvitationEmail email)
        {
            throw new InvalidOperationException("Simulated provider outage.");
        }
    }

    private sealed class LifecycleStore : IMatchRepository
    {
        public MatchStatus EvaluateLobbyAccess(long accountId)
        {
            return MatchStatus.Success;
        }
        internal bool FailCancel { get; set; }
        internal bool Started { get; set; }
        internal int CancelAttempts { get; private set; }
        internal int CreateAttempts { get; private set; }

        public MatchResult<Match> Create(AuthenticatedSession player, MatchSettings settings, DateTime now)
        {
            CreateAttempts++;
            return new MatchResult<Match>(new Match { Id = 7 });
        }

        public MatchStatus Cancel(long accountId, long matchId)
        {
            CancelAttempts++;
            if (FailCancel)
            {
                throw new InvalidOperationException("Disposable storage failure.");
            }
            if (Started)
            {
                return MatchStatus.MatchNotWaiting;
            }

            return MatchStatus.Success;
        }

        public MatchResult<Match[]> Available(long accountId)
        {
            throw new NotSupportedException();
        }
        public MatchResult<Match> Join(AuthenticatedSession player, string code, DateTime now)
        {
            throw new NotSupportedException();
        }
        public MatchResult<Match> Get(long accountId, long matchId)
        {
            throw new NotSupportedException();
        }
        public MatchResult<Match> ChangeTurnDuration(long accountId, long matchId, int seconds)
        {
            throw new NotSupportedException();
        }

        public MatchResult<MatchInvitationPreparation> PreparePrivateInvitation(
            AuthenticatedSession creator,
            long friendAccountId,
            MatchSettings settings,
            DateTime now)
        {
            Match match = new Match
            {
                Id = 7,
                Code = "REV777",
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
                Code = "REV777",
                State = "Waiting",
                TurnDurationSeconds = 40,
                IsPrivate = true,
                Participants = new[]
                {
                    new MatchParticipant
                    {
                        AccountId = accountId,
                        SeatNumber = 1,
                        DisplayName = "PlayerOne"
                    }
                }
            };
            return new MatchResult<Match>(match);
        }

        public MatchStatus ValidateGuestJoin(string matchCode)
        {
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

        public MatchResult<Match[]> GetMatchHistory(long accountId)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match> GetForObservation(long accountId, long matchId)
        {
            throw new NotSupportedException();
        }
    }
}
