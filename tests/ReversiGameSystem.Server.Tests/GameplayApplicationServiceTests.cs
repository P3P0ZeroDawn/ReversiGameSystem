using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Domain.Matches;

namespace ReversiGameSystem.Server.Tests;

/// <summary>
/// Verifies authoritative in-memory match management, placement rules, timeout limits and replay data.
/// </summary>
[TestClass]
public sealed class GameplayApplicationServiceTests
{
    [TestMethod]
    public void TrackStartedMatch_InitializesBoardAndTurnCorrectly()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);

        // Act
        context.ApplicationService.TrackStartedMatch(match);

        // Assert
        Assert.AreEqual(0L, match.MatchStateVersion);
        Assert.AreEqual<int?>(1, match.CurrentTurnSeatNumber);
        Assert.AreEqual(64, match.Board.Length);
        Assert.AreEqual(ReversiEngine.White, match.Board[27]);
        Assert.AreEqual(ReversiEngine.Black, match.Board[28]);
        Assert.AreEqual(ReversiEngine.Black, match.Board[35]);
        Assert.AreEqual(ReversiEngine.White, match.Board[36]);
        Assert.AreEqual(context.Clock.UtcNow, match.TurnStartedAtUtc);
    }

    [TestMethod]
    public void PlacePiece_MissingPlacement_ReturnsMissingPlacement()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();

        // Act
        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            null);

        // Assert
        Assert.AreEqual(MatchStatus.MissingPlacement, result.Status);
    }

    [TestMethod]
    public void PlacePiece_NegativeVersion_ReturnsInvalidMatchStateVersion()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        MatchPlacement placement = new MatchPlacement(10, -1, 2, 3);

        // Act
        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            placement);

        // Assert
        Assert.AreEqual(MatchStatus.InvalidMatchStateVersion, result.Status);
    }

    [TestMethod]
    public void PlacePiece_InvalidSession_ReturnsInvalidSession()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        MatchPlacement placement = new MatchPlacement(10, 0, 2, 3);

        // Act
        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            "non-existent-token",
            placement);

        // Assert
        Assert.AreEqual(MatchStatus.InvalidSession, result.Status);
    }

    [TestMethod]
    public void PlacePiece_NotPlayersTurn_ReturnsNotPlayersTurn()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        MatchPlacement placement = new MatchPlacement(match.Id, 0, 2, 4);

        // Act
        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerTwo.SessionToken,
            placement);

        // Assert
        Assert.AreEqual(MatchStatus.NotPlayersTurn, result.Status);
    }

    [TestMethod]
    public void PlacePiece_StaleVersion_ReturnsStaleMatchStateVersion()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        MatchPlacement placement = new MatchPlacement(match.Id, 1, 2, 3);

        // Act
        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            placement);

        // Assert
        Assert.AreEqual(MatchStatus.StaleMatchStateVersion, result.Status);
    }

    [TestMethod]
    public void PlacePiece_InvalidPosition_ReturnsInvalidPosition()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        MatchPlacement placement = new MatchPlacement(match.Id, 0, 0, 0);

        // Act
        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            placement);

        // Assert
        Assert.AreEqual(MatchStatus.InvalidPosition, result.Status);
    }

    [TestMethod]
    public void PlacePiece_ValidMove_FlipsPiecesAdvancesTurnAndSynchronizesMatch()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        MatchPlacement placement = new MatchPlacement(match.Id, 0, 2, 3);

        // Act
        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            placement);
        context.ApplicationService.SynchronizePendingMatches();

        // Assert
        Assert.AreEqual(MatchStatus.Success, result.Status);
        Match updated = result.Value
            ?? throw new InvalidOperationException("The result must contain the updated match.");
        Assert.AreEqual(1L, updated.MatchStateVersion);
        Assert.AreEqual<int?>(2, updated.CurrentTurnSeatNumber);
        Assert.AreEqual(ReversiEngine.Black, updated.Board[2 * 8 + 3]);
        Assert.AreEqual(ReversiEngine.Black, updated.Board[3 * 8 + 3]);
        Assert.AreEqual(1, updated.Moves.Count);
        Assert.AreEqual("Player", updated.LastMove?.Origin ?? string.Empty);
        Assert.AreEqual(1, context.Repository.SynchronizeCalls);
    }

    [TestMethod]
    public void PlacePiece_PersistenceUnavailable_PreservesAndRetriesConfirmedState()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.Repository.SynchronizationAvailable = false;

        // Act
        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            new MatchPlacement(match.Id, 0, 2, 3));

        // Assert
        Assert.AreEqual(MatchStatus.Success, result.Status);
        Assert.AreEqual(1L, match.MatchStateVersion);
        Assert.AreEqual(0, context.Repository.SynchronizeCalls);

        context.ApplicationService.SynchronizePendingMatches();
        Assert.AreEqual(1, context.Repository.SynchronizeCalls);

        context.ApplicationService.SynchronizePendingMatches();
        Assert.AreEqual(1, context.Repository.SynchronizeCalls);

        context.Repository.SynchronizationAvailable = true;
        context.Clock.Advance(TimeSpan.FromSeconds(5));
        context.ApplicationService.SynchronizePendingMatches();

        Assert.AreEqual(2, context.Repository.SynchronizeCalls);
        Assert.AreEqual(1L, context.Repository.LastSynchronizedVersion);
    }

    [TestMethod]
    public void PlacePiece_ResetsConsecutiveTimeoutCount()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        match.Participants[0].ConsecutiveTimeoutCount = 2;
        MatchPlacement placement = new MatchPlacement(match.Id, 0, 2, 3);

        // Act
        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            placement);

        // Assert
        Assert.AreEqual(MatchStatus.Success, result.Status);
        Assert.AreEqual(0, match.Participants[0].ConsecutiveTimeoutCount);
    }

    [TestMethod]
    public void ForfeitMatch_NonParticipant_ReturnsUnauthorized()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);

        // Act
        MatchResult<Match> result = context.ApplicationService.ForfeitMatch(
            context.Spectator.SessionToken,
            match.Id);

        // Assert
        Assert.AreEqual(MatchStatus.Unauthorized, result.Status);
    }

    [TestMethod]
    public void ForfeitMatch_Participant_ConcludesMatchWithAbandonment()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);

        // Act
        MatchResult<Match> result = context.ApplicationService.ForfeitMatch(
            context.PlayerOne.SessionToken,
            match.Id);
        context.ApplicationService.SynchronizePendingMatches();

        // Assert
        Assert.AreEqual(MatchStatus.Success, result.Status);
        Match concluded = result.Value ?? throw new InvalidOperationException("Value expected.");
        Assert.AreEqual("Finished", concluded.State);
        Assert.AreEqual(1L, concluded.MatchStateVersion);
        Assert.AreEqual("ABANDONMENT", concluded.EndReason);
        Assert.AreEqual<int?>(1, concluded.AbandoningSeatNumber);
        Assert.AreEqual<int?>(2, concluded.WinnerSeatNumber);
        Assert.AreEqual<int?>(null, concluded.CurrentTurnSeatNumber);
        Assert.AreEqual(1, context.Repository.SynchronizeCalls);
    }

    [TestMethod]
    public void ForfeitMatch_AlreadyFinished_IsIdempotent()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        _ = context.ApplicationService.ForfeitMatch(context.PlayerOne.SessionToken, match.Id);
        context.ApplicationService.SynchronizePendingMatches();

        // Act
        MatchResult<Match> secondForfeit = context.ApplicationService.ForfeitMatch(
            context.PlayerOne.SessionToken,
            match.Id);
        context.ApplicationService.SynchronizePendingMatches();

        // Assert
        Assert.AreEqual(MatchStatus.Success, secondForfeit.Status);
        Assert.AreEqual("Finished", secondForfeit.Value?.State);
        Assert.AreEqual(1, context.Repository.SynchronizeCalls);
    }

    [TestMethod]
    public void ProcessTurnTimeouts_TurnNotExpired_DoesNothing()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.Clock.Advance(TimeSpan.FromSeconds(20));

        // Act
        Match[] updated = context.ApplicationService.ProcessTurnTimeouts();

        // Assert
        Assert.AreEqual(0, updated.Length);
        Assert.AreEqual(0, match.Participants[0].ConsecutiveTimeoutCount);
    }

    [TestMethod]
    public void ProcessTurnTimeouts_SingleTimeout_PlaysAutomaticMoveAndIncrementsTimeout()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.Clock.Advance(TimeSpan.FromSeconds(41));

        // Act
        Match[] updated = context.ApplicationService.ProcessTurnTimeouts();
        context.ApplicationService.SynchronizePendingMatches();

        // Assert
        Assert.AreEqual(1, updated.Length);
        Assert.AreEqual(1, match.Participants[0].ConsecutiveTimeoutCount);
        Assert.AreEqual(1L, match.MatchStateVersion);
        Assert.AreEqual<int?>(2, match.CurrentTurnSeatNumber);
        Assert.AreEqual("Timeout", match.LastMove?.Origin ?? string.Empty);
        Assert.AreEqual(1, context.Repository.SynchronizeCalls);
    }

    [TestMethod]
    public void ProcessTurnTimeouts_ThreeConsecutiveTimeouts_ConcludesMatchWithInactivity()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        match.Participants[0].ConsecutiveTimeoutCount = 2;
        context.Clock.Advance(TimeSpan.FromSeconds(41));

        // Act
        Match[] updated = context.ApplicationService.ProcessTurnTimeouts();
        context.ApplicationService.SynchronizePendingMatches();

        // Assert
        Assert.AreEqual(1, updated.Length);
        Assert.AreEqual(3, match.Participants[0].ConsecutiveTimeoutCount);
        Assert.AreEqual("Finished", match.State);
        Assert.AreEqual("INACTIVITY", match.EndReason);
        Assert.AreEqual<int?>(2, match.WinnerSeatNumber);
        Assert.AreEqual<int?>(1, match.AbandoningSeatNumber);
        Assert.AreEqual<int?>(null, match.CurrentTurnSeatNumber);
        Assert.AreEqual(1, context.Repository.SynchronizeCalls);
    }

    [TestMethod]
    public void ObserveMatch_InProgress_ReturnsActiveMatch()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);

        // Act
        MatchResult<Match> result = context.ApplicationService.ObserveMatch(
            context.Spectator.SessionToken,
            match.Id);

        // Assert
        Assert.AreEqual(MatchStatus.Success, result.Status);
        Assert.AreEqual(match.Id, result.Value?.Id);
    }

    [TestMethod]
    public void ObserveMatch_FinishedMatch_ReturnsSpectatingUnavailable()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        _ = context.ApplicationService.ForfeitMatch(context.PlayerOne.SessionToken, match.Id);

        // Act
        MatchResult<Match> result = context.ApplicationService.ObserveMatch(
            context.Spectator.SessionToken,
            match.Id);

        // Assert
        Assert.AreEqual(MatchStatus.SpectatingUnavailable, result.Status);
    }

    [TestMethod]
    public void GetMoveHistory_ReturnsOrderedMoves()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        _ = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            new MatchPlacement(match.Id, 0, 2, 3));

        // Act
        MatchResult<MatchMove[]> result = context.ApplicationService.GetMoveHistory(
            context.PlayerOne.SessionToken,
            match.Id);

        // Assert
        Assert.AreEqual(MatchStatus.Success, result.Status);
        MatchMove[] moves = result.Value ?? throw new InvalidOperationException("Moves expected.");
        Assert.AreEqual(1, moves.Length);
        Assert.AreEqual(1, moves[0].MoveNumber);
        Assert.AreEqual(2, moves[0].Row);
        Assert.AreEqual(3, moves[0].Column);
    }

    [TestMethod]
    public void PlacePiece_OpponentHasNoLegalMoves_SkipsOpponentTurn()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        int[] board = new int[ReversiEngine.CellCount];
        board[(0 * 8) + 1] = ReversiEngine.White;
        board[(0 * 8) + 2] = ReversiEngine.Black;
        board[(7 * 8) + 0] = ReversiEngine.Black;
        board[(7 * 8) + 1] = ReversiEngine.White;
        match.Board = board;

        // Act
        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            new MatchPlacement(match.Id, 0, 0, 0));

        // Assert
        Assert.AreEqual(MatchStatus.Success, result.Status);
        Match updated = result.Value ?? throw new InvalidOperationException("Value expected.");
        Assert.AreEqual("InProgress", updated.State);
        Assert.AreEqual<int?>(1, updated.CurrentTurnSeatNumber);
        Assert.AreEqual<int?>(2, updated.SkippedSeatNumber);
    }

    [TestMethod]
    public void PlacePiece_NoPlayerHasLegalMoves_ConcludesMatchNormally()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        int[] board = new int[ReversiEngine.CellCount];
        board[(0 * 8) + 1] = ReversiEngine.White;
        board[(0 * 8) + 2] = ReversiEngine.Black;
        match.Board = board;

        // Act
        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            new MatchPlacement(match.Id, 0, 0, 0));

        // Assert
        Assert.AreEqual(MatchStatus.Success, result.Status);
        Match concluded = result.Value ?? throw new InvalidOperationException("Value expected.");
        Assert.AreEqual("Finished", concluded.State);
        Assert.AreEqual("NORMAL", concluded.EndReason);
        Assert.AreEqual<int?>(1, concluded.WinnerSeatNumber);
        Assert.AreEqual<int?>(null, concluded.CurrentTurnSeatNumber);
    }

    [TestMethod]
    public void ProcessTurnTimeouts_DatabaseUnavailable_StillTransitionsExpiredTurns()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.Repository.SynchronizationAvailable = false;
        _ = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            new MatchPlacement(match.Id, 0, 2, 3));
        context.ApplicationService.SynchronizePendingMatches();
        context.Clock.Advance(TimeSpan.FromSeconds(41));

        // Act
        Match[] updated = context.ApplicationService.ProcessTurnTimeouts();

        // Assert
        Assert.AreEqual(1, updated.Length);
        Assert.AreEqual(2L, updated[0].MatchStateVersion);
        Assert.AreEqual("Timeout", updated[0].LastMove?.Origin ?? string.Empty);
        Assert.AreEqual(1, context.Repository.SynchronizeCalls);
    }

    [TestMethod]
    public void SynchronizePendingMatches_FinishedMatchPersisted_EvictsFromActiveMatches()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        _ = context.ApplicationService.ForfeitMatch(context.PlayerOne.SessionToken, match.Id);
        context.ApplicationService.SynchronizePendingMatches();

        // Act
        MatchResult<Match> reloadResult = context.ApplicationService.GetMatchState(
            context.PlayerOne.SessionToken,
            match.Id);

        // Assert
        Assert.AreEqual(MatchStatus.Success, reloadResult.Status);
        Assert.AreEqual("Finished", reloadResult.Value?.State);
        Assert.AreEqual(1, context.Repository.LoadMatchCalls);
    }

    [TestMethod]
    public void GetAuthorizedMatchSnapshot_Participant_ReturnsSnapshot()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);

        // Act
        MatchResult<Match> participantResult =
            context.ApplicationService.GetAuthorizedMatchSnapshot(
                context.PlayerOne.SessionToken,
                match.Id);

        // Assert
        Assert.AreEqual(MatchStatus.Success, participantResult.Status);
        Assert.AreEqual(match.Id, participantResult.Value?.Id);
    }

    [TestMethod]
    public void GetAuthorizedMatchSnapshot_NonParticipant_ReturnsUnauthorized()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        match.State = "Finished";

        // Act
        MatchResult<Match> spectatorResult =
            context.ApplicationService.GetAuthorizedMatchSnapshot(
                context.Spectator.SessionToken,
                match.Id);

        // Assert
        Assert.AreEqual(MatchStatus.Unauthorized, spectatorResult.Status);
    }

    [TestMethod]
    public void SynchronizePendingMatches_PersistenceFailureOnFirstMatch_StopsTickForBackoff()
    {
        // Arrange
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

        // Act
        context.ApplicationService.SynchronizePendingMatches();

        // Assert
        Assert.AreEqual(1, context.Repository.SynchronizeCalls);
    }
}

internal sealed class GameplayTestContext
{
    internal MutableTestClock Clock { get; }
    internal SessionStore Sessions { get; }
    internal MockGameplayMatchRepository Repository { get; }
    internal GameplayMatchApplicationService ApplicationService { get; }
    internal AuthenticatedSession PlayerOne { get; }
    internal AuthenticatedSession PlayerTwo { get; }
    internal AuthenticatedSession Spectator { get; }

    internal GameplayTestContext()
    {
        Clock = new MutableTestClock(new DateTime(2026, 11, 11, 20, 0, 0, DateTimeKind.Utc));
        Sessions = new SessionStore(Clock);
        Repository = new MockGameplayMatchRepository();
        ApplicationService = new GameplayMatchApplicationService(Sessions, Repository, Clock);

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
                new MatchParticipant { AccountId = PlayerOne.AccountId, DisplayName = PlayerOne.Username, SeatNumber = 1 },
                new MatchParticipant { AccountId = PlayerTwo.AccountId, DisplayName = PlayerTwo.Username, SeatNumber = 2 }
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
        get
        {
            return _utcNow;
        }
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

    internal bool AccessUnavailable { get; set; }
    internal Action? ObservableMatchesLoaded { get; set; }
    internal MatchStatus AccessStatus { get; set; } = MatchStatus.Success;

    internal bool ListUnavailable { get; set; }

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
