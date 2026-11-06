using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Tests;

internal sealed class MatchHandlerLifecycleTests
{
    internal void CancelExpiredWaitingSessions_ChannelCleanupFailure_RetriesWhileSessionIsValid()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore { FailCancel = true };
        MatchHandler handler = new MatchHandler(context.Sessions, store, context);
        handler.Create(player.SessionToken, new MatchSettings());

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
        MatchHandler handler = new MatchHandler(context.Sessions, store, context);
        handler.Create(player.SessionToken, new MatchSettings());
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
        MatchHandler handler = new MatchHandler(context.Sessions, store, context);
        handler.Create(player.SessionToken, new MatchSettings());

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
        MatchHandler handler = new MatchHandler(context.Sessions, store, context);
        context.UtcNow = player.ExpiresAtUtc;

        // Act
        TestAssert.Throws<SessionRejectedException>(() => handler.Create(player.SessionToken, new MatchSettings()));

        // Assert
        TestAssert.AreEqual(0, store.CreateAttempts);
    }

    internal void Invite_DeliveryFailure_CancelsMatchAndThrowsServiceFault()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore();
        FailingInvitationEmailSender mailer = new FailingInvitationEmailSender();
        MatchHandler handler = new MatchHandler(context.Sessions, store, context, mailer);

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
        MatchHandler handler = new MatchHandler(context.Sessions, store, context, mailer);

        // Act
        MatchRejectedException exception = TestAssert.Throws<MatchRejectedException>(() =>
            handler.Invite(guest.SessionToken, 100, new MatchSettings()));

        // Assert
        TestAssert.AreEqual(MatchRejection.Unauthorized, exception.Reason);
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

    private sealed class LifecycleStore : IMatchStore
    {
        public void Authorize(long accountId)
        {
            // This fixture isolates creation and cleanup of an already authorized account.
        }
        internal bool FailCancel { get; set; }
        internal bool Started { get; set; }
        internal int CancelAttempts { get; private set; }
        internal int CreateAttempts { get; private set; }

        public MatchRecord Create(AuthenticatedSession player, MatchSettings settings, DateTime now)
        {
            CreateAttempts++;
            return new MatchRecord { Id = 7 };
        }

        public void Cancel(long accountId, long matchId)
        {
            CancelAttempts++;
            if (FailCancel)
            {
                throw new InvalidOperationException("Disposable storage failure.");
            }
            if (Started)
            {
                throw new MatchRejectedException(MatchRejection.MatchNotWaiting);
            }
        }

        public MatchRecord[] Available(long accountId) => throw new NotSupportedException();
        public MatchRecord Join(AuthenticatedSession player, string code, DateTime now) =>
            throw new NotSupportedException();
        public MatchRecord Get(long accountId, long matchId) => throw new NotSupportedException();
        public MatchRecord ChangeTurnDuration(long accountId, long matchId, int seconds) =>
            throw new NotSupportedException();

        public PrivateMatchInvitationPreparation PreparePrivate(
            AuthenticatedSession creator,
            long friendAccountId,
            MatchSettings settings,
            DateTime now)
        {
            MatchRecord match = new MatchRecord
            {
                Id = 7,
                Code = "REV777",
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
                Code = "REV777",
                State = "Waiting",
                TurnDurationSeconds = 40,
                IsPrivate = true,
                Participants = new[]
                {
                    new ParticipantRecord
                    {
                        AccountId = accountId,
                        SeatNumber = 1,
                        DisplayName = "PlayerOne"
                    }
                }
            };
        }

        public void ValidateJoinForGuest(string matchCode)
        {
        }
    }
}
