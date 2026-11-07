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
        handler.Create(player.SessionToken, new MatchSettings()).GetValue();

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
        handler.Create(player.SessionToken, new MatchSettings()).GetValue();
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
        handler.Create(player.SessionToken, new MatchSettings()).GetValue();

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
        MatchOperationResult<Match> result = handler.Create(
            player.SessionToken,
            new MatchSettings());

        // Assert
        TestAssert.AreEqual(0, store.CreateAttempts);
        TestAssert.AreEqual(MatchOperationStatus.InvalidSession, result.Status);
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
        MatchOperationResult<Match> result = handler.Invite(
            guest.SessionToken,
            100,
            new MatchSettings());

        // Assert
        TestAssert.AreEqual(MatchOperationStatus.Unauthorized, result.Status);
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
        public MatchRepositoryStatus Authorize(long accountId)
        {
            return MatchRepositoryStatus.Success;
        }
        internal bool FailCancel { get; set; }
        internal bool Started { get; set; }
        internal int CancelAttempts { get; private set; }
        internal int CreateAttempts { get; private set; }

        public MatchRepositoryResult<Match> Create(AuthenticatedSession player, MatchSettings settings, DateTime now)
        {
            CreateAttempts++;
            return MatchRepositoryResult<Match>.CreateSuccess(new Match { Id = 7 });
        }

        public MatchRepositoryStatus Cancel(long accountId, long matchId)
        {
            CancelAttempts++;
            if (FailCancel)
            {
                throw new InvalidOperationException("Disposable storage failure.");
            }
            if (Started)
            {
                return MatchRepositoryStatus.MatchNotWaiting;
            }

            return MatchRepositoryStatus.Success;
        }

        public MatchRepositoryResult<Match[]> Available(long accountId)
        {
            throw new NotSupportedException();
        }
        public MatchRepositoryResult<Match> Join(AuthenticatedSession player, string code, DateTime now)
        {
            throw new NotSupportedException();
        }
        public MatchRepositoryResult<Match> Get(long accountId, long matchId)
        {
            throw new NotSupportedException();
        }
        public MatchRepositoryResult<Match> ChangeTurnDuration(long accountId, long matchId, int seconds)
        {
            throw new NotSupportedException();
        }

        public MatchRepositoryResult<MatchInvitationPreparation> PreparePrivateInvitation(
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
            return MatchRepositoryResult<MatchInvitationPreparation>.CreateSuccess(preparation);
        }

        public MatchRepositoryResult<Match> MarkInvitationDelivered(
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
            return MatchRepositoryResult<Match>.CreateSuccess(match);
        }

        public MatchRepositoryStatus ValidateJoinForGuest(string matchCode)
        {
            return MatchRepositoryStatus.Success;
        }
    }
}
