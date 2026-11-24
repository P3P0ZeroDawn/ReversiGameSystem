using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class AbandonMatchServiceTests
{
    [TestMethod]
    public void TryAbandon_ActiveMatch_RecordsCompleteResult()
    {
        // Arrange
        MatchSession session = CreateSystemMatch();
        GameMoveService moveService = new GameMoveService(new Random(1));
        moveService.TryMakeMove(
            session,
            DiscColor.Black,
            new BoardPosition(2, 3),
            out _,
            out _);
        ReversiBoard boardBeforeAbandonment = session.Board;
        MatchMove recordedMove = session.Moves[0];
        AbandonMatchService service = new AbandonMatchService();

        // Act
        AbandonMatchResult result = service.TryAbandon(session, DiscColor.Black);

        // Assert
        Assert.AreEqual(AbandonMatchResult.Success, result);
        Assert.AreEqual(MatchState.Finished, session.State);
        Assert.AreEqual("System", session.WinnerName);
        Assert.AreEqual<DiscColor?>(DiscColor.Black, session.AbandonedByColor);
        Assert.AreEqual("Pedro", session.AbandonedByName);
        Assert.AreEqual<MatchEndReason?>(
            MatchEndReason.Abandonment,
            session.EndReason);
        Assert.AreEqual(true, session.FinishedAtUtc.HasValue);
        Assert.AreEqual(1, session.Moves.Count);
        Assert.AreEqual(true, ReferenceEquals(recordedMove, session.Moves[0]));
        Assert.AreEqual(true, ReferenceEquals(boardBeforeAbandonment, session.Board));
    }

    [TestMethod]
    public void TryAbandon_JoinedPlayer_AwardsCreatorVictory()
    {
        // Arrange
        MatchSession session = new MatchSession(
            "JOIN17",
            40,
            "Ana",
            null,
            DiscColor.White);
        session.JoinAsSecondPlayer("Pedro");
        AbandonMatchService service = new AbandonMatchService();

        // Act
        AbandonMatchResult result = service.TryAbandon(session, DiscColor.White);

        // Assert
        Assert.AreEqual(AbandonMatchResult.Success, result);
        Assert.AreEqual("Ana", session.WinnerName);
        Assert.AreEqual("Pedro", session.AbandonedByName);
        Assert.AreEqual<DiscColor?>(DiscColor.White, session.AbandonedByColor);
    }

    [TestMethod]
    public void TryAbandon_FinishedMatch_PreservesExistingResult()
    {
        // Arrange
        MatchSession session = CreateSystemMatch();
        session.Finish(DiscColor.Black, MatchEndReason.BoardCompleted);
        DateTime? originalFinishedAt = session.FinishedAtUtc;
        AbandonMatchService service = new AbandonMatchService();

        // Act
        AbandonMatchResult result = service.TryAbandon(session, DiscColor.Black);

        // Assert
        Assert.AreEqual(AbandonMatchResult.MatchAlreadyFinished, result);
        Assert.AreEqual("Pedro", session.WinnerName);
        Assert.AreEqual<MatchEndReason?>(
            MatchEndReason.BoardCompleted,
            session.EndReason);
        Assert.AreEqual(originalFinishedAt, session.FinishedAtUtc);
        Assert.AreEqual<DiscColor?>(null, session.AbandonedByColor);
        Assert.AreEqual<string?>(null, session.AbandonedByName);
    }

    private static MatchSession CreateSystemMatch()
    {
        MatchSession session = new MatchSession(
            "QUIT17",
            40,
            "Pedro",
            null);
        session.StartAgainstSystem("System");
        return session;
    }
}
