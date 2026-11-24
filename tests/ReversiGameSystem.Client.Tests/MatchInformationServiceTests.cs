using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class MatchInformationServiceTests
{
    [TestMethod]
    public void TryGetHistory_MixedRecords_FiltersAndSorts()
    {
        // Arrange
        DateTime referenceTime = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
        MatchInformation[] records =
        {
            CreateRecord("older", "Pedro", "Ana", referenceTime.AddDays(-2)),
            CreateRecord("unrelated", "Luis", "Luna", referenceTime),
            CreateRecord("newer", "César", "Pedro", referenceTime.AddHours(-1))
        };
        MatchInformationService service = new MatchInformationService("Pedro", records);

        // Act
        MatchInformationRetrievalResult result = service.TryGetHistory(
            out IReadOnlyList<MatchInformation> history);

        // Assert
        Assert.AreEqual(MatchInformationRetrievalResult.Success, result);
        Assert.AreEqual(2, history.Count);
        Assert.AreEqual("newer", history[0].Id);
        Assert.AreEqual("older", history[1].Id);
    }

    [TestMethod]
    public void TryGetHistory_NoPlayerMatches_ReturnsEmpty()
    {
        // Arrange
        MatchInformation[] records =
        {
            CreateRecord("unrelated", "Ana", "Luis", DateTime.UtcNow)
        };
        MatchInformationService service = new MatchInformationService("Pedro", records);

        // Act
        MatchInformationRetrievalResult result = service.TryGetHistory(
            out IReadOnlyList<MatchInformation> history);

        // Assert
        Assert.AreEqual(MatchInformationRetrievalResult.Success, result);
        Assert.AreEqual(0, history.Count);
    }

    [TestMethod]
    public void TryGetHistory_Record_PreservesSummaryFields()
    {
        // Arrange
        DateTime recordedAt = new DateTime(2026, 9, 17, 20, 30, 0, DateTimeKind.Utc);
        MatchInformation record = CreateRecord(
            "summary",
            "Pedro",
            "Ana",
            recordedAt,
            MatchEndReason.Abandonment);
        MatchInformationService service = new MatchInformationService(
            "Pedro",
            new[] { record });

        // Act
        service.TryGetHistory(out IReadOnlyList<MatchInformation> history);

        // Assert
        Assert.AreEqual(recordedAt, history[0].RecordedAtUtc);
        Assert.AreEqual<MatchEndReason?>(
            MatchEndReason.Abandonment,
            history[0].EndReason);
        Assert.AreEqual(4, history[0].LatestBoard.Count(DiscColor.Black));
        Assert.AreEqual(1, history[0].LatestBoard.Count(DiscColor.White));
    }

    private static MatchInformation CreateRecord(
        string id,
        string blackPlayer,
        string whitePlayer,
        DateTime recordedAt,
        MatchEndReason endReason = MatchEndReason.BoardCompleted)
    {
        ReversiBoard initialBoard = ReversiBoard.CreateInitial();
        initialBoard.TryApplyMove(
            DiscColor.Black,
            new BoardPosition(2, 3),
            out ReversiBoard? finalBoard);
        return new MatchInformation(
            id,
            blackPlayer,
            whitePlayer,
            40,
            MatchRecordState.Finished,
            blackPlayer,
            new[] { initialBoard, finalBoard! },
            new[]
            {
                new RecordedMove(
                    1,
                    blackPlayer,
                    "D3",
                    recordedAt.AddMinutes(-1),
                    1,
                    MatchMoveOrigin.Manual)
            },
            recordedAt,
            endReason);
    }
}
