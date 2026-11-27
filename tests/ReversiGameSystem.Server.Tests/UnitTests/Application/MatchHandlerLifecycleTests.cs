using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Matches;
using AppMatchService = ReversiGameSystem.Server.Application.Matches.MatchApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application;

[TestClass]
[TestCategory("Unit")]
public sealed class MatchApplicationServiceLifecycleTests
{
    [TestMethod]
    public void CancelExpiredWaitingSessions_ChannelCleanupFailure_RetriesWhileSessionIsValid()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore { FailCancel = true };
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context);
        _ = handler.CreateMatch(player.SessionToken, new MatchSettings()).Value;
        int expectedCancelledCount = 1;
        long expectedMatchId = 7L;
        int expectedRemainingCount = 0;

        Assert.Throws<InvalidOperationException>(
            () => handler.CancelWaitingForSession(player.SessionToken));
        store.FailCancel = false;
        long[] cancelled = handler.CancelExpiredWaitingSessions();

        Assert.AreEqual(expectedCancelledCount, cancelled.Length);
        Assert.AreEqual(expectedMatchId, cancelled[0]);
        Assert.AreEqual(expectedRemainingCount, handler.CancelExpiredWaitingSessions().Length);
    }

    [TestMethod]
    public void CancelWaitingForSession_TransientFailure_RetainsLobbyForRetry()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore { FailCancel = true };
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context);
        _ = handler.CreateMatch(player.SessionToken, new MatchSettings()).Value;
        context.Sessions.RevokeSession(player.SessionToken);
        int expectedCancelledCount = 1;
        long expectedMatchId = 7L;
        int expectedRemainingCount = 0;

        Assert.Throws<InvalidOperationException>(
            () => handler.CancelWaitingForSession(player.SessionToken));
        store.FailCancel = false;
        long[] cancelled = handler.CancelWaitingForSession(player.SessionToken);

        Assert.AreEqual(expectedCancelledCount, cancelled.Length);
        Assert.AreEqual(expectedMatchId, cancelled[0]);
        Assert.AreEqual(
            expectedRemainingCount,
            handler.CancelWaitingForSession(player.SessionToken).Length);
    }

    [TestMethod]
    public void CancelWaitingForSession_JoinAlreadyStarted_PreservesStartedMatch()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore { Started = true };
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context);
        _ = handler.CreateMatch(player.SessionToken, new MatchSettings()).Value;
        int expectedCancelledCount = 0;
        int expectedCancelAttempts = 1;

        long[] cancelled = handler.CancelWaitingForSession(player.SessionToken);

        Assert.AreEqual(expectedCancelledCount, cancelled.Length);
        Assert.AreEqual(
            expectedCancelledCount,
            handler.CancelWaitingForSession(player.SessionToken).Length);
        Assert.AreEqual(expectedCancelAttempts, store.CancelAttempts);
    }

    [TestMethod]
    public void Create_ExpiredSession_DoesNotCreateLobby()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context);
        context.UtcNow = player.ExpiresAtUtc;
        int expectedCreateAttempts = 0;
        MatchStatus expectedStatus = MatchStatus.InvalidSession;

        MatchResult<Match> result = handler.CreateMatch(
            player.SessionToken,
            new MatchSettings());

        Assert.AreEqual(expectedCreateAttempts, store.CreateAttempts);
        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void Invite_DeliveryFailure_CancelsMatchAndThrowsServiceFault()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore();
        FailingInvitationEmailSender mailer = new FailingInvitationEmailSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        int expectedCancelAttempts = 1;

        Assert.Throws<InvalidOperationException>(() =>
            handler.InviteToMatch(player.SessionToken, 100, new MatchSettings()));

        Assert.AreEqual(expectedCancelAttempts, store.CancelAttempts);
    }

    [TestMethod]
    public void Invite_GuestSession_ThrowsUnauthorizedRejection()
    {
        SessionTestContext context = new SessionTestContext();
        GuestSession guest = context.ApplicationService.OpenGuestSession("GuestPlayer").Value
            ?? throw new InvalidOperationException("Guest session expected.");
        LifecycleStore store = new LifecycleStore();
        FailingInvitationEmailSender mailer = new FailingInvitationEmailSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        MatchStatus expectedStatus = MatchStatus.Unauthorized;

        MatchResult<Match> result = handler.InviteToMatch(
            guest.SessionToken,
            100,
            new MatchSettings());

        Assert.AreEqual(expectedStatus, result.Status);
    }

    private static AuthenticatedSession CreatePlayer(SessionTestContext context)
    {
        return TestSessionHelper.CreateSession(context.Sessions, 42, "PlayerOne");
    }

    private sealed class FailingInvitationEmailSender : ITemplatedEmailSender
    {
        public void SendTemplatedEmail(
            string recipientAddress,
            string templateName,
            IReadOnlyDictionary<string, string> templateValues)
        {
            throw new InvalidOperationException("Simulated provider outage.");
        }
    }

    private sealed class LifecycleStore : IMatchRepository
    {
        internal bool FailCancel { get; set; }

        internal bool Started { get; set; }

        internal int CancelAttempts { get; private set; }

        internal int CreateAttempts { get; private set; }

        public MatchResult<Match> StartSystemMatch(long accountId, long matchId, DateTime now)
        {
            throw new NotSupportedException();
        }

        public MatchStatus EvaluateLobbyAccess(long accountId)
        {
            return MatchStatus.Success;
        }

        public MatchResult<Match> CreateMatch(
            AuthenticatedSession player,
            MatchSettings settings,
            DateTime now)
        {
            CreateAttempts++;
            return new MatchResult<Match>(new Match { Id = 7 });
        }

        public MatchStatus CancelMatch(long accountId, long matchId)
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

        public MatchResult<Match[]> GetAvailableMatches(long accountId)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match> JoinMatch(AuthenticatedSession player, string code, DateTime now)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match> LoadParticipantMatch(long accountId, long matchId)
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
