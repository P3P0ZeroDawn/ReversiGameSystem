using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Domain.Matches;

namespace ReversiGameSystem.Server.Tests;

/// <summary>
/// Verifies pure in-memory gameplay rules and isolated match snapshots without service mocks.
/// </summary>
[TestClass]
public sealed class GameplayMatchRulesTests
{
    private static readonly DateTime BaseUtc =
        new DateTime(2026, 11, 15, 18, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    [DataRow(false, "NO_LEGAL_MOVES")]
    [DataRow(true, "BOARD_FULL")]
    public void ApplyPlayerMove_FinalPlacement_AssignsSupportedEndReason(
        bool fillsBoard, string expectedReason)
    {
        const int TargetRow = 0;
        const int TargetColumn = 0;
        const int OpponentColumn = 1;
        const int FriendlyColumn = 2;
        Match match = CreateInProgressMatch(40);
        match.Board = new int[ReversiEngine.CellCount];
        if (fillsBoard)
        {
            for (int cell = 0; cell < match.Board.Length; cell++)
            {
                match.Board[cell] = ReversiEngine.Black;
            }
        }
        match.Board[TargetColumn] = ReversiEngine.Empty;
        match.Board[OpponentColumn] = ReversiEngine.White;
        match.Board[FriendlyColumn] = ReversiEngine.Black;
        MatchPlacement placement = new MatchPlacement(
            match.Id, match.MatchStateVersion, TargetRow, TargetColumn);

        GameplayMatchRules.ApplyPlayerMove(match, placement, BaseUtc);

        Assert.AreEqual(expectedReason, match.EndReason);
    }

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

        bool applied = isSystem
            ? GameplayMatchRules.ApplySystemMoveIfReady(match, expiredTurn)
            : GameplayMatchRules.ApplyTimeoutIfExpired(match, expiredTurn);

        Assert.AreEqual(true, applied);
        Assert.AreEqual(1, match.Moves.Count);
        MatchMove move = match.Moves[0];
        Assert.AreEqual(true, ReversiEngine.IsLegalMove(
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

        bool applied = GameplayMatchRules.ApplySystemMoveIfReady(
            match,
            BaseUtc.Add(GameplayMatchRules.SystemMoveDelay));

        Assert.AreEqual(isSystem, applied);
        Assert.AreEqual(isSystem ? 1 : 0, match.Moves.Count);
    }

    [TestMethod]
    public void InitializeStartedMatch_ActiveMatch_SetsOpeningBoardAndBlackTurn()
    {
        // Arrange
        Match match = CreateInProgressMatch(40);

        // Act
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);

        // Assert
        Assert.AreEqual(0L, match.MatchStateVersion);
        Assert.AreEqual<int?>(ReversiEngine.Black, match.CurrentTurnSeatNumber);
        Assert.AreEqual(BaseUtc, match.TurnStartedAtUtc);
        Assert.AreEqual(ReversiEngine.CellCount, match.Board.Length);
        Assert.AreEqual(ReversiEngine.White, match.Board[(3 * 8) + 3]);
        Assert.AreEqual(ReversiEngine.Black, match.Board[(3 * 8) + 4]);
    }

    [TestMethod]
    public void ValidatePlacement_NotInProgress_ReturnsMatchNotInProgress()
    {
        // Arrange
        Match match = CreateInProgressMatch(40);
        match.State = "Waiting";
        MatchPlacement placement = new MatchPlacement(match.Id, 0, 2, 3);

        // Act
        MatchResult<MatchParticipant> result = GameplayMatchRules.ValidatePlacement(
            match,
            101L,
            placement);

        // Assert
        Assert.AreEqual(MatchStatus.MatchNotInProgress, result.Status);
    }

    [TestMethod]
    public void ValidatePlacement_ValidTurnAndVersion_ReturnsAuthorizedParticipant()
    {
        // Arrange
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        MatchPlacement placement = new MatchPlacement(match.Id, 0, 2, 3);

        // Act
        MatchResult<MatchParticipant> result = GameplayMatchRules.ValidatePlacement(
            match,
            101L,
            placement);

        // Assert
        Assert.AreEqual(MatchStatus.Success, result.Status);
        Assert.AreEqual(1, result.Value?.SeatNumber);
    }

    [TestMethod]
    public void ApplyPlayerMove_ValidMove_UpdatesStateWithoutMutatingSnapshot()
    {
        // Arrange
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        Match beforeMoveSnapshot = match.CreateSnapshot();
        MatchParticipant blackPlayer = match.Participants[0];
        blackPlayer.ConsecutiveTimeoutCount = 2;

        // Act
        MatchStatus status = GameplayMatchRules.ApplyPlayerMove(
            match,
            new MatchPlacement(match.Id, 0, 2, 3),
            BaseUtc.AddSeconds(5));

        // Assert
        Assert.AreEqual(MatchStatus.Success, status);
        Assert.AreEqual(1L, match.MatchStateVersion);
        Assert.AreEqual(0, blackPlayer.ConsecutiveTimeoutCount);
        Assert.AreEqual<int?>(ReversiEngine.White, match.CurrentTurnSeatNumber);
        Assert.AreEqual(1, match.Moves.Count);
        Assert.AreEqual(0L, beforeMoveSnapshot.MatchStateVersion);
        Assert.AreEqual(0, beforeMoveSnapshot.Moves.Count);
        Assert.AreEqual(ReversiEngine.Empty, beforeMoveSnapshot.Board[(2 * 8) + 3]);
    }

    [TestMethod]
    public void ApplyTimeoutIfExpired_ThirdTimeout_ConcludesInactivityAndAdvancesVersion()
    {
        // Arrange
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        match.MatchStateVersion = 4;
        match.Participants[0].ConsecutiveTimeoutCount = 2;

        // Act
        bool changed = GameplayMatchRules.ApplyTimeoutIfExpired(
            match,
            BaseUtc.AddSeconds(41));

        // Assert
        Assert.IsTrue(changed, "Expired turn should produce a transition.");
        Assert.AreEqual("Finished", match.State);
        Assert.AreEqual("INACTIVITY", match.EndReason);
        Assert.AreEqual(5L, match.MatchStateVersion);
        Assert.AreEqual<int?>(2, match.WinnerSeatNumber);
        Assert.AreEqual<int?>(1, match.AbandoningSeatNumber);
    }

    [TestMethod]
    public void ForfeitByParticipant_SecondCall_ReturnsMatchFinishedWithoutMutatingVersion()
    {
        // Arrange
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        _ = GameplayMatchRules.ForfeitByParticipant(
            match,
            101L,
            BaseUtc.AddSeconds(10));

        // Act
        MatchStatus second = GameplayMatchRules.ForfeitByParticipant(
            match,
            101L,
            BaseUtc.AddSeconds(12));

        // Assert
        Assert.AreEqual(MatchStatus.MatchFinished, second);
        Assert.AreEqual(1L, match.MatchStateVersion);
    }

    [TestMethod]
    public void CreateSnapshot_MutatingOriginalMatch_DoesNotAffectSnapshot()
    {
        // Arrange
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        _ = GameplayMatchRules.ApplyPlayerMove(
            match,
            new MatchPlacement(match.Id, 0, 2, 3),
            BaseUtc.AddSeconds(3));
        match.Participants[1].IsSystem = true;
        Match snapshot = match.CreateSnapshot();

        // Act
        match.Board[(2 * 8) + 3] = ReversiEngine.Empty;
        match.Moves[0].Row = 7;
        match.Participants[0].ConsecutiveTimeoutCount = 3;
        match.Participants[1].IsSystem = false;
        match.Moves.Clear();

        // Assert
        Assert.AreEqual(ReversiEngine.Black, snapshot.Board[(2 * 8) + 3]);
        Assert.AreEqual(1, snapshot.Moves.Count);
        Assert.AreEqual(2, snapshot.Moves[0].Row);
        Assert.AreEqual(0, snapshot.Participants[0].ConsecutiveTimeoutCount);
        Assert.AreEqual(true, snapshot.Participants[1].IsSystem);
    }

    private static Match CreateInProgressMatch(int turnDurationSeconds)
    {
        return new Match
        {
            Id = 99L,
            Code = "RULE99",
            State = "InProgress",
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
