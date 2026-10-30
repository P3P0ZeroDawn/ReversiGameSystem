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
        AuthenticatedSessionRecord player = CreatePlayer(context);
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
        AuthenticatedSessionRecord player = CreatePlayer(context);
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
        AuthenticatedSessionRecord player = CreatePlayer(context);
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
        AuthenticatedSessionRecord player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore();
        MatchHandler handler = new MatchHandler(context.Sessions, store, context);
        context.UtcNow = player.ExpiresAtUtc;

        // Act
        TestAssert.Throws<SessionRejectedException>(() => handler.Create(player.SessionToken, new MatchSettings()));

        // Assert
        TestAssert.AreEqual(0, store.CreateAttempts);
    }

    private static AuthenticatedSessionRecord CreatePlayer(SessionTestContext context)
    {
        return context.Sessions.Create(new LoginAccount(42, "PlayerOne", "player@example.com",
            "test-hash", "ACTIVE", "en-US"));
    }

    private sealed class LifecycleStore : IMatchStore
    {
        internal bool FailCancel { get; set; }
        internal bool Started { get; set; }
        internal int CancelAttempts { get; private set; }
        internal int CreateAttempts { get; private set; }

        public MatchRecord Create(AuthenticatedSessionRecord player, MatchSettings settings, DateTime now)
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
        public MatchRecord Join(AuthenticatedSessionRecord player, string code, DateTime now) =>
            throw new NotSupportedException();
        public MatchRecord Get(long accountId, long matchId) => throw new NotSupportedException();
        public MatchRecord ChangeTurnDuration(long accountId, long matchId, int seconds) =>
            throw new NotSupportedException();
    }
}
