using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

internal sealed class LobbyAuthorizationTests
{
    internal void Authorize_LiveSession_ChecksAccountWithoutListingMatches()
    {
        // Arrange
        AuthorizationContext context = new AuthorizationContext();

        // Act
        MatchStatus status = context.Handler.EvaluateLobbyAccess(
            context.Player.SessionToken);

        // Assert
        TestAssert.AreEqual(context.Player.AccountId, context.Store.AuthorizedAccountId);
        TestAssert.AreEqual(MatchStatus.Success, status);
        TestAssert.AreEqual(1, context.Store.AuthorizationCalls);
        TestAssert.AreEqual(0, context.Store.AvailableCalls);
    }

    internal void Authorize_ExpiredSession_RejectsBeforeAccountQuery()
    {
        // Arrange
        AuthorizationContext context = new AuthorizationContext();
        context.Sessions.UtcNow = context.Player.ExpiresAtUtc;

        // Act
        MatchStatus status = context.Handler.EvaluateLobbyAccess(
            context.Player.SessionToken);

        // Assert
        TestAssert.AreEqual(0, context.Store.AuthorizationCalls);
        TestAssert.AreEqual(0, context.Store.AvailableCalls);
        TestAssert.AreEqual(MatchStatus.InvalidSession, status);
    }

    internal void Authorize_RevokedSession_RejectsBeforeAccountQuery()
    {
        // Arrange
        AuthorizationContext context = new AuthorizationContext();
        context.Sessions.Sessions.Revoke(context.Player.SessionToken);

        // Act
        MatchStatus status = context.Handler.EvaluateLobbyAccess(
            context.Player.SessionToken);

        // Assert
        TestAssert.AreEqual(0, context.Store.AuthorizationCalls);
        TestAssert.AreEqual(0, context.Store.AvailableCalls);
        TestAssert.AreEqual(MatchStatus.InvalidSession, status);
    }

    internal void OpenLobbyChannel_InactiveAccount_ReturnsMatchFaultWithoutListing()
    {
        // Arrange
        AuthorizationContext context = new AuthorizationContext();
        context.Store.Active = false;
        using MatchLobbyService service = new MatchLobbyService(
            context.Handler, NullLogger<MatchLobbyService>.Instance);

        // Act
        FaultException<MatchFault> failure = TestAssert.Throws<FaultException<MatchFault>>(
            () => service.OpenLobbyChannel(context.Player.SessionToken));

        // Assert
        TestAssert.AreEqual(MatchFailure.InactiveAccount, failure.Detail.Reason);
        TestAssert.AreEqual(1, context.Store.AuthorizationCalls);
        TestAssert.AreEqual(0, context.Store.AvailableCalls);
    }

    internal void OpenLobbyChannel_MissingToken_ReturnsSessionFaultBeforeAccountQuery()
    {
        // Arrange
        AuthorizationContext context = new AuthorizationContext();
        using MatchLobbyService service = new MatchLobbyService(
            context.Handler, NullLogger<MatchLobbyService>.Instance);

        // Act
        FaultException<SessionFault> failure = TestAssert.Throws<FaultException<SessionFault>>(
            () => service.OpenLobbyChannel(string.Empty));

        // Assert
        TestAssert.AreEqual(SessionFailure.MissingSessionToken, failure.Detail.Reason);
        TestAssert.AreEqual(0, context.Store.AuthorizationCalls);
        TestAssert.AreEqual(0, context.Store.AvailableCalls);
    }

    private sealed class AuthorizationContext
    {
        internal AuthorizationContext()
        {
            Sessions = new SessionTestContext();
            Player = Sessions.Sessions.Create(new LoginAccount(
                42, "LobbyPlayer", "lobby@example.com", "test-hash", "ACTIVE", "en-US"));
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

        public MatchResult<Match[]> Available(long accountId)
        {
            AvailableCalls++;
            throw new InvalidOperationException("Authorization must not enumerate lobbies.");
        }

        public MatchResult<Match> Create(
            AuthenticatedSession player, MatchSettings settings, DateTime now)
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

        public MatchResult<Match> Join(AuthenticatedSession player, string code, DateTime now)
        {
            throw new NotSupportedException();
        }

        public MatchStatus ValidateGuestJoin(string code)
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

        public MatchStatus Cancel(long accountId, long matchId)
        {
            throw new NotSupportedException();
        }
    }
}
