using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.GameplayMatchService;

[TestClass]
[TestCategory("Unit")]
public sealed class GameplayMatchServiceTests : GameplayMatchServiceTestBase
{
    [TestMethod]
    public void Constructor_NullApplicationService_ThrowsArgumentNullException()
    {
        Action action = () => _ = new Server.ServiceApi.GameplayMatchService(
            null!,
            Presence,
            Subscriptions);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullPresence_ThrowsArgumentNullException()
    {
        Action action = () => _ = new Server.ServiceApi.GameplayMatchService(
            ApplicationService,
            null!,
            Subscriptions);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullSubscriptions_ThrowsArgumentNullException()
    {
        Action action = () => _ = new Server.ServiceApi.GameplayMatchService(
            ApplicationService,
            Presence,
            null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    [DataRow(null, SessionFailure.MissingSessionToken)]
    [DataRow("invalid-token", SessionFailure.InvalidSession)]
    public void OpenMatchChannel_RejectedSession_ThrowsSessionFault(
        string? sessionToken,
        SessionFailure expectedFailure)
    {
        Action action = () => Service.OpenMatchChannel(sessionToken!, 100L);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }

    [TestMethod]
    public void OpenMatchChannel_UntrackedFinishedMatch_ThrowsMatchNotInProgressFault()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        Match finished = CreateTwoPlayerMatch(100L);
        finished.State = "Finished";
        Matches.LoadMatchResult = new MatchResult<Match>(finished);
        MatchFailure expectedFailure = MatchFailure.MatchNotInProgress;
        Action action = () => Service.OpenMatchChannel(token, 100L);

        FaultException<MatchFault> exception =
            Assert.Throws<FaultException<MatchFault>>(action);
        MatchFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }

    [TestMethod]
    public void GetMatchInformation_ValidParticipant_ReturnsAssignedSeatAndMatchId()
    {
        string token = AddAuthenticatedSession(20L, "PlayerTwo");
        long expectedMatchId = 100L;
        int expectedSeat = 2;
        ApplicationService.TrackStartedMatch(CreateTwoPlayerMatch(expectedMatchId));

        MatchInformation actualInformation = Service.GetMatchInformation(token, expectedMatchId);
        long actualMatchId = actualInformation.MatchId;
        int? actualSeat = actualInformation.AssignedSeatNumber;

        Assert.AreEqual(expectedMatchId, actualMatchId);
        Assert.AreEqual(expectedSeat, actualSeat);
    }

    [TestMethod]
    public void GetMatchState_TrackedMatch_ReturnsCurrentStateSnapshot()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        long expectedMatchId = 100L;
        ApplicationService.TrackStartedMatch(CreateTwoPlayerMatch(expectedMatchId));
        MatchState expectedState = MatchState.InProgress;

        MatchStateSnapshot actualSnapshot = Service.GetMatchState(token, expectedMatchId);
        long actualMatchId = actualSnapshot.MatchId;
        MatchState actualState = actualSnapshot.State;

        Assert.AreEqual(expectedMatchId, actualMatchId);
        Assert.AreEqual(expectedState, actualState);
    }

    [TestMethod]
    public void PlacePiece_NullPlacement_ThrowsMissingPlacementFault()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        MatchFailure expectedFailure = MatchFailure.MissingPlacement;
        Action action = () => Service.PlacePiece(token, null!);

        FaultException<MatchFault> exception =
            Assert.Throws<FaultException<MatchFault>>(action);
        MatchFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }

    [TestMethod]
    public void PlacePiece_ValidOpeningMove_ReturnsUpdatedStateSnapshot()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        Match match = CreateTwoPlayerMatch(100L);
        ApplicationService.TrackStartedMatch(match);
        PiecePlacement placement = new PiecePlacement
        {
            MatchId = 100L,
            ExpectedMatchStateVersion = match.MatchStateVersion,
            Row = 2,
            Column = 3
        };
        long expectedVersion = match.MatchStateVersion + 1;

        MatchStateSnapshot actualSnapshot = Service.PlacePiece(token, placement);
        long actualVersion = actualSnapshot.MatchStateVersion;

        Assert.AreEqual(expectedVersion, actualVersion);
    }

    [TestMethod]
    public void ForfeitMatch_ValidParticipant_ReturnsAbandonmentConclusion()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        ApplicationService.TrackStartedMatch(CreateTwoPlayerMatch(100L));
        MatchEndReason expectedReason = MatchEndReason.Abandonment;
        int expectedWinnerSeat = 2;

        MatchConclusion actualConclusion = Service.ForfeitMatch(token, 100L);
        MatchEndReason actualReason = actualConclusion.Reason;
        int? actualWinnerSeat = actualConclusion.WinnerSeatNumber;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedWinnerSeat, actualWinnerSeat);
    }

    [TestMethod]
    public void GetMatchHistory_ValidSession_ReturnsMappedSummaries()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        long expectedMatchId = 100L;
        Match finished = CreateTwoPlayerMatch(expectedMatchId);
        finished.State = "Finished";
        Matches.MatchHistoryResult = new MatchResult<Match[]>(new Match[] { finished });
        int expectedCount = 1;

        MatchSummary[] actualSummaries = Service.GetMatchHistory(token);
        int actualCount = actualSummaries.Length;
        long actualMatchId = actualSummaries[0].MatchId;

        Assert.AreEqual(expectedCount, actualCount);
        Assert.AreEqual(expectedMatchId, actualMatchId);
    }

    [TestMethod]
    public void GetMoveHistory_AfterOpeningMove_ReturnsMappedMoveRecords()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        Match match = CreateTwoPlayerMatch(100L);
        ApplicationService.TrackStartedMatch(match);
        Service.PlacePiece(token, new PiecePlacement
        {
            MatchId = 100L,
            ExpectedMatchStateVersion = match.MatchStateVersion,
            Row = 2,
            Column = 3
        });
        int expectedCount = 1;
        int expectedRow = 2;
        int expectedColumn = 3;

        MoveRecord[] actualMoves = Service.GetMoveHistory(token, 100L);
        int actualCount = actualMoves.Length;
        int actualRow = actualMoves[0].Row;
        int actualColumn = actualMoves[0].Column;

        Assert.AreEqual(expectedCount, actualCount);
        Assert.AreEqual(expectedRow, actualRow);
        Assert.AreEqual(expectedColumn, actualColumn);
    }

    [TestMethod]
    public void GetObservableMatches_ValidSession_ReturnsMappedMatchInformation()
    {
        string token = AddAuthenticatedSession(30L, "Spectator");
        long expectedMatchId = 100L;
        ApplicationService.TrackStartedMatch(CreateTwoPlayerMatch(expectedMatchId));
        int expectedCount = 1;

        MatchInformation[] actualMatches = Service.GetObservableMatches(token);
        int actualCount = actualMatches.Length;
        long actualMatchId = actualMatches[0].MatchId;

        Assert.AreEqual(expectedCount, actualCount);
        Assert.AreEqual(expectedMatchId, actualMatchId);
    }

    [TestMethod]
    public void GetMatchDetails_AfterOpeningMove_ReturnsDetailsWithMovesAndBoardFrames()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        Match match = CreateTwoPlayerMatch(100L);
        ApplicationService.TrackStartedMatch(match);
        Service.PlacePiece(token, new PiecePlacement
        {
            MatchId = 100L,
            ExpectedMatchStateVersion = match.MatchStateVersion,
            Row = 2,
            Column = 3
        });
        int expectedMoveCount = 1;
        int expectedFrameCount = 2;

        MatchDetails actualDetails = Service.GetMatchDetails(token, 100L);
        int actualMoveCount = actualDetails.Moves.Length;
        int actualFrameCount = actualDetails.BoardFrames.Length;

        Assert.AreEqual(expectedMoveCount, actualMoveCount);
        Assert.AreEqual(expectedFrameCount, actualFrameCount);
    }
}
