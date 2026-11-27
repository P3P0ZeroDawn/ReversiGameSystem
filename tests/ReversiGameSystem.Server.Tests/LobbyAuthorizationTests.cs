using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class LobbyAuthorizationTests
{
    [TestMethod]
    public void Authorize_LiveSession_ChecksAccountWithoutListingMatches()
    {
        // Arrange
        AuthorizationContext context = new AuthorizationContext();

        // Act
        MatchStatus status = context.Handler.EvaluateLobbyAccess(
            context.Player.SessionToken);

        // Assert
        Assert.AreEqual(context.Player.AccountId, context.Store.AuthorizedAccountId);
        Assert.AreEqual(MatchStatus.Success, status);
        Assert.AreEqual(1, context.Store.AuthorizationCalls);
        Assert.AreEqual(0, context.Store.AvailableCalls);
    }

    [TestMethod]
    public void Authorize_ExpiredSession_RejectsBeforeAccountQuery()
    {
        // Arrange
        AuthorizationContext context = new AuthorizationContext();
        context.Sessions.UtcNow = context.Player.ExpiresAtUtc;

        // Act
        MatchStatus status = context.Handler.EvaluateLobbyAccess(
            context.Player.SessionToken);

        // Assert
        Assert.AreEqual(0, context.Store.AuthorizationCalls);
        Assert.AreEqual(0, context.Store.AvailableCalls);
        Assert.AreEqual(MatchStatus.InvalidSession, status);
    }

    [TestMethod]
    public void Authorize_RevokedSession_RejectsBeforeAccountQuery()
    {
        // Arrange
        AuthorizationContext context = new AuthorizationContext();
        context.Sessions.Sessions.RevokeSession(context.Player.SessionToken);

        // Act
        MatchStatus status = context.Handler.EvaluateLobbyAccess(
            context.Player.SessionToken);

        // Assert
        Assert.AreEqual(0, context.Store.AuthorizationCalls);
        Assert.AreEqual(0, context.Store.AvailableCalls);
        Assert.AreEqual(MatchStatus.InvalidSession, status);
    }

    [TestMethod]
    public void OpenLobbyChannel_InactiveAccount_ReturnsMatchFaultWithoutListing()
    {
        // Arrange
        AuthorizationContext context = new AuthorizationContext();
        context.Store.Active = false;
        using MatchCallbackSubscriptions<IMatchLobbyServiceCallback> subscriptions =
            new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
                (_, _) => true,
                NullLogger.Instance);
        MatchLobbyService service = new MatchLobbyService(context.Handler, subscriptions);

        // Act
        FaultException<MatchFault> failure = Assert.Throws<FaultException<MatchFault>>(
            () => service.OpenLobbyChannel(context.Player.SessionToken));

        // Assert
        Assert.AreEqual(MatchFailure.InactiveAccount, failure.Detail.Reason);
        Assert.AreEqual(1, context.Store.AuthorizationCalls);
        Assert.AreEqual(0, context.Store.AvailableCalls);
    }

    [TestMethod]
    public void OpenLobbyChannel_MissingToken_ReturnsSessionFaultBeforeAccountQuery()
    {
        // Arrange
        AuthorizationContext context = new AuthorizationContext();
        using MatchCallbackSubscriptions<IMatchLobbyServiceCallback> subscriptions =
            new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
                (_, _) => true,
                NullLogger.Instance);
        MatchLobbyService service = new MatchLobbyService(context.Handler, subscriptions);

        // Act
        FaultException<SessionFault> failure = Assert.Throws<FaultException<SessionFault>>(
            () => service.OpenLobbyChannel(string.Empty));

        // Assert
        Assert.AreEqual(SessionFailure.MissingSessionToken, failure.Detail.Reason);
        Assert.AreEqual(0, context.Store.AuthorizationCalls);
        Assert.AreEqual(0, context.Store.AvailableCalls);
    }

    private sealed class AuthorizationContext
    {
        internal AuthorizationContext()
        {
            Sessions = new SessionTestContext();
            Player = TestSessionHelper.CreateSession(Sessions.Sessions, 42, "LobbyPlayer");
            Store = new AuthorizationStore();
            Handler = new MatchApplicationService(Sessions.Sessions, Store, Sessions);
        }

        internal SessionTestContext Sessions { get; }
        internal AuthenticatedSession Player { get; }
        internal AuthorizationStore Store { get; }
        internal MatchApplicationService Handler { get; }
    }

    private sealed class AuthorizationStore : IMatchRepository
    {
        public MatchResult<Match> StartSystemMatch(MatchPlayer player, long matchId, DateTime now)
        {
            throw new NotSupportedException();
        }

        internal bool Active { get; set; } = true;
        internal long AuthorizedAccountId { get; private set; }
        internal int AuthorizationCalls { get; private set; }
        internal int AvailableCalls { get; private set; }

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

        public MatchResult<Match[]> GetAvailableMatches(MatchPlayer player)
        {
            AvailableCalls++;
            throw new InvalidOperationException("Authorization must not enumerate lobbies.");
        }

        public MatchResult<Match> CreateMatch(
            MatchPlayer player, MatchSettings settings, DateTime now)
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

        public MatchResult<Match> MarkInvitationDelivered(long accountId, long matchId, DateTime now)
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

        public MatchStatus CancelMatch(MatchPlayer player, long matchId)
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
