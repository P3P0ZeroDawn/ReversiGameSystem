using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class MatchApplicationServiceLifecycleTests
{
    [TestMethod]
    public void CancelExpiredWaitingSessions_ChannelCleanupFailure_RetriesWhileSessionIsValid()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore { FailCancel = true };
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context);
        _ = handler.CreateMatch(player.SessionToken, new MatchSettings()).Value;

        // Act
        Assert.Throws<InvalidOperationException>(
            () => handler.CancelWaitingForSession(player.SessionToken));
        store.FailCancel = false;
        long[] cancelled = handler.CancelExpiredWaitingSessions();

        // Assert
        Assert.AreEqual(1, cancelled.Length);
        Assert.AreEqual(7L, cancelled[0]);
        Assert.AreEqual(0, handler.CancelExpiredWaitingSessions().Length);
    }

    [TestMethod]
    public void CancelWaitingForSession_TransientFailure_RetainsLobbyForRetry()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore { FailCancel = true };
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context);
        _ = handler.CreateMatch(player.SessionToken, new MatchSettings()).Value;
        context.Sessions.RevokeSession(player.SessionToken);

        // Act
        Assert.Throws<InvalidOperationException>(() => handler.CancelWaitingForSession(player.SessionToken));
        store.FailCancel = false;
        long[] cancelled = handler.CancelWaitingForSession(player.SessionToken);

        // Assert
        Assert.AreEqual(1, cancelled.Length);
        Assert.AreEqual(7L, cancelled[0]);
        Assert.AreEqual(0, handler.CancelWaitingForSession(player.SessionToken).Length);
    }

    [TestMethod]
    public void CancelWaitingForSession_JoinAlreadyStarted_PreservesStartedMatch()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore { Started = true };
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context);
        _ = handler.CreateMatch(player.SessionToken, new MatchSettings()).Value;

        // Act
        long[] cancelled = handler.CancelWaitingForSession(player.SessionToken);

        // Assert
        Assert.AreEqual(0, cancelled.Length);
        Assert.AreEqual(0, handler.CancelWaitingForSession(player.SessionToken).Length);
        Assert.AreEqual(1, store.CancelAttempts);
    }

    [TestMethod]
    public void Create_ExpiredSession_DoesNotCreateLobby()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context);
        context.UtcNow = player.ExpiresAtUtc;

        // Act
        MatchResult<Match> result = handler.CreateMatch(
            player.SessionToken,
            new MatchSettings());

        // Assert
        Assert.AreEqual(0, store.CreateAttempts);
        Assert.AreEqual(MatchStatus.InvalidSession, result.Status);
    }

    [TestMethod]
    public void Invite_DeliveryFailure_CancelsMatchAndThrowsServiceFault()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession player = CreatePlayer(context);
        LifecycleStore store = new LifecycleStore();
        FailingInvitationEmailSender mailer = new FailingInvitationEmailSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        Assert.Throws<InvalidOperationException>(() =>
            handler.InviteToMatch(player.SessionToken, 100, new MatchSettings()));

        // Assert
        Assert.AreEqual(1, store.CancelAttempts);
    }

    [TestMethod]
    public void Invite_GuestSession_ThrowsUnauthorizedRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        GuestSession guest = context.ApplicationService.OpenGuestSession("GuestPlayer").Value
            ?? throw new InvalidOperationException("Guest session expected.");
        LifecycleStore store = new LifecycleStore();
        FailingInvitationEmailSender mailer = new FailingInvitationEmailSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        MatchResult<Match> result = handler.InviteToMatch(
            guest.SessionToken,
            100,
            new MatchSettings());

        // Assert
        Assert.AreEqual(MatchStatus.Unauthorized, result.Status);
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
            System.Collections.Generic.IReadOnlyDictionary<string, string> templateValues)
        {
            throw new InvalidOperationException("Simulated provider outage.");
        }
    }

    private sealed class LifecycleStore : IMatchRepository
    {
        public MatchResult<Match> StartSystemMatch(MatchPlayer player, long matchId, DateTime now)
        {
            throw new NotSupportedException();
        }

        public MatchStatus EvaluateLobbyAccess(long accountId)
        {
            return MatchStatus.Success;
        }
        internal bool FailCancel { get; set; }
        internal bool Started { get; set; }
        internal int CancelAttempts { get; private set; }
        internal int CreateAttempts { get; private set; }

        public MatchResult<Match> CreateMatch(MatchPlayer player, MatchSettings settings, DateTime now)
        {
            CreateAttempts++;
            return new MatchResult<Match>(new Match { Id = 7 });
        }

        public MatchStatus CancelMatch(MatchPlayer player, long matchId)
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

        public MatchResult<Match[]> GetAvailableMatches(MatchPlayer player)
        {
            throw new NotSupportedException();
        }
        public MatchResult<Match> JoinMatch(MatchPlayer player, string code, DateTime now)
        {
            throw new NotSupportedException();
        }
        public MatchResult<Match> LoadParticipantMatch(MatchPlayer player, long matchId)
        {
            throw new NotSupportedException();
        }
        public MatchResult<Match> ChangeTurnDuration(MatchPlayer player, long matchId, int seconds)
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
