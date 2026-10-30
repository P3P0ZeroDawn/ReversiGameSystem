using System;
using System.Linq;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Matches;

namespace ReversiGameSystem.Server.ServiceApi;

internal static class MatchContractMapper
{
    internal static MatchParticipantSummary[] Participants(MatchRecord match) =>
        match.Participants.Select(player => new MatchParticipantSummary
        {
            SeatNumber = player.SeatNumber, AccountId = player.AccountId,
            DisplayName = player.DisplayName, Kind = ParticipantKind.Registered
        }).ToArray();

    internal static LobbySnapshot Lobby(MatchRecord match) => new LobbySnapshot
    {
        MatchId = match.Id, MatchCode = match.Code,
        State = (MatchState)Enum.Parse(typeof(MatchState), match.State),
        Participants = Participants(match),
        Configuration = new MatchConfiguration
        {
            TurnDurationSeconds = match.TurnDurationSeconds, OpponentKind = OpponentKind.Human,
            CountsTowardStatistics = match.CountsTowardStatistics
        }
    };

    // Step one exposes only the initial confirmed state; gameplay writes remain unsupported.
    internal static MatchStateSnapshot InitialState(MatchRecord match)
    {
        RequireStarted(match);
        return new MatchStateSnapshot
        {
            MatchId = match.Id, MatchStateVersion = match.MatchStateVersion, State = MatchState.InProgress,
            Board = match.InitialBoard.Select(cell => (BoardCellState)cell).ToArray(),
            BlackPieceCount = match.InitialBoard.Count(cell => cell == 1),
            WhitePieceCount = match.InitialBoard.Count(cell => cell == 2),
            CurrentTurnSeatNumber = match.CurrentTurnSeatNumber, TurnStartedAtUtc = match.TurnStartedAtUtc
        };
    }

    internal static void RequireStarted(MatchRecord match)
    {
        if (match.State == "Cancelled")
        {
            throw new MatchRejectedException(MatchRejection.MatchNotWaiting);
        }
        if (match.State != "InProgress")
        {
            throw new MatchRejectedException(MatchRejection.MatchNotInProgress);
        }
    }
}
