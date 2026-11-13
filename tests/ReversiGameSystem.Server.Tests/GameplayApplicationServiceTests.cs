using System;
using System.Collections.Generic;
using System.Linq;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Domain.Matches;
using ReversiGameSystem.Server.Persistence.Sessions;

namespace ReversiGameSystem.Server.Tests;

/// <summary>
/// Verifies authoritative in-memory match management, placement rules, timeout limits and replay data.
/// </summary>
internal sealed class GameplayApplicationServiceTests
{
    internal void TrackStartedMatch_InitializesBoardAndTurnCorrectly()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);

        // Act
        context.ApplicationService.TrackStartedMatch(match);

        // Assert
        TestAssert.AreEqual(0L, match.MatchStateVersion);
        TestAssert.AreEqual<int?>(1, match.CurrentTurnSeatNumber);
        TestAssert.AreEqual(64, match.Board.Length);
        TestAssert.AreEqual(ReversiEngine.White, match.Board[27]);
        TestAssert.AreEqual(ReversiEngine.Black, match.Board[28]);
        TestAssert.AreEqual(ReversiEngine.Black, match.Board[35]);
        TestAssert.AreEqual(ReversiEngine.White, match.Board[36]);
        TestAssert.AreEqual(context.Clock.UtcNow, match.TurnStartedAtUtc);
    }

    internal void PlacePiece_MissingPlacement_ReturnsMissingPlacement()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();

        // Act
        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            null);

        // Assert
        TestAssert.AreEqual(MatchStatus.MissingPlacement, result.Status);
    }

    internal void PlacePiece_NegativeVersion_ReturnsInvalidMatchStateVersion()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        MatchPlacement placement = new MatchPlacement(10, -1, 2, 3);

        // Act
        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            placement);

        // Assert
        TestAssert.AreEqual(MatchStatus.InvalidMatchStateVersion, result.Status);
    }

    internal void PlacePiece_InvalidSession_ReturnsInvalidSession()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        MatchPlacement placement = new MatchPlacement(10, 0, 2, 3);

        // Act
        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            "non-existent-token",
            placement);

        // Assert
        TestAssert.AreEqual(MatchStatus.InvalidSession, result.Status);
    }

    internal void PlacePiece_NotPlayersTurn_ReturnsNotPlayersTurn()
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
        TestAssert.AreEqual(MatchStatus.NotPlayersTurn, result.Status);
    }

    internal void PlacePiece_StaleVersion_ReturnsStaleMatchStateVersion()
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
        TestAssert.AreEqual(MatchStatus.StaleMatchStateVersion, result.Status);
    }

    internal void PlacePiece_InvalidPosition_ReturnsInvalidPosition()
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
        TestAssert.AreEqual(MatchStatus.InvalidPosition, result.Status);
    }

    internal void PlacePiece_ValidMove_FlipsPiecesAdvancesTurnAndSynchronizesMatch()
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
        _ = context.ApplicationService.ProcessTurnTimeouts();

        // Assert
        TestAssert.AreEqual(MatchStatus.Success, result.Status);
        Match updated = result.Value ?? throw new InvalidOperationException("The result must contain the updated match.");
        TestAssert.AreEqual(1L, updated.MatchStateVersion);
        TestAssert.AreEqual<int?>(2, updated.CurrentTurnSeatNumber);
        TestAssert.AreEqual(ReversiEngine.Black, updated.Board[2 * 8 + 3]);
        TestAssert.AreEqual(ReversiEngine.Black, updated.Board[3 * 8 + 3]);
        TestAssert.AreEqual(1, updated.Moves.Count);
        TestAssert.AreEqual("Player", updated.LastMove?.Origin ?? string.Empty);
        TestAssert.AreEqual(1, context.Repository.SynchronizeCalls);
    }

    internal void PlacePiece_PersistenceUnavailable_PreservesAndRetriesConfirmedState()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.Repository.SynchronizationAvailable = false;

        MatchResult<Match> result = context.ApplicationService.PlacePiece(
            context.PlayerOne.SessionToken,
            new MatchPlacement(match.Id, 0, 2, 3));

        TestAssert.AreEqual(MatchStatus.Success, result.Status);
        TestAssert.AreEqual(1L, match.MatchStateVersion);
        TestAssert.AreEqual(0, context.Repository.SynchronizeCalls);

        _ = context.ApplicationService.ProcessTurnTimeouts();
        TestAssert.AreEqual(1, context.Repository.SynchronizeCalls);

        context.Repository.SynchronizationAvailable = true;
        context.Clock.Advance(TimeSpan.FromSeconds(5));
        Match[] timeoutTransitions = context.ApplicationService.ProcessTurnTimeouts();

        TestAssert.AreEqual(0, timeoutTransitions.Length);
        TestAssert.AreEqual(2, context.Repository.SynchronizeCalls);
        TestAssert.AreEqual(1L, context.Repository.LastSynchronizedVersion);
    }

    internal void PlacePiece_ResetsConsecutiveTimeoutCount()
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
        TestAssert.AreEqual(MatchStatus.Success, result.Status);
        TestAssert.AreEqual(0, match.Participants[0].ConsecutiveTimeoutCount);
    }

    internal void ForfeitMatch_NonParticipant_ReturnsUnauthorized()
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
        TestAssert.AreEqual(MatchStatus.Unauthorized, result.Status);
    }

    internal void ForfeitMatch_Participant_ConcludesMatchWithAbandonment()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);

        // Act
        MatchResult<Match> result = context.ApplicationService.ForfeitMatch(
            context.PlayerOne.SessionToken,
            match.Id);
        _ = context.ApplicationService.ProcessTurnTimeouts();

        // Assert
        TestAssert.AreEqual(MatchStatus.Success, result.Status);
        Match concluded = result.Value ?? throw new InvalidOperationException("Value expected.");
        TestAssert.AreEqual("Finished", concluded.State);
        TestAssert.AreEqual("ABANDONMENT", concluded.EndReason);
        TestAssert.AreEqual<int?>(1, concluded.AbandoningSeatNumber);
        TestAssert.AreEqual<int?>(2, concluded.WinnerSeatNumber);
        TestAssert.AreEqual<int?>(null, concluded.CurrentTurnSeatNumber);
        TestAssert.AreEqual(1, context.Repository.SynchronizeCalls);
    }

    internal void ForfeitMatch_AlreadyFinished_IsIdempotent()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        _ = context.ApplicationService.ForfeitMatch(context.PlayerOne.SessionToken, match.Id);

        // Act
        MatchResult<Match> secondForfeit = context.ApplicationService.ForfeitMatch(
            context.PlayerOne.SessionToken,
            match.Id);
        _ = context.ApplicationService.ProcessTurnTimeouts();

        // Assert
        TestAssert.AreEqual(MatchStatus.Success, secondForfeit.Status);
        TestAssert.AreEqual("Finished", secondForfeit.Value?.State);
        TestAssert.AreEqual(1, context.Repository.SynchronizeCalls);
    }

    internal void ProcessTurnTimeouts_TurnNotExpired_DoesNothing()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.Clock.Advance(TimeSpan.FromSeconds(20));

        // Act
        Match[] updated = context.ApplicationService.ProcessTurnTimeouts();

        // Assert
        TestAssert.AreEqual(0, updated.Length);
        TestAssert.AreEqual(0, match.Participants[0].ConsecutiveTimeoutCount);
    }

    internal void ProcessTurnTimeouts_SingleTimeout_PlaysAutomaticMoveAndIncrementsTimeout()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.Clock.Advance(TimeSpan.FromSeconds(41));

        // Act
        Match[] updated = context.ApplicationService.ProcessTurnTimeouts();
        _ = context.ApplicationService.ProcessTurnTimeouts();

        // Assert
        TestAssert.AreEqual(1, updated.Length);
        TestAssert.AreEqual(1, match.Participants[0].ConsecutiveTimeoutCount);
        TestAssert.AreEqual(1L, match.MatchStateVersion);
        TestAssert.AreEqual<int?>(2, match.CurrentTurnSeatNumber);
        TestAssert.AreEqual("Timeout", match.LastMove?.Origin ?? string.Empty);
        TestAssert.AreEqual(1, context.Repository.SynchronizeCalls);
    }

    internal void ProcessTurnTimeouts_ThreeConsecutiveTimeouts_ConcludesMatchWithInactivity()
    {
        // Arrange
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        match.Participants[0].ConsecutiveTimeoutCount = 2;
        context.Clock.Advance(TimeSpan.FromSeconds(41));

        // Act
        Match[] updated = context.ApplicationService.ProcessTurnTimeouts();
        _ = context.ApplicationService.ProcessTurnTimeouts();

        // Assert
        TestAssert.AreEqual(1, updated.Length);
        TestAssert.AreEqual(3, match.Participants[0].ConsecutiveTimeoutCount);
        TestAssert.AreEqual("Finished", match.State);
        TestAssert.AreEqual("INACTIVITY", match.EndReason);
        TestAssert.AreEqual<int?>(2, match.WinnerSeatNumber);
        TestAssert.AreEqual<int?>(1, match.AbandoningSeatNumber);
        TestAssert.AreEqual<int?>(null, match.CurrentTurnSeatNumber);
        TestAssert.AreEqual(1, context.Repository.SynchronizeCalls);
    }

    internal void ObserveMatch_InProgress_ReturnsActiveMatch()
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
        TestAssert.AreEqual(MatchStatus.Success, result.Status);
        TestAssert.AreEqual(match.Id, result.Value?.Id);
    }

    internal void ObserveMatch_FinishedMatch_ReturnsSpectatingUnavailable()
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
        TestAssert.AreEqual(MatchStatus.SpectatingUnavailable, result.Status);
    }

    internal void GetMoveHistory_ReturnsOrderedMoves()
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
        TestAssert.AreEqual(MatchStatus.Success, result.Status);
        MatchMove[] moves = result.Value ?? throw new InvalidOperationException("Moves expected.");
        TestAssert.AreEqual(1, moves.Length);
        TestAssert.AreEqual(1, moves[0].MoveNumber);
        TestAssert.AreEqual(2, moves[0].Row);
        TestAssert.AreEqual(3, moves[0].Column);
    }
}

internal sealed class GameplayTestContext
{
    internal MutableTestClock Clock { get; }
    internal ISessionRepository Sessions { get; }
    internal MockGameplayMatchRepository Repository { get; }
    internal GameplayMatchApplicationService ApplicationService { get; }
    internal AuthenticatedSession PlayerOne { get; }
    internal AuthenticatedSession PlayerTwo { get; }
    internal AuthenticatedSession Spectator { get; }

    internal GameplayTestContext()
    {
        Clock = new MutableTestClock(new DateTime(2026, 11, 11, 20, 0, 0, DateTimeKind.Utc));
        SessionRepository sessionRepo = new SessionRepository(Clock);
        Sessions = sessionRepo;
        Repository = new MockGameplayMatchRepository();
        ApplicationService = new GameplayMatchApplicationService(Sessions, Repository, Clock);

        PlayerOne = sessionRepo.Create(new LoginAccount(101, "PlayerAlpha", "p1@test.invalid", "hash", "ACTIVE", "en-US"));
        PlayerTwo = sessionRepo.Create(new LoginAccount(202, "PlayerBeta", "p2@test.invalid", "hash", "ACTIVE", "en-US"));
        Spectator = sessionRepo.Create(new LoginAccount(303, "SpectatorSam", "spec@test.invalid", "hash", "ACTIVE", "en-US"));
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

    internal int SynchronizeCalls { get; private set; }
    internal bool SynchronizationAvailable { get; set; } = true;
    internal long LastSynchronizedVersion { get; private set; }

    internal void StoreMatch(Match match)
    {
        _matches[match.Id] = match;
    }

    public MatchResult<Match> LoadMatch(long matchId)
    {
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
