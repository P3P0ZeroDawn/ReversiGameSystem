using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class AbandonMatchServiceTests
{
    internal void TryAbandon_ActiveMatch_RecordsCompleteResult()
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
        TestAssert.AreEqual(AbandonMatchResult.Success, result);
        TestAssert.AreEqual(MatchState.Finished, session.State);
        TestAssert.AreEqual("System", session.WinnerName);
        TestAssert.AreEqual<DiscColor?>(DiscColor.Black, session.AbandonedByColor);
        TestAssert.AreEqual("Pedro", session.AbandonedByName);
        TestAssert.AreEqual<MatchEndReason?>(
            MatchEndReason.Abandonment,
            session.EndReason);
        TestAssert.AreEqual(true, session.FinishedAtUtc.HasValue);
        TestAssert.AreEqual(1, session.Moves.Count);
        TestAssert.AreEqual(true, ReferenceEquals(recordedMove, session.Moves[0]));
        TestAssert.AreEqual(true, ReferenceEquals(boardBeforeAbandonment, session.Board));
    }

    internal void TryAbandon_JoinedPlayer_AwardsCreatorVictory()
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
        TestAssert.AreEqual(AbandonMatchResult.Success, result);
        TestAssert.AreEqual("Ana", session.WinnerName);
        TestAssert.AreEqual("Pedro", session.AbandonedByName);
        TestAssert.AreEqual<DiscColor?>(DiscColor.White, session.AbandonedByColor);
    }

    internal void TryAbandon_FinishedMatch_PreservesExistingResult()
    {
        // Arrange
        MatchSession session = CreateSystemMatch();
        session.Finish(DiscColor.Black, MatchEndReason.BoardCompleted);
        DateTime? originalFinishedAt = session.FinishedAtUtc;
        AbandonMatchService service = new AbandonMatchService();

        // Act
        AbandonMatchResult result = service.TryAbandon(session, DiscColor.Black);

        // Assert
        TestAssert.AreEqual(AbandonMatchResult.MatchAlreadyFinished, result);
        TestAssert.AreEqual("Pedro", session.WinnerName);
        TestAssert.AreEqual<MatchEndReason?>(
            MatchEndReason.BoardCompleted,
            session.EndReason);
        TestAssert.AreEqual(originalFinishedAt, session.FinishedAtUtc);
        TestAssert.AreEqual<DiscColor?>(null, session.AbandonedByColor);
        TestAssert.AreEqual<string?>(null, session.AbandonedByName);
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
