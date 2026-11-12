using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MimeKit;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Persistence.Mail;

namespace ReversiGameSystem.Server.Tests;

/// <summary>
/// Unit and integration test suite for private match invitations over WCF and application layers.
/// </summary>
internal sealed class MatchInvitationTests
{
    private const string ExpectedMandatoryLegend = "«Puedes unirte con este código mientras el creador siga esperando»";

    /// <summary>
    /// Creates the individual test delegates for server invitation tests.
    /// </summary>
    /// <returns>An array of test actions.</returns>
    internal static Action[] CreateTests()
    {
        MatchInvitationTests suite = new MatchInvitationTests();
        return new Action[]
        {
            suite.Invite_ConnectedFriend_DeliversEmailAndReturnsWaitingSnapshot,
            suite.Invite_DisconnectedFriend_DeliversEmailAndReturnsWaitingSnapshot,
            suite.Invite_GuestSession_ThrowsUnauthorizedRejection,
            suite.Invite_SelfInvitation_ThrowsUnauthorizedRejection,
            suite.Invite_NegativeOrZeroFriendAccountId_ThrowsInvalidAccountIdRejection,
            suite.Invite_AccountUnavailable_ThrowsAccountUnavailableRejection,
            suite.Invite_FriendshipNotFound_ThrowsFriendshipNotFoundRejection,
            suite.Invite_InactiveCaller_ThrowsInactiveAccountRejection,
            suite.Invite_InvalidTurnDuration_ThrowsInvalidTurnDurationRejection,
            suite.Invite_InvalidOpponentKind_ThrowsInvalidConfigurationRejection,
            suite.Invite_CountsTowardStatisticsTrue_ThrowsInvalidConfigurationRejection,
            suite.Invite_DeliveryFailure_CancelsPreparedMatchAndThrowsException,
            suite.Invite_DeliveryFailureWhenCompensationFails_PreservesUnconfirmedMatch,
            suite.Invite_CreatorLeavesBeforeConfirmation_CancelsMatchAndThrowsMatchNotWaiting,
            suite.GetAvailableMatches_PrivateMatch_ExcludesPrivateMatchesFromLobby,
            suite.Join_PrivateMatchForInvitedAccount_SucceedsAndStartsMatch,
            suite.Join_PrivateMatchForOtherAccount_ThrowsUnauthorizedRejection,
            suite.Join_PrivateMatchForGuest_ThrowsUnauthorizedRejection,
            suite.Join_UnconfirmedPrivateMatch_ThrowsMatchCodeUnavailableRejection,
            suite.Join_CancelledPrivateMatch_ThrowsMatchCodeUnavailableRejection,
            suite.Join_ConcurrentSameInvitedAccount_RegistersOnlyOneSecondSeat,
            suite.CancelWaitingForSession_PrivateMatchWaiting_CancelsMatchAndRemovesLobby,
            suite.CancelWaitingForSession_DifferentSessionForSameAccount_DoesNotProlongPreviousWaitingMatch,
            suite.EmailSender_CreateMessage_ContainsMandatoryLegendAndMatchCode
        };
    }

    /// <summary>
    /// Executes all tests in the invitation test suite.
    /// </summary>
    internal static void RunAll()
    {
        foreach (Action test in CreateTests())
        {
            test();
        }
    }

    /// <summary>
    /// Verifies that inviting a connected friend successfully prepares, delivers the email,
    /// and confirms the waiting lobby.
    /// </summary>
    internal void Invite_ConnectedFriend_DeliversEmailAndReturnsWaitingSnapshot()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        AuthenticatedSession friend = CreatePlayer(context, 20, "BetaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, "beta@example.com");
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        Match result = handler.Invite(
            creator.SessionToken,
            20,
            new MatchSettings { TurnDurationSeconds = 40 }).Value
            ?? throw new InvalidOperationException("Match expected.");

        // Assert
        TestAssert.AreEqual("Waiting", result.State);
        TestAssert.AreEqual(true, result.IsPrivate);
        TestAssert.AreEqual(1, mailer.SentEmails.Count);
        MatchInvitationEmail sent = mailer.SentEmails[0];
        TestAssert.AreEqual("beta@example.com", sent.RecipientEmail);
        TestAssert.AreEqual("AlphaPlayer", sent.CreatorDisplayName);
        TestAssert.AreEqual(result.Code, sent.MatchCode);
    }

    /// <summary>
    /// Verifies that inviting an offline (disconnected) friend succeeds if account and friendship are active.
    /// </summary>
    internal void Invite_DisconnectedFriend_DeliversEmailAndReturnsWaitingSnapshot()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 30, "gamma@example.com");
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        Match result = handler.Invite(
            creator.SessionToken,
            30,
            new MatchSettings { TurnDurationSeconds = 60 }).Value
            ?? throw new InvalidOperationException("Match expected.");

        // Assert
        TestAssert.AreEqual("Waiting", result.State);
        TestAssert.AreEqual(true, result.IsPrivate);
        TestAssert.AreEqual(1, mailer.SentEmails.Count);
        TestAssert.AreEqual("gamma@example.com", mailer.SentEmails[0].RecipientEmail);
    }

    /// <summary>
    /// Verifies that a guest session attempting to invite someone throws an Unauthorized rejection.
    /// </summary>
    internal void Invite_GuestSession_ThrowsUnauthorizedRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        GuestSession guest = context.Sessions.CreateGuest("Guest-GuestAlpha").GetValue();
        InvitationMockStore store = new InvitationMockStore();
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        MatchResult<Match> result = handler.Invite(
            guest.SessionToken,
            20,
            new MatchSettings());

        // Assert
        TestAssert.AreEqual(MatchStatus.Unauthorized, result.Status);
        TestAssert.AreEqual(0, mailer.SentEmails.Count);
    }

    /// <summary>
    /// Verifies that inviting one's own account ID throws an Unauthorized rejection.
    /// </summary>
    internal void Invite_SelfInvitation_ThrowsUnauthorizedRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        MatchResult<Match> result = handler.Invite(
            creator.SessionToken,
            10,
            new MatchSettings());

        // Assert
        TestAssert.AreEqual(MatchStatus.Unauthorized, result.Status);
        TestAssert.AreEqual(0, mailer.SentEmails.Count);
    }

    /// <summary>
    /// Verifies that specifying a friend account ID less than or equal to 0 throws an InvalidAccountId rejection.
    /// </summary>
    internal void Invite_NegativeOrZeroFriendAccountId_ThrowsInvalidAccountIdRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act & Assert
        TestAssert.AreEqual(
            MatchStatus.InvalidFriendAccountId,
            handler.Invite(creator.SessionToken, 0, new MatchSettings()).Status);
        TestAssert.AreEqual(
            MatchStatus.InvalidFriendAccountId,
            handler.Invite(creator.SessionToken, -5, new MatchSettings()).Status);
    }

    /// <summary>
    /// Verifies that an inactive or nonexistent recipient account throws AccountUnavailable.
    /// </summary>
    internal void Invite_AccountUnavailable_ThrowsAccountUnavailableRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        MatchResult<Match> result = handler.Invite(
            creator.SessionToken,
            999,
            new MatchSettings());

        // Assert
        TestAssert.AreEqual(MatchStatus.FriendAccountUnavailable, result.Status);
        TestAssert.AreEqual(0, mailer.SentEmails.Count);
    }

    /// <summary>
    /// Verifies that inviting a user without an active, accepted friendship throws FriendshipNotFound.
    /// </summary>
    internal void Invite_FriendshipNotFound_ThrowsFriendshipNotFoundRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterAccount(50, "inactive@example.com", true);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        MatchResult<Match> result = handler.Invite(
            creator.SessionToken,
            50,
            new MatchSettings());

        // Assert
        TestAssert.AreEqual(MatchStatus.FriendshipNotFound, result.Status);
        TestAssert.AreEqual(0, mailer.SentEmails.Count);
    }

    /// <summary>
    /// Verifies that an inactive creator throws InactiveAccount rejection.
    /// </summary>
    internal void Invite_InactiveCaller_ThrowsInactiveAccountRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession inactiveCaller = context.Sessions.Create(
            new LoginAccount(10, "InactiveAlpha", "alpha@example.com", "hash", "INACTIVE", "en-US"));
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, "beta@example.com");
        store.RegisterAccount(10, "alpha@example.com", false);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        MatchResult<Match> result = handler.Invite(
            inactiveCaller.SessionToken,
            20,
            new MatchSettings());

        // Assert
        TestAssert.AreEqual(MatchStatus.InactiveAccount, result.Status);
        TestAssert.AreEqual(0, mailer.SentEmails.Count);
    }

    /// <summary>
    /// Verifies that specifying a turn duration outside 10..120 throws InvalidTurnDuration.
    /// </summary>
    internal void Invite_InvalidTurnDuration_ThrowsInvalidTurnDurationRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act & Assert
        TestAssert.AreEqual(
            MatchStatus.InvalidTurnDuration,
            handler.Invite(
                creator.SessionToken,
                20,
                new MatchSettings { TurnDurationSeconds = 9 }).Status);
        TestAssert.AreEqual(
            MatchStatus.InvalidTurnDuration,
            handler.Invite(
                creator.SessionToken,
                20,
                new MatchSettings { TurnDurationSeconds = 121 }).Status);
    }

    /// <summary>
    /// Verifies that setting OpponentKind != Human throws InvalidConfiguration.
    /// </summary>
    internal void Invite_InvalidOpponentKind_ThrowsInvalidConfigurationRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        MatchResult<Match> result = handler.Invite(
            creator.SessionToken,
            20,
            new MatchSettings { OpponentKind = "System" });

        // Assert
        TestAssert.AreEqual(MatchStatus.InvalidConfiguration, result.Status);
    }

    /// <summary>
    /// Verifies that setting CountsTowardStatistics = true throws InvalidConfiguration.
    /// </summary>
    internal void Invite_CountsTowardStatisticsTrue_ThrowsInvalidConfigurationRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        MatchResult<Match> result = handler.Invite(
            creator.SessionToken,
            20,
            new MatchSettings { CountsTowardStatistics = true });

        // Assert
        TestAssert.AreEqual(MatchStatus.InvalidConfiguration, result.Status);
    }

    /// <summary>
    /// Verifies that when the email delivery fails, the prepared match is cancelled and an exception thrown.
    /// </summary>
    internal void Invite_DeliveryFailure_CancelsPreparedMatchAndThrowsException()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, "beta@example.com");
        InMemoryInvitationSender mailer = new InMemoryInvitationSender { FailSend = true };
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        TestAssert.Throws<InvalidOperationException>(() =>
            handler.Invite(creator.SessionToken, 20, new MatchSettings()));

        // Assert
        TestAssert.AreEqual(1, store.CancelCount);
        TestAssert.AreEqual(0, store.ConfirmedCount);
    }

    /// <summary>
    /// Verifies that when both email delivery and cancellation compensation fail,
    /// the match remains unconfirmed, preventing any join.
    /// </summary>
    internal void Invite_DeliveryFailureWhenCompensationFails_PreservesUnconfirmedMatch()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        InvitationMockStore store = new InvitationMockStore { FailCancel = true };
        store.RegisterFriendship(10, 20, "beta@example.com");
        InMemoryInvitationSender mailer = new InMemoryInvitationSender { FailSend = true };
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        // Act
        TestAssert.Throws<AggregateException>(() =>
            handler.Invite(creator.SessionToken, 20, new MatchSettings()));

        // Assert
        TestAssert.AreEqual(1, store.Rows.Count);
        MockMatchRow row = store.Rows.Values.First();
        TestAssert.IsTrue(!row.IsConfirmed, "Match must not be confirmed.");
    }

    /// <summary>
    /// Verifies that if the creator leaves or cancels the session during email delivery,
    /// confirmation fails and MatchNotWaiting is thrown.
    /// </summary>
    internal void Invite_CreatorLeavesBeforeConfirmation_CancelsMatchAndThrowsMatchNotWaiting()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, "beta@example.com");
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);
        mailer.BeforeSendCallback = () =>
        {
            context.Sessions.Revoke(creator.SessionToken);
        };

        // Act
        MatchResult<Match> result = handler.Invite(
            creator.SessionToken,
            20,
            new MatchSettings());

        // Assert
        TestAssert.AreEqual(MatchStatus.MatchNotWaiting, result.Status);
        TestAssert.AreEqual(1, store.CancelCount);
    }

    /// <summary>
    /// Verifies that GetAvailableMatches strictly excludes private matches.
    /// </summary>
    internal void GetAvailableMatches_PrivateMatch_ExcludesPrivateMatchesFromLobby()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        AuthenticatedSession friend = CreatePlayer(context, 20, "BetaPlayer");
        AuthenticatedSession stranger = CreatePlayer(context, 99, "StrangerPlayer");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, "beta@example.com");
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);

        _ = handler.Invite(creator.SessionToken, 20, new MatchSettings()).Value;
        AuthenticatedSession otherCreator = CreatePlayer(context, 88, "OtherCreator");
        _ = handler.Create(otherCreator.SessionToken, new MatchSettings()).Value;

        // Act & Assert
        Match[] forCreator = handler.Available(creator.SessionToken).Value
            ?? throw new InvalidOperationException("Matches expected.");
        TestAssert.AreEqual(1, forCreator.Length);
        TestAssert.AreEqual(false, forCreator[0].IsPrivate);

        Match[] forFriend = handler.Available(friend.SessionToken).Value
            ?? throw new InvalidOperationException("Matches expected.");
        TestAssert.AreEqual(1, forFriend.Length);
        TestAssert.AreEqual(false, forFriend[0].IsPrivate);

        Match[] forStranger = handler.Available(stranger.SessionToken).Value
            ?? throw new InvalidOperationException("Matches expected.");
        TestAssert.AreEqual(1, forStranger.Length);
        TestAssert.AreEqual(false, forStranger[0].IsPrivate);
    }

    /// <summary>
    /// Verifies that the invited account can join the private match with the match code.
    /// </summary>
    internal void Join_PrivateMatchForInvitedAccount_SucceedsAndStartsMatch()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        AuthenticatedSession friend = CreatePlayer(context, 20, "BetaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, "beta@example.com");
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);
        Match waiting = handler.Invite(
            creator.SessionToken,
            20,
            new MatchSettings()).Value
            ?? throw new InvalidOperationException("Match expected.");

        // Act
        Match joined = handler.Join(friend.SessionToken, waiting.Code!).Value
            ?? throw new InvalidOperationException("Match expected.");

        // Assert
        TestAssert.AreEqual("InProgress", joined.State);
        TestAssert.AreEqual(2, joined.Participants.Length);
        TestAssert.AreEqual(20L, joined.Participants[1].AccountId);
    }

    /// <summary>
    /// Verifies that an uninvited account attempting to join a private match throws Unauthorized.
    /// </summary>
    internal void Join_PrivateMatchForOtherAccount_ThrowsUnauthorizedRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        AuthenticatedSession friend = CreatePlayer(context, 20, "BetaPlayer");
        AuthenticatedSession stranger = CreatePlayer(context, 99, "StrangerPlayer");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, "beta@example.com");
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);
        Match waiting = handler.Invite(
            creator.SessionToken,
            20,
            new MatchSettings()).Value
            ?? throw new InvalidOperationException("Match expected.");

        // Act
        MatchResult<Match> result = handler.Join(
            stranger.SessionToken,
            waiting.Code!);

        // Assert
        TestAssert.AreEqual(MatchStatus.Unauthorized, result.Status);
    }

    /// <summary>
    /// Verifies that a guest account attempting to join a private match returns Unauthorized status.
    /// </summary>
    internal void Join_PrivateMatchForGuest_ThrowsUnauthorizedRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, "beta@example.com");
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);
        Match waiting = handler.Invite(
            creator.SessionToken,
            20,
            new MatchSettings()).Value
            ?? throw new InvalidOperationException("Match expected.");

        // Act
        MatchStatus status = store.ValidateGuestJoin(waiting.Code!);

        // Assert
        TestAssert.AreEqual(MatchStatus.Unauthorized, status);
    }

    /// <summary>
    /// Verifies that joining an unconfirmed private match throws MatchCodeUnavailable.
    /// </summary>
    internal void Join_UnconfirmedPrivateMatch_ThrowsMatchCodeUnavailableRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession friend = CreatePlayer(context, 20, "BetaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        Match unconfirmed = new Match
        {
            Id = 55,
            Code = "AB2324",
            State = "Waiting",
            IsPrivate = true,
            Participants = new[] { new MatchParticipant { AccountId = 10, SeatNumber = 1, DisplayName = "Alpha" } }
        };
        store.Rows[55] = new MockMatchRow { Match = unconfirmed, InvitedAccountId = 20, IsConfirmed = false };

        // Act
        MatchResult<Match> result = store.Join(friend, "AB2324", DateTime.UtcNow);

        // Assert
        TestAssert.AreEqual(MatchStatus.MatchCodeUnavailable, result.Status);
    }

    /// <summary>
    /// Verifies that joining a cancelled private match throws MatchCodeUnavailable.
    /// </summary>
    internal void Join_CancelledPrivateMatch_ThrowsMatchCodeUnavailableRejection()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession friend = CreatePlayer(context, 20, "BetaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        Match cancelled = new Match
        {
            Id = 56,
            Code = "AB2325",
            State = "Cancelled",
            IsPrivate = true,
            Participants = new[] { new MatchParticipant { AccountId = 10, SeatNumber = 1, DisplayName = "Alpha" } }
        };
        store.Rows[56] = new MockMatchRow { Match = cancelled, InvitedAccountId = 20, IsConfirmed = true };

        // Act
        MatchResult<Match> result = store.Join(friend, "AB2325", DateTime.UtcNow);

        // Assert
        TestAssert.AreEqual(MatchStatus.MatchCodeUnavailable, result.Status);
    }

    /// <summary>
    /// Verifies that concurrent joins by the invited account result in exactly one successful start.
    /// </summary>
    internal void Join_ConcurrentSameInvitedAccount_RegistersOnlyOneSecondSeat()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        AuthenticatedSession friend = CreatePlayer(context, 20, "BetaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, "beta@example.com");
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);
        Match waiting = handler.Invite(
            creator.SessionToken,
            20,
            new MatchSettings()).Value
            ?? throw new InvalidOperationException("Match expected.");

        // Act
        Task<bool>[] attempts = Enumerable.Range(0, 3).Select(_ => Task.Run(() =>
        {
            MatchResult<Match> result = handler.Join(
                friend.SessionToken,
                waiting.Code!);
            return result.Status == MatchStatus.Success;
        })).ToArray();
        Task.WaitAll(attempts);

        // Assert
        int successes = attempts.Count(task => task.Result);
        TestAssert.AreEqual(1, successes);
    }

    /// <summary>
    /// Verifies that cancelling waiting for a private match cancels the match in storage and clears the lobby.
    /// </summary>
    internal void CancelWaitingForSession_PrivateMatchWaiting_CancelsMatchAndRemovesLobby()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, "AlphaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, "beta@example.com");
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);
        Match waiting = handler.Invite(
            creator.SessionToken,
            20,
            new MatchSettings()).Value
            ?? throw new InvalidOperationException("Match expected.");

        // Act
        long[] cancelled = handler.CancelWaitingForSession(creator.SessionToken);

        // Assert
        TestAssert.AreEqual(1, cancelled.Length);
        TestAssert.AreEqual(waiting.Id, cancelled[0]);
        TestAssert.AreEqual(0, handler.CancelWaitingForSession(creator.SessionToken).Length);
    }

    /// <summary>
    /// Verifies that a new session for the same account does not prolong or restore the previous waiting match.
    /// </summary>
    internal void CancelWaitingForSession_DifferentSessionForSameAccount_DoesNotProlongPreviousWaitingMatch()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creatorSession1 = CreatePlayer(context, 10, "AlphaPlayer");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, "beta@example.com");
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        MatchApplicationService handler = new MatchApplicationService(context.Sessions, store, context, mailer);
        Match waiting = handler.Invite(
            creatorSession1.SessionToken,
            20,
            new MatchSettings()).Value
            ?? throw new InvalidOperationException("Match expected.");

        AuthenticatedSession creatorSession2 = CreatePlayer(context, 10, "AlphaPlayer");

        // Act
        long[] cancelled = handler.CancelWaitingForSession(creatorSession1.SessionToken);

        // Assert
        TestAssert.AreEqual(1, cancelled.Length);
        TestAssert.AreEqual(waiting.Id, cancelled[0]);
        TestAssert.AreEqual(0, handler.CancelWaitingForSession(creatorSession2.SessionToken).Length);
    }

    /// <summary>
    /// Verifies that MatchInvitationEmailSender formats the message containing the mandatory legend and code.
    /// </summary>
    internal void EmailSender_CreateMessage_ContainsMandatoryLegendAndMatchCode()
    {
        // Arrange
        WithTestMailSettings(() =>
        {
            MailSettings settings = MailSettings.Load();
            MatchInvitationEmailSender sender = new MatchInvitationEmailSender(settings);
            MatchInvitationEmail email = new MatchInvitationEmail("beta@example.com", "AlphaPlayer", "REV999");

            // Act
            MethodInfo method = TestAssert.IsNotNull(
                typeof(MatchInvitationEmailSender).GetMethod("CreateMessage", BindingFlags.NonPublic | BindingFlags.Instance));
            object? rawResult = method.Invoke(sender, new object[] { email });
            MimeMessage message = (MimeMessage)TestAssert.IsNotNull(rawResult);
            string body = TestAssert.IsNotNull(message.TextBody);

            // Assert
            TestAssert.IsTrue(body.Contains(ExpectedMandatoryLegend), "Body must contain mandatory legend.");
            TestAssert.IsTrue(body.Contains("REV999"), "Body must contain the match code.");
            TestAssert.IsTrue(body.Contains("AlphaPlayer"), "Body must contain creator display name.");
        });
    }

    private static void WithTestMailSettings(Action action)
    {
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            ["REVERSI_MAIL_HOST"] = "127.0.0.1",
            ["REVERSI_MAIL_PORT"] = "587",
            ["REVERSI_MAIL_SECURITY"] = "StartTls",
            ["REVERSI_MAIL_USERNAME"] = "test-user",
            ["REVERSI_MAIL_PASSWORD"] = "test-password",
            ["REVERSI_MAIL_FROM_ADDRESS"] = "system@reversi.example.com",
            ["REVERSI_MAIL_FROM_NAME"] = "Reversi System"
        };
        Dictionary<string, string?> previous = new Dictionary<string, string?>();
        try
        {
            foreach (KeyValuePair<string, string> entry in values)
            {
                previous.Add(entry.Key, Environment.GetEnvironmentVariable(entry.Key));
                Environment.SetEnvironmentVariable(entry.Key, entry.Value);
            }
            action();
        }
        finally
        {
            foreach (KeyValuePair<string, string?> entry in previous)
            {
                Environment.SetEnvironmentVariable(entry.Key, entry.Value);
            }
        }
    }

    private static AuthenticatedSession CreatePlayer(
        SessionTestContext context, long accountId, string username)
    {
        return context.Sessions.Create(new LoginAccount(
            accountId, username, username.ToLowerInvariant() + "@example.com", "hash", "ACTIVE", "en-US"));
    }

    private sealed class InMemoryInvitationSender : IMatchInvitationEmailSender
    {
        internal List<MatchInvitationEmail> SentEmails { get; } = new List<MatchInvitationEmail>();
        internal bool FailSend { get; set; }
        internal Action? BeforeSendCallback { get; set; }

        public void Send(MatchInvitationEmail email)
        {
            BeforeSendCallback?.Invoke();
            if (FailSend)
            {
                throw new InvalidOperationException("Simulated provider outage.");
            }
            SentEmails.Add(email);
        }
    }

    internal sealed class MockMatchRow
    {
        internal Match Match { get; set; } = null!;
        internal long InvitedAccountId { get; set; }
        internal bool IsConfirmed { get; set; }
    }

    private sealed class InvitationMockStore : IMatchRepository
    {
        public MatchStatus EvaluateLobbyAccess(long accountId)
        {
            if (!Accounts.TryGetValue(accountId, out (string Email, bool IsActive) account)
                || !account.IsActive)
            {
                return MatchStatus.InactiveAccount;
            }

            return MatchStatus.Success;
        }
        private readonly object _sync = new object();
        private long _nextId = 100;
        internal Dictionary<long, MockMatchRow> Rows { get; } = new Dictionary<long, MockMatchRow>();
        internal Dictionary<long, (string Email, bool IsActive)> Accounts { get; } =
            new Dictionary<long, (string Email, bool IsActive)>();
        internal HashSet<(long A, long B)> Friendships { get; } = new HashSet<(long A, long B)>();
        internal int CancelCount { get; private set; }
        internal int ConfirmedCount { get; private set; }
        internal bool FailCancel { get; set; }

        internal void RegisterFriendship(long accountA, long accountB, string friendEmail)
        {
            Accounts[accountA] = ("creator@example.com", true);
            Accounts[accountB] = (friendEmail, true);
            Friendships.Add((accountA, accountB));
            Friendships.Add((accountB, accountA));
        }

        internal void RegisterAccount(long accountId, string email, bool isActive)
        {
            Accounts[accountId] = (email, isActive);
        }

        public MatchResult<Match> Create(AuthenticatedSession player, MatchSettings settings, DateTime now)
        {
            lock (_sync)
            {
                long id = ++_nextId;
                string code = "AB" + (id % 1000).ToString("D4").Replace('0', '2').Replace('1', '3');
                Match match = new Match
                {
                    Id = id,
                    Code = code,
                    State = "Waiting",
                    TurnDurationSeconds = settings.TurnDurationSeconds,
                    IsPrivate = false,
                    Participants = new[]
                    {
                        new MatchParticipant
                        {
                            AccountId = player.AccountId,
                            SeatNumber = 1,
                            DisplayName = player.Username
                        }
                    }
                };
                Rows[id] = new MockMatchRow { Match = match, IsConfirmed = true };
                return new MatchResult<Match>(match);
            }
        }

        public MatchResult<Match[]> Available(long accountId)
        {
            lock (_sync)
            {
                Match[] matches = Rows.Values
                    .Where(row => !row.Match.IsPrivate && row.Match.State == "Waiting")
                    .Select(row => row.Match)
                    .ToArray();
                return new MatchResult<Match[]>(matches);
            }
        }

        public MatchResult<Match> Join(AuthenticatedSession player, string code, DateTime now)
        {
            lock (_sync)
            {
                MockMatchRow? row = Rows.Values.FirstOrDefault(r => r.Match.Code == code);
                if (row == null || row.Match.State != "Waiting")
                {
                    return new MatchResult<Match>(MatchStatus.MatchCodeUnavailable);
                }
                if (row.Match.IsPrivate && !row.IsConfirmed)
                {
                    return new MatchResult<Match>(MatchStatus.MatchCodeUnavailable);
                }
                if (row.Match.IsPrivate && row.InvitedAccountId != player.AccountId)
                {
                    return new MatchResult<Match>(MatchStatus.Unauthorized);
                }
                if (row.Match.Participants[0].AccountId == player.AccountId)
                {
                    return new MatchResult<Match>(MatchStatus.CannotJoinOwnMatch);
                }

                row.Match.State = "InProgress";
                row.Match.Participants = new[]
                {
                    row.Match.Participants[0],
                    new MatchParticipant
                    {
                        AccountId = player.AccountId,
                        SeatNumber = 2,
                        DisplayName = player.Username
                    }
                };
                return new MatchResult<Match>(row.Match);
            }
        }

        public MatchResult<MatchInvitationPreparation> PreparePrivateInvitation(
            AuthenticatedSession creator,
            long friendAccountId,
            MatchSettings settings,
            DateTime now)
        {
            lock (_sync)
            {
                if (Accounts.TryGetValue(creator.AccountId, out (string Email, bool IsActive) creatorInfo) && !creatorInfo.IsActive)
                {
                    return new MatchResult<MatchInvitationPreparation>(MatchStatus.InactiveAccount);
                }
                if (friendAccountId <= 0)
                {
                    return new MatchResult<MatchInvitationPreparation>(MatchStatus.FriendAccountUnavailable);
                }
                if (friendAccountId == creator.AccountId)
                {
                    return new MatchResult<MatchInvitationPreparation>(MatchStatus.Unauthorized);
                }
                if (!Accounts.TryGetValue(friendAccountId, out (string Email, bool IsActive) friendInfo) || !friendInfo.IsActive)
                {
                    return new MatchResult<MatchInvitationPreparation>(MatchStatus.FriendAccountUnavailable);
                }
                if (!Friendships.Contains((creator.AccountId, friendAccountId)))
                {
                    return new MatchResult<MatchInvitationPreparation>(MatchStatus.FriendshipNotFound);
                }

                long id = ++_nextId;
                string code = "PR" + (id % 1000).ToString("D4").Replace('0', '2').Replace('1', '3');
                Match match = new Match
                {
                    Id = id,
                    Code = code,
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
                Rows[id] = new MockMatchRow
                {
                    Match = match,
                    InvitedAccountId = friendAccountId,
                    IsConfirmed = false
                };

                MatchInvitationPreparation preparation = new MatchInvitationPreparation(match, friendInfo.Email);
                return new MatchResult<MatchInvitationPreparation>(preparation);
            }
        }

        public MatchResult<Match> MarkInvitationDelivered(
            long accountId,
            long matchId,
            DateTime now)
        {
            lock (_sync)
            {
                if (!Rows.TryGetValue(matchId, out MockMatchRow? row) || row.Match.State != "Waiting")
                {
                    return new MatchResult<Match>(MatchStatus.MatchNotWaiting);
                }

                row.IsConfirmed = true;
                ConfirmedCount++;
                return new MatchResult<Match>(row.Match);
            }
        }

        public MatchStatus ValidateGuestJoin(string matchCode)
        {
            lock (_sync)
            {
                MockMatchRow? row = Rows.Values.FirstOrDefault(r => r.Match.Code == matchCode);
                if (row != null && row.Match.IsPrivate)
                {
                    return MatchStatus.Unauthorized;
                }

                return MatchStatus.Success;
            }
        }

        public MatchStatus Cancel(long accountId, long matchId)
        {
            lock (_sync)
            {
                CancelCount++;
                if (FailCancel)
                {
                    throw new InvalidOperationException("Simulated cancel failure.");
                }
                if (Rows.TryGetValue(matchId, out MockMatchRow? row))
                {
                    row.Match.State = "Cancelled";
                }

                return MatchStatus.Success;
            }
        }

        public MatchResult<Match> Get(long accountId, long matchId)
        {
            lock (_sync)
            {
                if (!Rows.TryGetValue(matchId, out MockMatchRow? row))
                {
                    return new MatchResult<Match>(MatchStatus.MatchNotFound);
                }

                return new MatchResult<Match>(row.Match);
            }
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

        public MatchResult<Match> ChangeTurnDuration(long accountId, long matchId, int seconds)
        {
            throw new NotSupportedException();
        }
    }
}
