using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class MatchContractMapperTests
{
    [TestMethod]
    public void MapState_InProgressMatch_MapsStateVersionTurnAndNullConclusion()
    {
        DateTime startedAtUtc = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);
        long expectedMatchId = 10L;
        long expectedVersion = 2L;
        MatchState expectedState = MatchState.InProgress;
        int expectedTurnSeat = 2;
        int expectedBlackCount = 2;
        int expectedWhiteCount = 2;
        Match match = new Match
        {
            Id = expectedMatchId,
            State = "InProgress",
            MatchStateVersion = expectedVersion,
            CurrentTurnSeatNumber = expectedTurnSeat,
            StartedAtUtc = startedAtUtc
        };

        MatchStateSnapshot actualSnapshot = MatchContractMapper.MapState(match);
        long actualMatchId = actualSnapshot.MatchId;
        long actualVersion = actualSnapshot.MatchStateVersion;
        MatchState actualState = actualSnapshot.State;
        int? actualTurnSeat = actualSnapshot.CurrentTurnSeatNumber;
        int actualBlackCount = actualSnapshot.BlackPieceCount;
        int actualWhiteCount = actualSnapshot.WhitePieceCount;

        Assert.AreEqual(expectedMatchId, actualMatchId);
        Assert.AreEqual(expectedVersion, actualVersion);
        Assert.AreEqual(expectedState, actualState);
        Assert.AreEqual(expectedTurnSeat, actualTurnSeat);
        Assert.AreEqual(expectedBlackCount, actualBlackCount);
        Assert.AreEqual(expectedWhiteCount, actualWhiteCount);
        Assert.IsNull(actualSnapshot.Conclusion);
    }

    [TestMethod]
    public void MapState_FinishedMatch_IncludesConclusionAndWinner()
    {
        DateTime endedAtUtc = new DateTime(2026, 11, 8, 12, 30, 0, DateTimeKind.Utc);
        MatchEndReason expectedReason = MatchEndReason.Abandonment;
        int expectedWinnerSeat = 2;
        int expectedAbandoningSeat = 1;
        Match match = new Match
        {
            Id = 20L,
            State = "Finished",
            MatchStateVersion = 3L,
            EndReason = "ABANDONMENT",
            WinnerSeatNumber = expectedWinnerSeat,
            AbandoningSeatNumber = expectedAbandoningSeat,
            EndedAtUtc = endedAtUtc
        };

        MatchStateSnapshot actualSnapshot = MatchContractMapper.MapState(match);

        Assert.IsNotNull(actualSnapshot.Conclusion);
        MatchEndReason actualReason = actualSnapshot.Conclusion.Reason;
        int? actualWinnerSeat = actualSnapshot.Conclusion.WinnerSeatNumber;
        int? actualAbandoningSeat = actualSnapshot.Conclusion.AbandoningSeatNumber;
        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedWinnerSeat, actualWinnerSeat);
        Assert.AreEqual(expectedAbandoningSeat, actualAbandoningSeat);
    }

    [TestMethod]
    [DataRow("Player", MoveOrigin.Manual)]
    [DataRow("Timeout", MoveOrigin.Timeout)]
    [DataRow("System", MoveOrigin.System)]
    public void MapState_WithLastMove_MapsMoveOrigin(string origin, MoveOrigin expectedOrigin)
    {
        Match match = new Match
        {
            Id = 30L,
            State = "InProgress",
            LastMove = new MatchMove
            {
                MoveNumber = 1,
                SeatNumber = 1,
                Row = 2,
                Column = 3,
                PlayedAtUtc = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc),
                FlippedPieceCount = 1,
                Origin = origin
            }
        };

        MatchStateSnapshot actualSnapshot = MatchContractMapper.MapState(match);

        Assert.IsNotNull(actualSnapshot.LastMove);
        MoveOrigin actualOrigin = actualSnapshot.LastMove.Origin;
        Assert.AreEqual(expectedOrigin, actualOrigin);
    }
}
