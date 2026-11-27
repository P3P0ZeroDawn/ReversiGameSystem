using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Domain.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Domain;

/// <summary>
/// Verifies pure in-memory gameplay rules and isolated match snapshots without service mocks.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class GameplayMatchRulesTests
{
    private const string InProgressState = "InProgress";
    private const string FinishedState = "Finished";
    private const string InactivityReason = "INACTIVITY";

    private static readonly DateTime BaseUtc =
        new DateTime(2026, 11, 15, 18, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AutomaticMove_OpeningBoard_SelectsLegalPlacement(bool isSystem)
    {
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        match.Participants[0].IsSystem = isSystem;
        int[] originalBoard = (int[])match.Board.Clone();
        DateTime expiredTurn = BaseUtc.AddSeconds(match.TurnDurationSeconds);
        int expectedMoveCount = 1;

        bool applied = isSystem
            ? GameplayMatchRules.ApplySystemMoveIfReady(match, expiredTurn)
            : GameplayMatchRules.ApplyTimeoutIfExpired(match, expiredTurn);

        Assert.IsTrue(applied);
        Assert.AreEqual(expectedMoveCount, match.Moves.Count);
        MatchMove move = match.Moves[0];
        Assert.IsTrue(ReversiEngine.IsLegalMove(
            originalBoard, move.Row, move.Column, ReversiEngine.Black));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ApplySystemMoveIfReady_AccountWithoutIdentifier_UsesParticipantKind(bool isSystem)
    {
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        match.Participants[0].AccountId = 0;
        match.Participants[0].IsSystem = isSystem;
        int expectedMoveCount = isSystem ? 1 : 0;

        bool applied = GameplayMatchRules.ApplySystemMoveIfReady(
            match,
            BaseUtc.Add(GameplayMatchRules.SystemMoveDelay));

        Assert.AreEqual(isSystem, applied);
        Assert.AreEqual(expectedMoveCount, match.Moves.Count);
    }

    [TestMethod]
    public void InitializeStartedMatch_ActiveMatch_SetsOpeningBoardAndBlackTurn()
    {
        Match match = CreateInProgressMatch(40);
        long expectedVersion = 0L;
        int? expectedTurnSeat = ReversiEngine.Black;

        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);

        Assert.AreEqual(expectedVersion, match.MatchStateVersion);
        Assert.AreEqual(expectedTurnSeat, match.CurrentTurnSeatNumber);
        Assert.AreEqual(BaseUtc, match.TurnStartedAtUtc);
        Assert.AreEqual(ReversiEngine.CellCount, match.Board.Length);
        Assert.AreEqual(ReversiEngine.White, match.Board[(3 * 8) + 3]);
        Assert.AreEqual(ReversiEngine.Black, match.Board[(3 * 8) + 4]);
    }

    [TestMethod]
    public void ValidatePlacement_NotInProgress_ReturnsMatchNotInProgress()
    {
        Match match = CreateInProgressMatch(40);
        match.State = "Waiting";
        MatchPlacement placement = new MatchPlacement(match.Id, 0, 2, 3);
        MatchStatus expectedStatus = MatchStatus.MatchNotInProgress;

        MatchResult<MatchParticipant> result = GameplayMatchRules.ValidatePlacement(
            match,
            101L,
            placement);

        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void ValidatePlacement_ValidTurnAndVersion_ReturnsAuthorizedParticipant()
    {
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        MatchPlacement placement = new MatchPlacement(match.Id, 0, 2, 3);
        MatchStatus expectedStatus = MatchStatus.Success;
        int expectedSeatNumber = 1;

        MatchResult<MatchParticipant> result = GameplayMatchRules.ValidatePlacement(
            match,
            101L,
            placement);

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.AreEqual(expectedSeatNumber, result.Value?.SeatNumber);
    }

    [TestMethod]
    public void ApplyPlayerMove_ValidMove_UpdatesStateWithoutMutatingSnapshot()
    {
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        Match beforeMoveSnapshot = match.CreateSnapshot();
        MatchParticipant blackPlayer = match.Participants[0];
        blackPlayer.ConsecutiveTimeoutCount = 2;
        MatchStatus expectedStatus = MatchStatus.Success;
        long expectedVersion = 1L;
        int expectedTimeouts = 0;
        int? expectedNextTurnSeat = ReversiEngine.White;
        int expectedMoveCount = 1;
        long expectedSnapshotVersion = 0L;
        int expectedSnapshotMoves = 0;

        MatchStatus status = GameplayMatchRules.ApplyPlayerMove(
            match,
            new MatchPlacement(match.Id, 0, 2, 3),
            BaseUtc.AddSeconds(5));

        Assert.AreEqual(expectedStatus, status);
        Assert.AreEqual(expectedVersion, match.MatchStateVersion);
        Assert.AreEqual(expectedTimeouts, blackPlayer.ConsecutiveTimeoutCount);
        Assert.AreEqual(expectedNextTurnSeat, match.CurrentTurnSeatNumber);
        Assert.AreEqual(expectedMoveCount, match.Moves.Count);
        Assert.AreEqual(expectedSnapshotVersion, beforeMoveSnapshot.MatchStateVersion);
        Assert.AreEqual(expectedSnapshotMoves, beforeMoveSnapshot.Moves.Count);
        Assert.AreEqual(ReversiEngine.Empty, beforeMoveSnapshot.Board[(2 * 8) + 3]);
    }

    [TestMethod]
    public void ApplyTimeoutIfExpired_ThirdTimeout_ConcludesInactivityAndAdvancesVersion()
    {
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        match.MatchStateVersion = 4;
        match.Participants[0].ConsecutiveTimeoutCount = 2;
        long expectedVersion = 5L;
        int? expectedWinnerSeat = 2;
        int? expectedAbandoningSeat = 1;

        bool changed = GameplayMatchRules.ApplyTimeoutIfExpired(
            match,
            BaseUtc.AddSeconds(41));

        Assert.IsTrue(changed, "Expired turn should produce a transition.");
        Assert.AreEqual(FinishedState, match.State);
        Assert.AreEqual(InactivityReason, match.EndReason);
        Assert.AreEqual(expectedVersion, match.MatchStateVersion);
        Assert.AreEqual(expectedWinnerSeat, match.WinnerSeatNumber);
        Assert.AreEqual(expectedAbandoningSeat, match.AbandoningSeatNumber);
    }

    [TestMethod]
    public void ForfeitByParticipant_SecondCall_ReturnsMatchFinishedWithoutMutatingVersion()
    {
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        _ = GameplayMatchRules.ForfeitByParticipant(
            match,
            101L,
            BaseUtc.AddSeconds(10));
        MatchStatus expectedStatus = MatchStatus.MatchFinished;
        long expectedVersion = 1L;

        MatchStatus second = GameplayMatchRules.ForfeitByParticipant(
            match,
            101L,
            BaseUtc.AddSeconds(12));

        Assert.AreEqual(expectedStatus, second);
        Assert.AreEqual(expectedVersion, match.MatchStateVersion);
    }

    [TestMethod]
    public void CreateSnapshot_MutatingOriginalMatch_DoesNotAffectSnapshot()
    {
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        _ = GameplayMatchRules.ApplyPlayerMove(
            match,
            new MatchPlacement(match.Id, 0, 2, 3),
            BaseUtc.AddSeconds(3));
        match.Participants[1].IsSystem = true;
        Match snapshot = match.CreateSnapshot();
        int expectedMoveCount = 1;
        int expectedRow = 2;
        int expectedTimeouts = 0;

        match.Board[(2 * 8) + 3] = ReversiEngine.Empty;
        match.Moves[0].Row = 7;
        match.Participants[0].ConsecutiveTimeoutCount = 3;
        match.Participants[1].IsSystem = false;
        match.Moves.Clear();

        Assert.AreEqual(ReversiEngine.Black, snapshot.Board[(2 * 8) + 3]);
        Assert.AreEqual(expectedMoveCount, snapshot.Moves.Count);
        Assert.AreEqual(expectedRow, snapshot.Moves[0].Row);
        Assert.AreEqual(expectedTimeouts, snapshot.Participants[0].ConsecutiveTimeoutCount);
        Assert.IsTrue(snapshot.Participants[1].IsSystem);
    }

    private static Match CreateInProgressMatch(int turnDurationSeconds)
    {
        return new Match
        {
            Id = 99L,
            Code = "RULE99",
            State = InProgressState,
            TurnDurationSeconds = turnDurationSeconds,
            StartedAtUtc = BaseUtc,
            Participants = new[]
            {
                new MatchParticipant
                {
                    AccountId = 101L,
                    DisplayName = "BlackPlayer",
                    SeatNumber = ReversiEngine.Black
                },
                new MatchParticipant
                {
                    AccountId = 202L,
                    DisplayName = "WhitePlayer",
                    SeatNumber = ReversiEngine.White
                }
            }
        };
    }
}
