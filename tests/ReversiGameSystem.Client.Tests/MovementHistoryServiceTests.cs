using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class MovementHistoryServiceTests
{
    internal void TryGetMovements_UnorderedRecords_SortsBySequence()
    {
        // Arrange
        DateTime recordedAt = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
        RecordedMove second = new RecordedMove(
            2, "Ana", "C3", recordedAt.AddMinutes(1), 2, MatchMoveOrigin.Timeout);
        RecordedMove first = new RecordedMove(
            1, "Pedro", "D3", recordedAt, 1, MatchMoveOrigin.Manual);
        MatchInformation match = CreateMatch(new[] { second, first });
        MovementHistoryService service = new MovementHistoryService(new[] { match });

        // Act
        MovementHistoryRetrievalResult result = service.TryGetMovements(
            match.Id,
            out IReadOnlyList<RecordedMove> movements);

        // Assert
        TestAssert.AreEqual(MovementHistoryRetrievalResult.Success, result);
        TestAssert.AreEqual(2, movements.Count);
        TestAssert.AreEqual(1, movements[0].Number);
        TestAssert.AreEqual(2, movements[1].Number);
        TestAssert.AreEqual(recordedAt, movements[0].RecordedAtUtc);
        TestAssert.AreEqual(1, movements[0].AffectedPieces);
        TestAssert.AreEqual(MatchMoveOrigin.Manual, movements[0].Origin);
        TestAssert.AreEqual(MatchMoveOrigin.Timeout, movements[1].Origin);
    }

    internal void TryGetMovements_NoRecords_ReturnsEmpty()
    {
        // Arrange
        MatchInformation match = CreateMatch(Array.Empty<RecordedMove>());
        MovementHistoryService service = new MovementHistoryService(new[] { match });

        // Act
        MovementHistoryRetrievalResult result = service.TryGetMovements(
            match.Id,
            out IReadOnlyList<RecordedMove> movements);

        // Assert
        TestAssert.AreEqual(MovementHistoryRetrievalResult.Success, result);
        TestAssert.AreEqual(0, movements.Count);
    }

    internal void TryGetMovements_MissingMatch_ReturnsRetrievalError()
    {
        // Arrange
        MovementHistoryService service = new MovementHistoryService(
            Array.Empty<MatchInformation>());

        // Act
        MovementHistoryRetrievalResult result = service.TryGetMovements(
            "missing",
            out IReadOnlyList<RecordedMove> movements);

        // Assert
        TestAssert.AreEqual(MovementHistoryRetrievalResult.RetrievalError, result);
        TestAssert.AreEqual(0, movements.Count);
    }

    private static MatchInformation CreateMatch(IReadOnlyList<RecordedMove> movements)
    {
        return new MatchInformation(
            "movements",
            "Pedro",
            "Ana",
            40,
            MatchRecordState.Finished,
            "Pedro",
            new[] { ReversiBoard.CreateInitial() },
            movements,
            new DateTime(2026, 9, 18, 13, 0, 0, DateTimeKind.Utc),
            MatchEndReason.BoardCompleted);
    }
}
