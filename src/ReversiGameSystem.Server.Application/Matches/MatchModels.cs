using System;

namespace ReversiGameSystem.Server.Application.Matches;

/// <summary>Identifies expected match failures in services awaiting result migration.</summary>
public enum MatchRejection
{
    /// <summary>The match identifier is not positive.</summary>
    InvalidMatchId,
    /// <summary>The proposed match configuration is unsupported.</summary>
    InvalidConfiguration,
    /// <summary>The proposed turn duration is outside the accepted range.</summary>
    InvalidTurnDuration,
    /// <summary>The requested match does not exist.</summary>
    MatchNotFound,
    /// <summary>The creator attempted to occupy the second seat.</summary>
    CannotJoinOwnMatch,
    /// <summary>The supplied match code has an invalid format.</summary>
    InvalidMatchCode,
    /// <summary>The match code no longer identifies a joinable match.</summary>
    MatchCodeUnavailable,
    /// <summary>The caller does not own or participate in the requested match.</summary>
    Unauthorized,
    /// <summary>The requested operation requires a waiting match.</summary>
    MatchNotWaiting,
    /// <summary>The account is not active.</summary>
    InactiveAccount,
    /// <summary>The requested operation requires an active match.</summary>
    MatchNotInProgress
}

/// <summary>Reports an expected match failure in services awaiting result migration.</summary>
public sealed class MatchRejectedException : Exception
{
    /// <summary>Creates an expected match rejection with its actionable reason.</summary>
    public MatchRejectedException(MatchRejection reason)
        : base("Match request rejected.")
    {
        Reason = reason;
    }

    /// <summary>Gets the actionable expected rejection reason.</summary>
    public MatchRejection Reason { get; }
}
