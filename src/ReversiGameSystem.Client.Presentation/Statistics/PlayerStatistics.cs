using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Statistics;

internal enum CompletedMatchOutcome
{
    Win,
    Loss,
    Draw
}

/// <summary>
/// Counts finished matches for one player.
/// </summary>
internal sealed class PlayerStatistics
{
    private PlayerStatistics(int wins, int losses, int draws)
    {
        Wins = wins;
        Losses = losses;
        Draws = draws;
    }

    internal int Wins { get; }

    internal int Losses { get; }

    internal int Draws { get; }

    internal int MatchesPlayed
    {
        get { return Wins + Losses + Draws; }
    }

    internal double WinRate
    {
        get { return MatchesPlayed == 0 ? 0 : (double)Wins / MatchesPlayed; }
    }

    internal static PlayerStatistics FromCompletedMatches(
        IEnumerable<CompletedMatchOutcome> outcomes)
    {
        int wins = 0;
        int losses = 0;
        int draws = 0;
        foreach (CompletedMatchOutcome outcome in outcomes)
        {
            switch (outcome)
            {
                case CompletedMatchOutcome.Win:
                    wins++;
                    break;
                case CompletedMatchOutcome.Loss:
                    losses++;
                    break;
                case CompletedMatchOutcome.Draw:
                    draws++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outcomes));
            }
        }

        return new PlayerStatistics(wins, losses, draws);
    }
}
