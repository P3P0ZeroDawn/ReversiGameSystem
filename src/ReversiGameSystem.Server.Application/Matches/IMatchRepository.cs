using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Matches;

/// <summary>Defines atomic durable match operations and state transitions.</summary>
public interface IMatchRepository
{
    /// <summary>Verifies that an account remains active.</summary>
    MatchRepositoryStatus Authorize(long accountId);

    /// <summary>Creates a waiting match and its creator atomically.</summary>
    MatchRepositoryResult<Match> Create(AuthenticatedSession player, MatchSettings settings, DateTime now);

    /// <summary>Prepares a private waiting match pending invitation delivery.</summary>
    MatchRepositoryResult<MatchInvitationPreparation> PreparePrivateInvitation(
        AuthenticatedSession player,
        long friendAccountId,
        MatchSettings settings,
        DateTime now);

    /// <summary>Marks the private invitation as delivered and joinable.</summary>
    MatchRepositoryResult<Match> MarkInvitationDelivered(long accountId, long matchId, DateTime now);

    /// <summary>Lists public waiting matches available to an account.</summary>
    MatchRepositoryResult<Match[]> Available(long accountId);

    /// <summary>Occupies the second seat and starts the waiting match atomically.</summary>
    MatchRepositoryResult<Match> Join(AuthenticatedSession player, string code, DateTime now);

    /// <summary>Determines the status applicable to a guest join attempt.</summary>
    MatchRepositoryStatus ValidateJoinForGuest(string code);

    /// <summary>Loads a match for one authorized participant.</summary>
    MatchRepositoryResult<Match> Get(long accountId, long matchId);

    /// <summary>Changes the duration of a waiting match owned by the account.</summary>
    MatchRepositoryResult<Match> ChangeTurnDuration(long accountId, long matchId, int seconds);

    /// <summary>Cancels a waiting match owned by the account.</summary>
    MatchRepositoryStatus Cancel(long accountId, long matchId);
}
