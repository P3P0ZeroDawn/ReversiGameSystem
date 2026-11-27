using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.ServiceApi;
using AppMatchService =
    ReversiGameSystem.Server.Application.Matches.MatchApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application;

[TestClass]
[TestCategory("Unit")]
public sealed class LobbyAuthorizationTests
{
    [TestMethod]
    public void Authorize_LiveSession_ChecksAccountWithoutListingMatches()
    {
        AuthorizationContext context = new AuthorizationContext();
        MatchStatus expectedStatus = MatchStatus.Success;
        int expectedAuthCalls = 1;
        int expectedAvailableCalls = 0;

        MatchStatus status = context.Handler.EvaluateLobbyAccess(
            context.Player.SessionToken);

        Assert.AreEqual(context.Player.AccountId, context.Store.AuthorizedAccountId);
        Assert.AreEqual(expectedStatus, status);
        Assert.AreEqual(expectedAuthCalls, context.Store.AuthorizationCalls);
        Assert.AreEqual(expectedAvailableCalls, context.Store.AvailableCalls);
    }

    [TestMethod]
    public void Authorize_ExpiredSession_RejectsBeforeAccountQuery()
    {
        AuthorizationContext context = new AuthorizationContext();
        context.Sessions.UtcNow = context.Player.ExpiresAtUtc;
        MatchStatus expectedStatus = MatchStatus.InvalidSession;
        int expectedAuthCalls = 0;
        int expectedAvailableCalls = 0;

        MatchStatus status = context.Handler.EvaluateLobbyAccess(
            context.Player.SessionToken);

        Assert.AreEqual(expectedAuthCalls, context.Store.AuthorizationCalls);
        Assert.AreEqual(expectedAvailableCalls, context.Store.AvailableCalls);
        Assert.AreEqual(expectedStatus, status);
    }

    [TestMethod]
    public void Authorize_RevokedSession_RejectsBeforeAccountQuery()
    {
        AuthorizationContext context = new AuthorizationContext();
        context.Sessions.Sessions.RevokeSession(context.Player.SessionToken);
        MatchStatus expectedStatus = MatchStatus.InvalidSession;
        int expectedAuthCalls = 0;
        int expectedAvailableCalls = 0;

        MatchStatus status = context.Handler.EvaluateLobbyAccess(
            context.Player.SessionToken);

        Assert.AreEqual(expectedAuthCalls, context.Store.AuthorizationCalls);
        Assert.AreEqual(expectedAvailableCalls, context.Store.AvailableCalls);
        Assert.AreEqual(expectedStatus, status);
    }

    [TestMethod]
    public void OpenLobbyChannel_InactiveAccount_ReturnsMatchFaultWithoutListing()
    {
        AuthorizationContext context = new AuthorizationContext();
        context.Store.Active = false;
        using MatchCallbackSubscriptions<IMatchLobbyServiceCallback> subscriptions =
            new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
                (_, _) => true,
                NullLogger.Instance);
        MatchLobbyService service = new MatchLobbyService(context.Handler, subscriptions);
        MatchFailure expectedReason = MatchFailure.InactiveAccount;
        int expectedAuthCalls = 1;
        int expectedAvailableCalls = 0;

        FaultException<MatchFault> failure = Assert.Throws<FaultException<MatchFault>>(
            () => service.OpenLobbyChannel(context.Player.SessionToken));

        Assert.AreEqual(expectedReason, failure.Detail.Reason);
        Assert.AreEqual(expectedAuthCalls, context.Store.AuthorizationCalls);
        Assert.AreEqual(expectedAvailableCalls, context.Store.AvailableCalls);
    }

    [TestMethod]
    public void OpenLobbyChannel_MissingToken_ReturnsSessionFaultBeforeAccountQuery()
    {
        AuthorizationContext context = new AuthorizationContext();
        using MatchCallbackSubscriptions<IMatchLobbyServiceCallback> subscriptions =
            new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
                (_, _) => true,
                NullLogger.Instance);
        MatchLobbyService service = new MatchLobbyService(context.Handler, subscriptions);
        SessionFailure expectedReason = SessionFailure.MissingSessionToken;
        int expectedAuthCalls = 0;
        int expectedAvailableCalls = 0;

        FaultException<SessionFault> failure = Assert.Throws<FaultException<SessionFault>>(
            () => service.OpenLobbyChannel(string.Empty));

        Assert.AreEqual(expectedReason, failure.Detail.Reason);
        Assert.AreEqual(expectedAuthCalls, context.Store.AuthorizationCalls);
        Assert.AreEqual(expectedAvailableCalls, context.Store.AvailableCalls);
    }

    private sealed class AuthorizationContext
    {
        internal AuthorizationContext()
        {
            Sessions = new SessionTestContext();
            Player = TestSessionHelper.CreateSession(Sessions.Sessions, 42, "LobbyPlayer");
            Store = new AuthorizationStore();
            Handler = new AppMatchService(Sessions.Sessions, Store, Sessions);
        }

        internal SessionTestContext Sessions { get; }

        internal AuthenticatedSession Player { get; }

        internal AuthorizationStore Store { get; }

        internal AppMatchService Handler { get; }
    }

    private sealed class AuthorizationStore : IMatchRepository
    {
        internal bool Active { get; set; } = true;

        internal long AuthorizedAccountId { get; private set; }

        internal int AuthorizationCalls { get; private set; }

        internal int AvailableCalls { get; private set; }

        public MatchResult<Match> StartSystemMatch(long accountId, long matchId, DateTime now)
        {
            throw new NotSupportedException();
        }

        public MatchStatus EvaluateLobbyAccess(long accountId)
        {
            AuthorizationCalls++;
            AuthorizedAccountId = accountId;
            if (!Active)
            {
                return MatchStatus.InactiveAccount;
            }

            return MatchStatus.Success;
        }

        public MatchResult<Match[]> GetAvailableMatches(long accountId)
        {
            AvailableCalls++;
            throw new InvalidOperationException("Authorization must not enumerate lobbies.");
        }

        public MatchResult<Match> CreateMatch(
            AuthenticatedSession player,
            MatchSettings settings,
            DateTime now)
        {
            throw new NotSupportedException();
        }

        public MatchResult<MatchInvitationPreparation> PreparePrivateInvitation(
            AuthenticatedSession player,
            long friendAccountId,
            MatchSettings settings,
            DateTime now)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match> MarkInvitationDelivered(
            long accountId,
            long matchId,
            DateTime now)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match> JoinMatch(AuthenticatedSession player, string code, DateTime now)
        {
            throw new NotSupportedException();
        }

        public MatchStatus ValidateGuestJoin(string code)
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

        public MatchStatus CancelMatch(long accountId, long matchId)
        {
            throw new NotSupportedException();
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
