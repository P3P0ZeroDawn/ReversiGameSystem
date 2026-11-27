using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.GameplayMatchApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class GameplayMatchApplicationServicePlayTests
    : GameplayMatchApplicationServiceTestBase
{
    [TestMethod]
    public void TrackStartedMatch_NullMatch_ThrowsArgumentNullException()
    {
        Action action = () => Service.TrackStartedMatch(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void TrackStartedMatch_ValidMatch_MarksMatchTrackedInProgress()
    {
        Match match = CreateTwoPlayerMatch(100L);

        Service.TrackStartedMatch(match);
        bool actualInProgress = Service.IsTrackedMatchInProgress(100L);

        Assert.IsTrue(actualInProgress);
    }

    [TestMethod]
    public void PlacePiece_NullPlacement_ReturnsMissingPlacement()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        MatchStatus expectedStatus = MatchStatus.MissingPlacement;

        MatchResult<Match> actualResult = Service.PlacePiece(token, null);
        MatchStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void PlacePiece_NegativeExpectedVersion_ReturnsInvalidMatchStateVersion()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        MatchPlacement placement = new MatchPlacement(100L, -1L, 2, 3);
        MatchStatus expectedStatus = MatchStatus.InvalidMatchStateVersion;

        MatchResult<Match> actualResult = Service.PlacePiece(token, placement);
        MatchStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void PlacePiece_StaleExpectedVersion_ReturnsStaleMatchStateVersion()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        Match match = CreateTwoPlayerMatch(100L);
        Service.TrackStartedMatch(match);
        MatchPlacement placement = new MatchPlacement(100L, 99L, 2, 3);
        MatchStatus expectedStatus = MatchStatus.StaleMatchStateVersion;

        MatchResult<Match> actualResult = Service.PlacePiece(token, placement);
        MatchStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void PlacePiece_WrongPlayerTurn_ReturnsNotPlayersTurn()
    {
        string playerTwoToken = AddAuthenticatedSession(20L, "PlayerTwo");
        Match match = CreateTwoPlayerMatch(100L);
        Service.TrackStartedMatch(match);
        MatchPlacement placement = new MatchPlacement(100L, match.MatchStateVersion, 2, 3);
        MatchStatus expectedStatus = MatchStatus.NotPlayersTurn;

        MatchResult<Match> actualResult = Service.PlacePiece(playerTwoToken, placement);
        MatchStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void PlacePiece_ValidOpeningMove_AdvancesVersionAndSwitchesTurn()
    {
        string playerOneToken = AddAuthenticatedSession(10L, "PlayerOne");
        Match match = CreateTwoPlayerMatch(100L);
        Service.TrackStartedMatch(match);
        long initialVersion = match.MatchStateVersion;
        MatchPlacement placement = new MatchPlacement(100L, initialVersion, 2, 3);
        MatchStatus expectedStatus = MatchStatus.Success;
        long expectedVersion = initialVersion + 1;
        int expectedNextTurnSeat = 2;

        MatchResult<Match> actualResult = Service.PlacePiece(playerOneToken, placement);
        MatchStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsNotNull(actualResult.Value);
        long actualVersion = actualResult.Value.MatchStateVersion;
        int? actualNextTurnSeat = actualResult.Value.CurrentTurnSeatNumber;
        Assert.AreEqual(expectedVersion, actualVersion);
        Assert.AreEqual(expectedNextTurnSeat, actualNextTurnSeat);
    }

    [TestMethod]
    public void ForfeitMatch_InProgressMatch_FinishesMatchAndSetsWinner()
    {
        string playerOneToken = AddAuthenticatedSession(10L, "PlayerOne");
        Match match = CreateTwoPlayerMatch(100L);
        Service.TrackStartedMatch(match);
        MatchStatus expectedStatus = MatchStatus.Success;
        string expectedState = "Finished";
        int expectedWinnerSeat = 2;

        MatchResult<Match> actualResult = Service.ForfeitMatch(playerOneToken, 100L);
        MatchStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsNotNull(actualResult.Value);
        string actualState = actualResult.Value.State;
        int? actualWinnerSeat = actualResult.Value.WinnerSeatNumber;
        Assert.AreEqual(expectedState, actualState);
        Assert.AreEqual(expectedWinnerSeat, actualWinnerSeat);
    }

    [TestMethod]
    public void ForfeitDisconnectedParticipant_UnknownSeat_ReturnsUnauthorized()
    {
        Match match = CreateTwoPlayerMatch(100L);
        Service.TrackStartedMatch(match);
        MatchStatus expectedStatus = MatchStatus.Unauthorized;

        MatchResult<Match> actualResult = Service.ForfeitDisconnectedParticipant(100L, 99);
        MatchStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void ProcessTurnTimeouts_BeforeTurnDeadline_ReturnsEmptyArray()
    {
        Match match = CreateTwoPlayerMatch(100L, turnDurationSeconds: 40);
        Service.TrackStartedMatch(match);
        Clock.UtcNow = Clock.UtcNow.AddSeconds(20);
        int expectedCount = 0;

        Match[] actualUpdates = Service.ProcessTurnTimeouts();
        int actualCount = actualUpdates.Length;

        Assert.AreEqual(expectedCount, actualCount);
    }

    [TestMethod]
    public void ProcessTurnTimeouts_AfterTurnDeadline_AppliesTimeoutAndReturnsSnapshot()
    {
        Match match = CreateTwoPlayerMatch(100L, turnDurationSeconds: 40);
        Service.TrackStartedMatch(match);
        Clock.UtcNow = Clock.UtcNow.AddSeconds(41);
        int expectedCount = 1;

        Match[] actualUpdates = Service.ProcessTurnTimeouts();
        int actualCount = actualUpdates.Length;

        Assert.AreEqual(expectedCount, actualCount);
    }

    [TestMethod]
    public void SynchronizePendingMatches_WhenRepositoryFails_RespectsRetryInterval()
    {
        string playerOneToken = AddAuthenticatedSession(10L, "PlayerOne");
        Match match = CreateTwoPlayerMatch(100L);
        Service.TrackStartedMatch(match);
        Service.PlacePiece(playerOneToken, new MatchPlacement(100L, match.MatchStateVersion, 2, 3));
        Matches.SynchronizeMatchResult = false;
        int expectedCallsBeforeRetry = 1;

        Service.SynchronizePendingMatches();
        Clock.UtcNow = Clock.UtcNow.AddSeconds(2);
        Service.SynchronizePendingMatches();
        int actualCallsBeforeRetry = Matches.SynchronizeMatchCalls;

        Assert.AreEqual(expectedCallsBeforeRetry, actualCallsBeforeRetry);
    }

    [TestMethod]
    public void SynchronizePendingMatches_FinishedMatchSynced_UntracksMatch()
    {
        string playerOneToken = AddAuthenticatedSession(10L, "PlayerOne");
        Match match = CreateTwoPlayerMatch(100L);
        Service.TrackStartedMatch(match);
        Service.ForfeitMatch(playerOneToken, 100L);
        Matches.SynchronizeMatchResult = true;

        Service.SynchronizePendingMatches();
        bool actualStillTracked = Service.IsTrackedMatchInProgress(100L);

        Assert.IsFalse(actualStillTracked);
    }
}
