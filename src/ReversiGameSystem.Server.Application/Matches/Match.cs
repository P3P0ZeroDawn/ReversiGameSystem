using System;

namespace ReversiGameSystem.Server.Application.Matches;

/// <summary>Accepted human match configuration.</summary>
public sealed class MatchSettings
{
    /// <summary>Gets or sets the accepted duration per turn in seconds.</summary>
    public int TurnDurationSeconds { get; set; } = 40;

    /// <summary>Gets or sets whether completed results are eligible for statistics.</summary>
    public bool CountsTowardStatistics { get; set; }

    /// <summary>Gets or sets the proposed opponent; this increment accepts Human only.</summary>
    public string OpponentKind { get; set; } = "Human";
}

/// <summary>Durable identity of a participant.</summary>
public sealed class MatchParticipant
{
    /// <summary>Gets or sets the participant seat, either one or two.</summary>
    public int SeatNumber { get; set; }

    /// <summary>Gets or sets the stable server-resolved account identifier.</summary>
    public long AccountId { get; set; }

    /// <summary>Gets or sets the public name recorded when joining.</summary>
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>Contains confirmed match data independently of transport contracts.</summary>
public sealed class Match
{
    /// <summary>Gets or sets the stable match identifier.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the join code while the lobby accepts joins.</summary>
    public string? Code { get; set; }

    /// <summary>Gets or sets the confirmed lifecycle state.</summary>
    public string State { get; set; } = "Waiting";

    /// <summary>Gets or sets the accepted duration per turn in seconds.</summary>
    public int TurnDurationSeconds { get; set; }

    /// <summary>Gets or sets whether completed results are eligible for statistics.</summary>
    public bool CountsTowardStatistics { get; set; }

    /// <summary>Gets or sets the durable UTC match start, absent while waiting.</summary>
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>Gets or sets the confirmed participants in seat order.</summary>
    public MatchParticipant[] Participants { get; set; } = Array.Empty<MatchParticipant>();

    /// <summary>Gets or sets whether the match is a private friend invitation.</summary>
    public bool IsPrivate { get; set; }

    /// <summary>Gets a fresh initial board: zero is empty, one black, two white.</summary>
    public int[] InitialBoard
    {
        get
        {
            return CreateInitialBoard();
        }
    }

    /// <summary>Gets the initial confirmed version supported by this increment.</summary>
    public long MatchStateVersion
    {
        get
        {
            return 0;
        }
    }

    /// <summary>Gets the creator seat holding the initial turn, or no active seat.</summary>
    public int? CurrentTurnSeatNumber
    {
        get
        {
            int? seatNumber = null;
            if (State == "InProgress")
            {
                seatNumber = 1;
            }

            return seatNumber;
        }
    }

    /// <summary>Gets the initial turn start persisted with the match start.</summary>
    public DateTime? TurnStartedAtUtc
    {
        get
        {
            return StartedAtUtc;
        }
    }

    private static int[] CreateInitialBoard()
    {
        int[] board = new int[64];
        board[27] = 2;
        board[36] = 2;
        board[28] = 1;
        board[35] = 1;
        return board;
    }
}

/// <summary>Contains one prepared private match and its server-resolved destination.</summary>
public sealed class MatchInvitationPreparation
{
    /// <summary>Creates the private match preparation details.</summary>
    public MatchInvitationPreparation(Match match, string friendEmail)
    {
        Match = match ?? throw new ArgumentNullException(nameof(match));
        FriendEmail = friendEmail ?? throw new ArgumentNullException(nameof(friendEmail));
    }

    /// <summary>Gets the prepared match.</summary>
    public Match Match { get; }

    /// <summary>Gets the destination email address.</summary>
    public string FriendEmail { get; }
}
