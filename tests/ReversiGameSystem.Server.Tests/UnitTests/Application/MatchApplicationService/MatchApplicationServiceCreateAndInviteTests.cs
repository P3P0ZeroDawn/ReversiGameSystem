using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Matches;
using ApplicationService =
    ReversiGameSystem.Server.Application.Matches.MatchApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.MatchApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class MatchApplicationServiceCreateAndInviteTests
    : MatchApplicationServiceTestBase
{
    [TestMethod]
    public void Constructor_NullSessions_ThrowsArgumentNullException()
    {
        Action action = () => _ = new ApplicationService(null!, Matches, Clock);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullMatches_ThrowsArgumentNullException()
    {
        Action action = () => _ = new ApplicationService(Sessions, null!, Clock);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullClock_ThrowsArgumentNullException()
    {
        Action action = () => _ = new ApplicationService(Sessions, Matches, null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    [DataRow(null, MatchStatus.MissingSessionToken)]
    [DataRow("invalid-token", MatchStatus.InvalidSession)]
    public void CreateMatch_RejectedSession_ReturnsFailureWithoutRepositoryCall(
        string? sessionToken,
        MatchStatus expectedStatus)
    {
        int expectedCallCount = 0;

        MatchResult<Match> actualResult = Service.CreateMatch(sessionToken!, new MatchSettings());
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.CreateMatchCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void CreateMatch_NullSettings_ReturnsInvalidConfigurationWithoutRepositoryCall()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        MatchStatus expectedStatus = MatchStatus.InvalidConfiguration;
        int expectedCallCount = 0;

        MatchResult<Match> actualResult = Service.CreateMatch(token, null);
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.CreateMatchCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    [DataRow("System", false)]
    [DataRow("Human", true)]
    public void CreateMatch_InvalidSettings_ReturnsInvalidConfigurationWithoutRepositoryCall(
        string opponentKind,
        bool countsTowardStatistics)
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        MatchSettings settings = new MatchSettings
        {
            OpponentKind = opponentKind,
            CountsTowardStatistics = countsTowardStatistics,
            TurnDurationSeconds = 40
        };
        MatchStatus expectedStatus = MatchStatus.InvalidConfiguration;
        int expectedCallCount = 0;

        MatchResult<Match> actualResult = Service.CreateMatch(token, settings);
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.CreateMatchCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    [DataRow(9)]
    [DataRow(121)]
    public void CreateMatch_InvalidTurnDuration_ReturnsInvalidTurnDurationWithoutRepositoryCall(
        int turnDurationSeconds)
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        MatchSettings settings = new MatchSettings
        {
            TurnDurationSeconds = turnDurationSeconds
        };
        MatchStatus expectedStatus = MatchStatus.InvalidTurnDuration;
        int expectedCallCount = 0;

        MatchResult<Match> actualResult = Service.CreateMatch(token, settings);
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.CreateMatchCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void CreateMatch_SecondMatchForSameSession_CancelsPriorTrackedMatch()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        long expectedCancelledMatchId = 100L;
        Matches.CreateMatchResult = new MatchResult<Match>(new Match
        {
            Id = expectedCancelledMatchId
        });
        Service.CreateMatch(token, new MatchSettings());
        Matches.CreateMatchResult = new MatchResult<Match>(new Match
        {
            Id = 101L
        });
        int expectedCancelCalls = 1;

        Service.CreateMatch(token, new MatchSettings());
        int actualCancelCalls = Matches.CancelMatchCalls;
        long actualCancelledMatchId = Matches.LastMatchId;

        Assert.AreEqual(expectedCancelCalls, actualCancelCalls);
        Assert.AreEqual(expectedCancelledMatchId, actualCancelledMatchId);
    }

    [TestMethod]
    public void InviteToMatch_GuestSession_ReturnsUnauthorizedWithoutRepositoryCall()
    {
        string token = AddGuestSession("guest-token");
        MatchStatus expectedStatus = MatchStatus.Unauthorized;
        int expectedCallCount = 0;

        MatchResult<Match> actualResult =
            Service.InviteToMatch(token, 20L, new MatchSettings());
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.PreparePrivateInvitationCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-3L)]
    public void InviteToMatch_NonPositiveFriendAccountId_ReturnsInvalidFriendAccountId(
        long friendAccountId)
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        MatchStatus expectedStatus = MatchStatus.InvalidFriendAccountId;
        int expectedCallCount = 0;

        MatchResult<Match> actualResult =
            Service.InviteToMatch(token, friendAccountId, new MatchSettings());
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.PreparePrivateInvitationCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void InviteToMatch_SelfInvitation_ReturnsUnauthorizedWithoutRepositoryCall()
    {
        long accountId = 10L;
        string token = AddAuthenticatedSession(accountId, "PlayerTen");
        MatchStatus expectedStatus = MatchStatus.Unauthorized;
        int expectedCallCount = 0;

        MatchResult<Match> actualResult =
            Service.InviteToMatch(token, accountId, new MatchSettings());
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.PreparePrivateInvitationCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void InviteToMatch_EmailDeliveryFails_CancelsPreparedMatchAndThrows()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        long expectedMatchId = 77L;
        Matches.PreparePrivateInvitationResult = new MatchResult<MatchInvitationPreparation>(
            new MatchInvitationPreparation(
                new Match
                {
                    Id = expectedMatchId,
                    Code = "ABC234"
                },
                "friend@example.com"));
        EmailSender.Failure = new InvalidOperationException("SMTP failure.");
        int expectedCancelCalls = 1;
        Action action = () => Service.InviteToMatch(token, 20L, new MatchSettings());

        Assert.Throws<InvalidOperationException>(action);
        int actualCancelCalls = Matches.CancelMatchCalls;
        long actualMatchId = Matches.LastMatchId;
        Assert.AreEqual(expectedCancelCalls, actualCancelCalls);
        Assert.AreEqual(expectedMatchId, actualMatchId);
    }

    [TestMethod]
    public void InviteToMatch_EmailAndCancelFail_ThrowsAggregateException()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Matches.PreparePrivateInvitationResult = new MatchResult<MatchInvitationPreparation>(
            new MatchInvitationPreparation(
                new Match
                {
                    Id = 77L,
                    Code = "ABC234"
                },
                "friend@example.com"));
        EmailSender.Failure = new InvalidOperationException("SMTP failure.");
        Matches.CancelMatchStatus = MatchStatus.Unauthorized;
        int expectedInnerCount = 2;
        Action action = () => Service.InviteToMatch(token, 20L, new MatchSettings());

        AggregateException exception = Assert.Throws<AggregateException>(action);
        int actualInnerCount = exception.InnerExceptions.Count;

        Assert.AreEqual(expectedInnerCount, actualInnerCount);
    }

    [TestMethod]
    public void InviteToMatch_ValidInvitation_SendsEmailAndMarksDelivered()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Match expectedMatch = new Match
        {
            Id = 88L,
            Code = "XYZ789"
        };
        string expectedEmail = "friend@example.com";
        Matches.PreparePrivateInvitationResult = new MatchResult<MatchInvitationPreparation>(
            new MatchInvitationPreparation(expectedMatch, expectedEmail));
        Matches.MarkInvitationDeliveredResult = new MatchResult<Match>(expectedMatch);
        MatchStatus expectedStatus = MatchStatus.Success;
        string expectedTemplate = EmailTemplateNames.MatchInvitation;
        int expectedDeliveredCalls = 1;

        MatchResult<Match> actualResult =
            Service.InviteToMatch(token, 20L, new MatchSettings());
        MatchStatus actualStatus = actualResult.Status;
        Match? actualMatch = actualResult.Value;
        string? actualRecipient = EmailSender.LastRecipientAddress;
        string? actualTemplate = EmailSender.LastTemplateName;
        int actualDeliveredCalls = Matches.MarkInvitationDeliveredCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreSame(expectedMatch, actualMatch);
        Assert.AreEqual(expectedEmail, actualRecipient);
        Assert.AreEqual(expectedTemplate, actualTemplate);
        Assert.AreEqual(expectedDeliveredCalls, actualDeliveredCalls);
    }
}
