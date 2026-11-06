using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Matches;

/// <summary>Defines atomic durable match operations and state transitions.</summary>
public interface IMatchRepository
{
    /// <summary>Verifies that an account remains active.</summary>
    void Authorize(long accountId);

    /// <summary>Creates a waiting match and its creator atomically.</summary>
    Match Create(AuthenticatedSession player, MatchSettings settings, DateTime now);

    /// <summary>Prepares a private waiting match pending invitation delivery.</summary>
    MatchInvitationPreparation PreparePrivateInvitation(
        AuthenticatedSession player,
        long friendAccountId,
        MatchSettings settings,
        DateTime now);

    /// <summary>Marks the private invitation as delivered and joinable.</summary>
    Match MarkInvitationDelivered(long accountId, long matchId, DateTime now);

    /// <summary>Lists public waiting matches available to an account.</summary>
    Match[] Available(long accountId);

    /// <summary>Occupies the second seat and starts the waiting match atomically.</summary>
    Match Join(AuthenticatedSession player, string code, DateTime now);

    /// <summary>Determines the rejection applicable to a guest join attempt.</summary>
    void ValidateJoinForGuest(string code);

    /// <summary>Loads a match for one authorized participant.</summary>
    Match Get(long accountId, long matchId);

    /// <summary>Changes the duration of a waiting match owned by the account.</summary>
    Match ChangeTurnDuration(long accountId, long matchId, int seconds);

    /// <summary>Cancels a waiting match owned by the account.</summary>
    void Cancel(long accountId, long matchId);
}
