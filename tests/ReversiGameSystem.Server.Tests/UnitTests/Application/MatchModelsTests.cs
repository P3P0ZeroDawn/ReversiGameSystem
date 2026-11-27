using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application;

[TestClass]
[TestCategory("Unit")]
public sealed class MatchModelsTests
{
    [TestMethod]
    public void Match_WaitingWithoutExplicitTurn_ReturnsNullCurrentTurnSeatNumber()
    {
        Match match = new Match
        {
            State = "Waiting"
        };

        int? actualSeat = match.CurrentTurnSeatNumber;

        Assert.IsNull(actualSeat);
    }

    [TestMethod]
    public void Match_InProgressWithoutExplicitTurn_DefaultsCurrentTurnSeatNumberToOne()
    {
        int expectedSeat = 1;
        Match match = new Match
        {
            State = "InProgress"
        };

        int? actualSeat = match.CurrentTurnSeatNumber;

        Assert.AreEqual(expectedSeat, actualSeat);
    }

    [TestMethod]
    public void Match_TurnStartedAtUtcNotSet_FallsBackToStartedAtUtc()
    {
        DateTime expectedTime = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);
        Match match = new Match
        {
            StartedAtUtc = expectedTime
        };

        DateTime? actualTime = match.TurnStartedAtUtc;

        Assert.AreEqual(expectedTime, actualTime);
    }

    [TestMethod]
    public void Match_CreateSnapshot_DeepCopiesBoardParticipantsAndMoves()
    {
        DateTime startedAtUtc = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);
        Match original = new Match
        {
            Id = 50L,
            Code = "ABC123",
            State = "InProgress",
            TurnDurationSeconds = 40,
            StartedAtUtc = startedAtUtc,
            Participants = new MatchParticipant[]
            {
                new MatchParticipant
                {
                    SeatNumber = 1,
                    AccountId = 10L,
                    DisplayName = "PlayerOne"
                }
            },
            Moves =
            {
                new MatchMove
                {
                    MoveNumber = 1,
                    SeatNumber = 1,
                    Row = 2,
                    Column = 3,
                    PlayedAtUtc = startedAtUtc,
                    FlippedPieceCount = 1
                }
            },
            LastMove = new MatchMove
            {
                MoveNumber = 1,
                SeatNumber = 1,
                Row = 2,
                Column = 3,
                PlayedAtUtc = startedAtUtc,
                FlippedPieceCount = 1
            }
        };
        int expectedBoardCell = original.Board[0];
        string expectedDisplayName = "PlayerOne";
        int expectedMoveCount = 1;

        Match snapshot = original.CreateSnapshot();
        original.Board[0] = 2;
        original.Participants[0].DisplayName = "Mutated";
        original.Moves.Clear();

        int actualBoardCell = snapshot.Board[0];
        string actualDisplayName = snapshot.Participants[0].DisplayName;
        int actualMoveCount = snapshot.Moves.Count;
        Assert.AreEqual(expectedBoardCell, actualBoardCell);
        Assert.AreEqual(expectedDisplayName, actualDisplayName);
        Assert.AreEqual(expectedMoveCount, actualMoveCount);
    }

    [TestMethod]
    public void MatchInvitationPreparation_NullMatch_ThrowsArgumentNullException()
    {
        Action action = () => _ = new MatchInvitationPreparation(null!, "friend@example.com");

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void MatchInvitationPreparation_NullFriendEmail_ThrowsArgumentNullException()
    {
        Match match = new Match();

        Action action = () => _ = new MatchInvitationPreparation(match, null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void MatchResult_NullValue_ThrowsArgumentNullException()
    {
        Action action = () => _ = new MatchResult<string>((string)null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void MatchResult_ValidValue_SetsSuccessStatusAndValue()
    {
        MatchStatus expectedStatus = MatchStatus.Success;
        string expectedValue = "ok";

        MatchResult<string> result = new MatchResult<string>(expectedValue);
        MatchStatus actualStatus = result.Status;
        string? actualValue = result.Value;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedValue, actualValue);
    }

    [TestMethod]
    public void MatchResult_SuccessStatusWithoutValue_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => _ = new MatchResult<string>(MatchStatus.Success);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [TestMethod]
    public void MatchResult_FailureStatus_SetsStatusAndNullValue()
    {
        MatchStatus expectedStatus = MatchStatus.MatchNotFound;

        MatchResult<string> result = new MatchResult<string>(expectedStatus);
        MatchStatus actualStatus = result.Status;
        string? actualValue = result.Value;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsNull(actualValue);
    }
}
