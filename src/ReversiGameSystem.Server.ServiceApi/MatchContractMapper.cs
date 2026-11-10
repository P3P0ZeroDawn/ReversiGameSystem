using System;
using System.Linq;
using System.ServiceModel;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Translates internal match domain representations into WCF contract snapshots.</summary>
internal static class MatchContractMapper
{
    /// <summary>Maps internal participant domain entities into contract summaries.</summary>
    internal static MatchParticipantSummary[] Participants(Match match)
    {
        return match.Participants.Select(player => new MatchParticipantSummary
        {
            SeatNumber = player.SeatNumber,
            AccountId = player.AccountId,
            DisplayName = player.DisplayName,
            Kind = ParticipantKind.Registered
        }).ToArray();
    }

    /// <summary>Maps internal match state into a lobby snapshot.</summary>
    internal static LobbySnapshot Lobby(Match match)
    {
        return new LobbySnapshot
        {
            MatchId = match.Id,
            MatchCode = match.Code,
            State = (MatchState)Enum.Parse(typeof(MatchState), match.State),
            Participants = Participants(match),
            Configuration = new MatchConfiguration
            {
                TurnDurationSeconds = match.TurnDurationSeconds,
                OpponentKind = OpponentKind.Human,
                CountsTowardStatistics = match.CountsTowardStatistics
            }
        };
    }

    /// <summary>Creates the initial snapshot for a started match.</summary>
    internal static MatchStateSnapshot InitialState(Match match)
    {
        RequireStarted(match);
        return new MatchStateSnapshot
        {
            MatchId = match.Id,
            MatchStateVersion = match.MatchStateVersion,
            State = MatchState.InProgress,
            Board = match.InitialBoard.Select(cell => (BoardCellState)cell).ToArray(),
            BlackPieceCount = match.InitialBoard.Count(cell => cell == 1),
            WhitePieceCount = match.InitialBoard.Count(cell => cell == 2),
            CurrentTurnSeatNumber = match.CurrentTurnSeatNumber,
            TurnStartedAtUtc = match.TurnStartedAtUtc
        };
    }

    /// <summary>Ensures that the match is in the active InProgress state.</summary>
    internal static void RequireStarted(Match match)
    {
        if (string.Equals(match.State, "Cancelled", StringComparison.Ordinal))
        {
            throw new FaultException<MatchFault>(new MatchFault
            {
                Reason = MatchFailure.MatchNotWaiting
            }, "The match request was rejected.");
        }

        if (!string.Equals(match.State, "InProgress", StringComparison.Ordinal))
        {
            throw new FaultException<MatchFault>(new MatchFault
            {
                Reason = MatchFailure.MatchNotInProgress
            }, "The match request was rejected.");
        }
    }
}
