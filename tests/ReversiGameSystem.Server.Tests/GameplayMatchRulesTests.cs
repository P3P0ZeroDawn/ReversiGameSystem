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
    public void InitializeStartedMatch_ActiveMatch_SetsOpeningBoardAndBlackTurn()
    {
        Match match = CreateInProgressMatch(40);

        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);

        TestAssert.AreEqual(0L, match.MatchStateVersion);
        TestAssert.AreEqual<int?>(ReversiEngine.Black, match.CurrentTurnSeatNumber);
        TestAssert.AreEqual(BaseUtc, match.TurnStartedAtUtc);
        TestAssert.AreEqual(ReversiEngine.CellCount, match.Board.Length);
        TestAssert.AreEqual(ReversiEngine.White, match.Board[(3 * 8) + 3]);
        TestAssert.AreEqual(ReversiEngine.Black, match.Board[(3 * 8) + 4]);
    }

    [TestMethod]
    public void ValidatePlacement_NotInProgress_ReturnsMatchNotInProgress()
    {
        Match match = CreateInProgressMatch(40);
        match.State = "Waiting";
        MatchPlacement placement = new MatchPlacement(match.Id, 0, 2, 3);

        MatchResult<MatchParticipant> result = GameplayMatchRules.ValidatePlacement(
            match,
            101L,
            placement);

        TestAssert.AreEqual(MatchStatus.MatchNotInProgress, result.Status);
    }

    [TestMethod]
    public void ValidatePlacement_ValidTurnAndVersion_ReturnsAuthorizedParticipant()
    {
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        MatchPlacement placement = new MatchPlacement(match.Id, 0, 2, 3);

        MatchResult<MatchParticipant> result = GameplayMatchRules.ValidatePlacement(
            match,
            101L,
            placement);

        TestAssert.AreEqual(MatchStatus.Success, result.Status);
        TestAssert.AreEqual(1, result.Value?.SeatNumber);
    }

    [TestMethod]
    public void ApplyPlayerMove_ValidMove_UpdatesStateWithoutMutatingSnapshot()
    {
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        Match beforeMoveSnapshot = match.CreateSnapshot();
        MatchParticipant blackPlayer = match.Participants[0];
        blackPlayer.ConsecutiveTimeoutCount = 2;

        MatchStatus status = GameplayMatchRules.ApplyPlayerMove(
            match,
            new MatchPlacement(match.Id, 0, 2, 3),
            BaseUtc.AddSeconds(5));

        TestAssert.AreEqual(MatchStatus.Success, status);
        TestAssert.AreEqual(1L, match.MatchStateVersion);
        TestAssert.AreEqual(0, blackPlayer.ConsecutiveTimeoutCount);
        TestAssert.AreEqual<int?>(ReversiEngine.White, match.CurrentTurnSeatNumber);
        TestAssert.AreEqual(1, match.Moves.Count);
        TestAssert.AreEqual(0L, beforeMoveSnapshot.MatchStateVersion);
        TestAssert.AreEqual(0, beforeMoveSnapshot.Moves.Count);
        TestAssert.AreEqual(ReversiEngine.Empty, beforeMoveSnapshot.Board[(2 * 8) + 3]);
    }

    [TestMethod]
    public void ApplyTimeoutIfExpired_ThirdTimeout_ConcludesInactivityAndAdvancesVersion()
    {
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);
        match.MatchStateVersion = 4;
        match.Participants[0].ConsecutiveTimeoutCount = 2;

        bool changed = GameplayMatchRules.ApplyTimeoutIfExpired(
            match,
            BaseUtc.AddSeconds(41));

        TestAssert.IsTrue(changed, "Expired turn should produce a transition.");
        TestAssert.AreEqual("Finished", match.State);
        TestAssert.AreEqual("INACTIVITY", match.EndReason);
        TestAssert.AreEqual(5L, match.MatchStateVersion);
        TestAssert.AreEqual<int?>(2, match.WinnerSeatNumber);
        TestAssert.AreEqual<int?>(1, match.AbandoningSeatNumber);
    }

    [TestMethod]
    public void ForfeitByParticipant_SecondCall_ReturnsMatchFinishedWithoutMutatingVersion()
    {
        Match match = CreateInProgressMatch(40);
        GameplayMatchRules.InitializeStartedMatch(match, BaseUtc);

        MatchStatus first = GameplayMatchRules.ForfeitByParticipant(
            match,
            101L,
            BaseUtc.AddSeconds(10));
        MatchStatus second = GameplayMatchRules.ForfeitByParticipant(
            match,
            101L,
            BaseUtc.AddSeconds(12));

        TestAssert.AreEqual(MatchStatus.Success, first);
        TestAssert.AreEqual(MatchStatus.MatchFinished, second);
        TestAssert.AreEqual(1L, match.MatchStateVersion);
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
        Match snapshot = match.CreateSnapshot();

        match.Board[(2 * 8) + 3] = ReversiEngine.Empty;
        match.Moves[0].Row = 7;
        match.Participants[0].ConsecutiveTimeoutCount = 3;
        match.Moves.Clear();

        TestAssert.AreEqual(ReversiEngine.Black, snapshot.Board[(2 * 8) + 3]);
        TestAssert.AreEqual(1, snapshot.Moves.Count);
        TestAssert.AreEqual(2, snapshot.Moves[0].Row);
        TestAssert.AreEqual(0, snapshot.Participants[0].ConsecutiveTimeoutCount);
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
