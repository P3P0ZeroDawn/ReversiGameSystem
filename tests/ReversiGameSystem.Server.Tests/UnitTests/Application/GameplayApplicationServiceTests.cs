using System;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Domain.Matches;
using AppGameplayMatchService =
    ReversiGameSystem.Server.Application.Matches.GameplayMatchApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application;

/// <summary>
/// Verifies authoritative in-memory match management, placement rules, timeout limits and replay.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class GameplayApplicationServiceTests
{
    private const string InProgressState = "InProgress";
    private const string FinishedState = "Finished";
    private const string TimeoutOrigin = "Timeout";
    private const string PlayerOrigin = "Player";

    [TestMethod]
    public void TrackStartedMatch_ValidMatch_InitializesBoardAndTurn()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        long expectedVersion = 0L;
        int? expectedTurnSeat = 1;
        int expectedBoardLength = 64;

        context.ApplicationService.TrackStartedMatch(match);

        Assert.AreEqual(expectedVersion, match.MatchStateVersion);
        Assert.AreEqual(expectedTurnSeat, match.CurrentTurnSeatNumber);
        Assert.AreEqual(expectedBoardLength, match.Board.Length);
        Assert.AreEqual(ReversiEngine.White, match.Board[27]);
        Assert.AreEqual(ReversiEngine.Black, match.Board[28]);
        Assert.AreEqual(ReversiEngine.Black, match.Board[35]);
        Assert.AreEqual(ReversiEngine.White, match.Board[36]);
        Assert.AreEqual(context.Clock.UtcNow, match.TurnStartedAtUtc);
    }

    [TestMethod]
    public void PlacePiece_MissingPlacement_ReturnsMissingPlacement()
    {
        GameplayTestContext context = new GameplayTestContext();
        MatchStatus expectedStatus = MatchStatus.MissingPlacement;

        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            null);

        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void PlacePiece_NegativeVersion_ReturnsInvalidMatchStateVersion()
    {
        GameplayTestContext context = new GameplayTestContext();
        MatchPlacement placement = new MatchPlacement(10, -1, 2, 3);
        MatchStatus expectedStatus = MatchStatus.InvalidMatchStateVersion;

        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            placement);

        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void PlacePiece_InvalidSession_ReturnsInvalidSession()
    {
        GameplayTestContext context = new GameplayTestContext();
        MatchPlacement placement = new MatchPlacement(10, 0, 2, 3);
        MatchStatus expectedStatus = MatchStatus.InvalidSession;

        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            "non-existent-token",
            placement);

        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void PlacePiece_NotPlayersTurn_ReturnsNotPlayersTurn()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        MatchPlacement placement = new MatchPlacement(match.Id, 0, 2, 4);
        MatchStatus expectedStatus = MatchStatus.NotPlayersTurn;

        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerTwo.SessionToken,
            placement);

        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void PlacePiece_StaleVersion_ReturnsStaleMatchStateVersion()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        MatchPlacement placement = new MatchPlacement(match.Id, 1, 2, 3);
        MatchStatus expectedStatus = MatchStatus.StaleMatchStateVersion;

        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            placement);

        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void PlacePiece_InvalidPosition_ReturnsInvalidPosition()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        MatchPlacement placement = new MatchPlacement(match.Id, 0, 0, 0);
        MatchStatus expectedStatus = MatchStatus.InvalidPosition;

        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            placement);

        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void PlacePiece_ValidMove_FlipsPiecesAdvancesTurnAndSynchronizesMatch()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        MatchPlacement placement = new MatchPlacement(match.Id, 0, 2, 3);
        MatchStatus expectedStatus = MatchStatus.Success;
        long expectedVersion = 1L;
        int? expectedTurnSeat = 2;
        int expectedMoveCount = 1;
        int expectedSyncCalls = 1;

        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            placement);
        context.ApplicationService.SynchronizePendingMatches();

        Assert.AreEqual(expectedStatus, result.Status);
        Match updated = TestAssert.IsNotNull(result.Value);
        Assert.AreEqual(expectedVersion, updated.MatchStateVersion);
        Assert.AreEqual(expectedTurnSeat, updated.CurrentTurnSeatNumber);
        Assert.AreEqual(ReversiEngine.Black, updated.Board[2 * 8 + 3]);
        Assert.AreEqual(ReversiEngine.Black, updated.Board[3 * 8 + 3]);
        Assert.AreEqual(expectedMoveCount, updated.Moves.Count);
        Assert.AreEqual(PlayerOrigin, updated.LastMove?.Origin ?? string.Empty);
        Assert.AreEqual(expectedSyncCalls, context.Repository.SynchronizeCalls);
    }

    [TestMethod]
    public void PlacePiece_PersistenceUnavailable_PreservesAndRetriesConfirmedState()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.Repository.SynchronizationAvailable = false;
        MatchStatus expectedStatus = MatchStatus.Success;
        long expectedVersion = 1L;
        int expectedInitialSyncCalls = 0;
        int expectedFirstRetryCalls = 1;
        int expectedSecondRetryCalls = 2;

        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            new MatchPlacement(match.Id, 0, 2, 3));

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.AreEqual(expectedVersion, match.MatchStateVersion);
        Assert.AreEqual(expectedInitialSyncCalls, context.Repository.SynchronizeCalls);

        context.ApplicationService.SynchronizePendingMatches();
        Assert.AreEqual(expectedFirstRetryCalls, context.Repository.SynchronizeCalls);

        context.ApplicationService.SynchronizePendingMatches();
        Assert.AreEqual(expectedFirstRetryCalls, context.Repository.SynchronizeCalls);

        context.Repository.SynchronizationAvailable = true;
        context.Clock.Advance(TimeSpan.FromSeconds(5));
        context.ApplicationService.SynchronizePendingMatches();

        Assert.AreEqual(expectedSecondRetryCalls, context.Repository.SynchronizeCalls);
        Assert.AreEqual(expectedVersion, context.Repository.LastSynchronizedVersion);
    }

    [TestMethod]
    public void PlacePiece_AfterTimeout_ResetsConsecutiveTimeoutCount()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        match.Participants[0].ConsecutiveTimeoutCount = 2;
        MatchPlacement placement = new MatchPlacement(match.Id, 0, 2, 3);
        MatchStatus expectedStatus = MatchStatus.Success;
        int expectedTimeouts = 0;

        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            placement);

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.AreEqual(expectedTimeouts, match.Participants[0].ConsecutiveTimeoutCount);
    }

    [TestMethod]
    public void ForfeitMatch_NonParticipant_ReturnsUnauthorized()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        MatchStatus expectedStatus = MatchStatus.Unauthorized;

        MatchResult<Match> result = context.ApplicationService.ForfeitMatch(
            context.Spectator.SessionToken,
            match.Id);

        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void ForfeitMatch_Participant_ConcludesMatchWithAbandonment()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        MatchStatus expectedStatus = MatchStatus.Success;
        long expectedVersion = 1L;
        string expectedEndReason = "ABANDONMENT";
        int? expectedAbandoningSeat = 1;
        int? expectedWinnerSeat = 2;
        int expectedSyncCalls = 1;

        MatchResult<Match> result = context.ApplicationService.ForfeitMatch(
            context.PlayerOne.SessionToken,
            match.Id);
        context.ApplicationService.SynchronizePendingMatches();

        Assert.AreEqual(expectedStatus, result.Status);
        Match concluded = TestAssert.IsNotNull(result.Value);
        Assert.AreEqual(FinishedState, concluded.State);
        Assert.AreEqual(expectedVersion, concluded.MatchStateVersion);
        Assert.AreEqual(expectedEndReason, concluded.EndReason);
        Assert.AreEqual(expectedAbandoningSeat, concluded.AbandoningSeatNumber);
        Assert.AreEqual(expectedWinnerSeat, concluded.WinnerSeatNumber);
        Assert.IsNull(concluded.CurrentTurnSeatNumber);
        Assert.AreEqual(expectedSyncCalls, context.Repository.SynchronizeCalls);
    }

    [TestMethod]
    public void ForfeitMatch_AlreadyFinished_IsIdempotent()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        _ = context.ApplicationService.ForfeitMatch(context.PlayerOne.SessionToken, match.Id);
        context.ApplicationService.SynchronizePendingMatches();
        MatchStatus expectedStatus = MatchStatus.Success;
        int expectedSyncCalls = 1;

        MatchResult<Match> secondForfeit = context.ApplicationService.ForfeitMatch(
            context.PlayerOne.SessionToken,
            match.Id);
        context.ApplicationService.SynchronizePendingMatches();

        Assert.AreEqual(expectedStatus, secondForfeit.Status);
        Assert.AreEqual(FinishedState, secondForfeit.Value?.State);
        Assert.AreEqual(expectedSyncCalls, context.Repository.SynchronizeCalls);
    }

    [TestMethod]
    public void ProcessTurnTimeouts_TurnNotExpired_DoesNothing()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.Clock.Advance(TimeSpan.FromSeconds(20));
        int expectedCount = 0;

        Match[] updated = context.ApplicationService.ProcessTurnTimeouts();

        Assert.AreEqual(expectedCount, updated.Length);
        Assert.AreEqual(expectedCount, match.Participants[0].ConsecutiveTimeoutCount);
    }

    [TestMethod]
    public void ProcessTurnTimeouts_SingleTimeout_PlaysAutomaticMoveAndIncrementsTimeout()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.Clock.Advance(TimeSpan.FromSeconds(41));
        int expectedCount = 1;
        long expectedVersion = 1L;
        int? expectedTurnSeat = 2;

        Match[] updated = context.ApplicationService.ProcessTurnTimeouts();
        context.ApplicationService.SynchronizePendingMatches();

        Assert.AreEqual(expectedCount, updated.Length);
        Assert.AreEqual(expectedCount, match.Participants[0].ConsecutiveTimeoutCount);
        Assert.AreEqual(expectedVersion, match.MatchStateVersion);
        Assert.AreEqual(expectedTurnSeat, match.CurrentTurnSeatNumber);
        Assert.AreEqual(TimeoutOrigin, match.LastMove?.Origin ?? string.Empty);
        Assert.AreEqual(expectedCount, context.Repository.SynchronizeCalls);
    }

    [TestMethod]
    public void ProcessTurnTimeouts_ThreeConsecutiveTimeouts_ConcludesMatchWithInactivity()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        match.Participants[0].ConsecutiveTimeoutCount = 2;
        context.Clock.Advance(TimeSpan.FromSeconds(41));
        int expectedUpdatedCount = 1;
        int expectedTimeouts = 3;
        string expectedEndReason = "INACTIVITY";
        int? expectedWinnerSeat = 2;
        int? expectedAbandoningSeat = 1;
        int expectedSyncCalls = 1;

        Match[] updated = context.ApplicationService.ProcessTurnTimeouts();
        context.ApplicationService.SynchronizePendingMatches();

        Assert.AreEqual(expectedUpdatedCount, updated.Length);
        Assert.AreEqual(expectedTimeouts, match.Participants[0].ConsecutiveTimeoutCount);
        Assert.AreEqual(FinishedState, match.State);
        Assert.AreEqual(expectedEndReason, match.EndReason);
        Assert.AreEqual(expectedWinnerSeat, match.WinnerSeatNumber);
        Assert.AreEqual(expectedAbandoningSeat, match.AbandoningSeatNumber);
        Assert.IsNull(match.CurrentTurnSeatNumber);
        Assert.AreEqual(expectedSyncCalls, context.Repository.SynchronizeCalls);
    }

    [TestMethod]
    public void ObserveMatch_InProgress_ReturnsActiveMatch()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        MatchStatus expectedStatus = MatchStatus.Success;

        MatchResult<Match> result = context.ApplicationService.ObserveMatch(
            context.Spectator.SessionToken,
            match.Id);

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.AreEqual(match.Id, result.Value?.Id);
    }

    [TestMethod]
    public void ObserveMatch_FinishedMatch_ReturnsSpectatingUnavailable()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        _ = context.ApplicationService.ForfeitMatch(context.PlayerOne.SessionToken, match.Id);
        MatchStatus expectedStatus = MatchStatus.SpectatingUnavailable;

        MatchResult<Match> result = context.ApplicationService.ObserveMatch(
            context.Spectator.SessionToken,
            match.Id);

        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void GetMoveHistory_PlayedMove_ReturnsOrderedMoves()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        _ = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            new MatchPlacement(match.Id, 0, 2, 3));
        MatchStatus expectedStatus = MatchStatus.Success;
        int expectedCount = 1;
        int expectedRow = 2;
        int expectedColumn = 3;

        MatchResult<MatchMove[]> result = context.ApplicationService.GetMoveHistory(
            context.PlayerOne.SessionToken,
            match.Id);

        Assert.AreEqual(expectedStatus, result.Status);
        MatchMove[] moves = TestAssert.IsNotNull(result.Value);
        Assert.AreEqual(expectedCount, moves.Length);
        Assert.AreEqual(expectedCount, moves[0].MoveNumber);
        Assert.AreEqual(expectedRow, moves[0].Row);
        Assert.AreEqual(expectedColumn, moves[0].Column);
    }

    [TestMethod]
    public void PlacePiece_OpponentHasNoLegalMoves_SkipsOpponentTurn()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        int[] board = new int[ReversiEngine.CellCount];
        board[(0 * 8) + 1] = ReversiEngine.White;
        board[(0 * 8) + 2] = ReversiEngine.Black;
        board[(7 * 8) + 0] = ReversiEngine.Black;
        board[(7 * 8) + 1] = ReversiEngine.White;
        match.Board = board;
        MatchStatus expectedStatus = MatchStatus.Success;
        int? expectedTurnSeat = 1;
        int? expectedSkippedSeat = 2;

        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            new MatchPlacement(match.Id, 0, 0, 0));

        Assert.AreEqual(expectedStatus, result.Status);
        Match updated = TestAssert.IsNotNull(result.Value);
        Assert.AreEqual(InProgressState, updated.State);
        Assert.AreEqual(expectedTurnSeat, updated.CurrentTurnSeatNumber);
        Assert.AreEqual(expectedSkippedSeat, updated.SkippedSeatNumber);
    }

    [TestMethod]
    public void PlacePiece_NoPlayerHasLegalMoves_ConcludesMatchNormally()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        int[] board = new int[ReversiEngine.CellCount];
        board[(0 * 8) + 1] = ReversiEngine.White;
        board[(0 * 8) + 2] = ReversiEngine.Black;
        match.Board = board;
        MatchStatus expectedStatus = MatchStatus.Success;
        string expectedEndReason = "NO_LEGAL_MOVES";
        int? expectedWinnerSeat = 1;

        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            new MatchPlacement(match.Id, 0, 0, 0));

        Assert.AreEqual(expectedStatus, result.Status);
        Match concluded = TestAssert.IsNotNull(result.Value);
        Assert.AreEqual(FinishedState, concluded.State);
        Assert.AreEqual(expectedEndReason, concluded.EndReason);
        Assert.AreEqual(expectedWinnerSeat, concluded.WinnerSeatNumber);
        Assert.IsNull(concluded.CurrentTurnSeatNumber);
    }

    [TestMethod]
    public void ProcessTurnTimeouts_DatabaseUnavailable_StillTransitionsExpiredTurns()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.Repository.SynchronizationAvailable = false;
        _ = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            new MatchPlacement(match.Id, 0, 2, 3));
        context.ApplicationService.SynchronizePendingMatches();
        context.Clock.Advance(TimeSpan.FromSeconds(41));
        int expectedUpdatedCount = 1;
        long expectedVersion = 2L;
        int expectedSyncCalls = 1;

        Match[] updated = context.ApplicationService.ProcessTurnTimeouts();

        Assert.AreEqual(expectedUpdatedCount, updated.Length);
        Assert.AreEqual(expectedVersion, updated[0].MatchStateVersion);
        Assert.AreEqual(TimeoutOrigin, updated[0].LastMove?.Origin ?? string.Empty);
        Assert.AreEqual(expectedSyncCalls, context.Repository.SynchronizeCalls);
    }

    [TestMethod]
    public void SynchronizePendingMatches_FinishedMatchPersisted_EvictsFromActiveMatches()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        _ = context.ApplicationService.ForfeitMatch(context.PlayerOne.SessionToken, match.Id);
        context.ApplicationService.SynchronizePendingMatches();
        MatchStatus expectedStatus = MatchStatus.Success;
        int expectedLoadCalls = 1;

        MatchResult<Match> reloadResult = context.ApplicationService.GetMatchState(
            context.PlayerOne.SessionToken,
            match.Id);

        Assert.AreEqual(expectedStatus, reloadResult.Status);
        Assert.AreEqual(FinishedState, reloadResult.Value?.State);
        Assert.AreEqual(expectedLoadCalls, context.Repository.LoadMatchCalls);
    }

    [TestMethod]
    public void GetAuthorizedMatchSnapshot_Participant_ReturnsSnapshot()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        MatchStatus expectedStatus = MatchStatus.Success;

        MatchResult<Match> participantResult =
            context.ApplicationService.GetAuthorizedMatchSnapshot(
                context.PlayerOne.SessionToken,
                match.Id);

        Assert.AreEqual(expectedStatus, participantResult.Status);
        Assert.AreEqual(match.Id, participantResult.Value?.Id);
    }

    [TestMethod]
    public void GetAuthorizedMatchSnapshot_NonParticipant_ReturnsUnauthorized()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        match.State = FinishedState;
        MatchStatus expectedStatus = MatchStatus.Unauthorized;

        MatchResult<Match> spectatorResult =
            context.ApplicationService.GetAuthorizedMatchSnapshot(
                context.Spectator.SessionToken,
                match.Id);

        Assert.AreEqual(expectedStatus, spectatorResult.Status);
    }

    [TestMethod]
    public void SynchronizePendingMatches_PersistenceFailureOnFirstMatch_StopsTickForBackoff()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match firstMatch = context.CreateMatch(40);
        Match secondMatch = context.CreateMatch(40);
        secondMatch.Id = 56L;
        context.Repository.StoreMatch(secondMatch);
        context.ApplicationService.TrackStartedMatch(firstMatch);
        context.ApplicationService.TrackStartedMatch(secondMatch);
        context.Repository.SynchronizationAvailable = false;
        _ = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            new MatchPlacement(firstMatch.Id, 0, 2, 3));
        _ = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            new MatchPlacement(secondMatch.Id, 0, 2, 3));
        int expectedSyncCalls = 1;

        context.ApplicationService.SynchronizePendingMatches();

        Assert.AreEqual(expectedSyncCalls, context.Repository.SynchronizeCalls);
    }
}

internal sealed class GameplayTestContext
{
    internal MutableTestClock Clock { get; }

    internal SessionStore Sessions { get; }

    internal MockGameplayMatchRepository Repository { get; }

    internal AppGameplayMatchService ApplicationService { get; }

    internal AuthenticatedSession PlayerOne { get; }

    internal AuthenticatedSession PlayerTwo { get; }

    internal AuthenticatedSession Spectator { get; }

    internal GameplayTestContext()
    {
        Clock = new MutableTestClock(new DateTime(2026, 11, 11, 20, 0, 0, DateTimeKind.Utc));
        Sessions = new SessionStore(Clock, NullLogger<SessionStore>.Instance);
        Repository = new MockGameplayMatchRepository();
        ApplicationService = new AppGameplayMatchService(Sessions, Repository, Clock);

        PlayerOne = TestSessionHelper.CreateSession(Sessions, 101, "PlayerAlpha");
        PlayerTwo = TestSessionHelper.CreateSession(Sessions, 202, "PlayerBeta");
        Spectator = TestSessionHelper.CreateSession(Sessions, 303, "SpectatorSam");
    }

    internal Match CreateMatch(int turnDurationSeconds)
    {
        Match match = new Match
        {
            Id = 55L,
            Code = "REV555",
            State = "InProgress",
            TurnDurationSeconds = turnDurationSeconds,
            StartedAtUtc = Clock.UtcNow,
            Participants = new[]
            {
                new MatchParticipant
                {
                    AccountId = PlayerOne.AccountId,
                    DisplayName = PlayerOne.Username,
                    SeatNumber = 1
                },
                new MatchParticipant
                {
                    AccountId = PlayerTwo.AccountId,
                    DisplayName = PlayerTwo.Username,
                    SeatNumber = 2
                }
            }
        };

        Repository.StoreMatch(match);
        return match;
    }
}

internal sealed class MutableTestClock : IUtcClock
{
    private DateTime _utcNow;

    internal MutableTestClock(DateTime start)
    {
        _utcNow = start;
    }

    public DateTime UtcNow
    {
        get { return _utcNow; }
    }

    internal void Advance(TimeSpan delta)
    {
        _utcNow = _utcNow.Add(delta);
    }
}

internal sealed class MockGameplayMatchRepository : IGameplayRepository
{
    private readonly Dictionary<long, Match> _matches = new Dictionary<long, Match>();

    internal int LoadMatchCalls { get; private set; }

    internal int SynchronizeCalls { get; private set; }

    internal bool SynchronizationAvailable { get; set; } = true;

    internal long LastSynchronizedVersion { get; private set; }

    internal bool AccessUnavailable { get; set; }

    internal Action? ObservableMatchesLoaded { get; set; }

    internal MatchStatus AccessStatus { get; set; } = MatchStatus.Success;

    internal bool ListUnavailable { get; set; }

    internal void StoreMatch(Match match)
    {
        _matches[match.Id] = match;
    }

    public MatchStatus EvaluateLobbyAccess(long accountId)
    {
        if (AccessUnavailable)
        {
            throw new InvalidOperationException("access storage unavailable");
        }

        return AccessStatus;
    }

    public MatchResult<Match[]> GetObservableMatches(long accountId)
    {
        if (ListUnavailable)
        {
            throw new InvalidOperationException("storage unavailable");
        }

        if (AccessStatus != MatchStatus.Success)
        {
            return new MatchResult<Match[]>(AccessStatus);
        }

        Match[] matches = _matches.Values
            .Where(match => match.State == "InProgress")
            .Select(match => match.CreateSnapshot())
            .ToArray();
        ObservableMatchesLoaded?.Invoke();
        return new MatchResult<Match[]>(matches);
    }

    public MatchResult<Match> LoadMatch(long matchId)
    {
        LoadMatchCalls++;
        if (_matches.TryGetValue(matchId, out Match? match))
        {
            return new MatchResult<Match>(match);
        }

        return new MatchResult<Match>(MatchStatus.MatchNotFound);
    }

    public bool SynchronizeMatch(Match match)
    {
        SynchronizeCalls++;
        LastSynchronizedVersion = match.MatchStateVersion;
        return SynchronizationAvailable;
    }

    public MatchResult<Match[]> LoadMatchHistory(long accountId)
    {
        return new MatchResult<Match[]>(_matches.Values.ToArray());
    }

    public MatchResult<Match> LoadMatchForObservation(long accountId, long matchId)
    {
        return LoadMatch(matchId);
    }
}
