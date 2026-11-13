using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Matches;

/// <summary>Defines atomic durable match operations and state transitions.</summary>
public interface IMatchRepository
{
    /// <summary>Checks whether an active account participates or may access a waiting match.</summary>
    public MatchStatus EvaluateLobbyAccess(long accountId);

    /// <summary>Cancels a waiting match owned by the account.</summary>
    public MatchStatus Cancel(long accountId, long matchId);

    /// <summary>Determines the status applicable to a guest join attempt.</summary>
    public MatchStatus ValidateGuestJoin(string code);

    /// <summary>Creates a waiting match and its creator atomically.</summary>
    public MatchResult<Match> Create(AuthenticatedSession player, MatchSettings settings, DateTime now);

    /// <summary>Lists public waiting matches available to an account.</summary>
    public MatchResult<Match[]> Available(long accountId);

    /// <summary>Occupies the second seat and starts the waiting match atomically.</summary>
    public MatchResult<Match> Join(AuthenticatedSession player, string code, DateTime now);

    /// <summary>Loads a match for one authorized participant.</summary>
    public MatchResult<Match> LoadParticipantMatch(long accountId, long matchId);

    /// <summary>Changes the duration of a waiting match owned by the account.</summary>
    public MatchResult<Match> ChangeTurnDuration(long accountId, long matchId, int seconds);

    /// <summary>Prepares a private waiting match pending invitation delivery.</summary>
    public MatchResult<MatchInvitationPreparation> PreparePrivateInvitation(
        AuthenticatedSession player,
        long friendAccountId,
        MatchSettings settings,
        DateTime now);

    /// <summary>Marks the private invitation as delivered and joinable.</summary>
    public MatchResult<Match> MarkInvitationDelivered(long accountId, long matchId, DateTime now);

}
