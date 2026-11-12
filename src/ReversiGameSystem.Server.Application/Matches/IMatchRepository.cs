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
    public MatchResult<Match> Get(long accountId, long matchId);

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

    /// <summary>Persists an authoritative move and advances turn state atomically.</summary>
    /// <param name="matchId">The target positive match identifier.</param>
    /// <param name="move">The placement data to insert.</param>
    /// <param name="consecutiveTimeouts">The consecutive timeout count for the acting seat.</param>
    /// <param name="nextTurnSeatNumber">The seat number holding the next turn.</param>
    /// <param name="turnStartedAtUtc">The UTC timestamp when the new turn begins.</param>
    /// <returns>The operation status.</returns>
    public MatchStatus SaveMove(
        long matchId,
        MatchMove move,
        int consecutiveTimeouts,
        int nextTurnSeatNumber,
        DateTime turnStartedAtUtc);

    /// <summary>Persists an authoritative final move and concludes the match atomically in a single transaction.</summary>
    /// <param name="matchId">The target positive match identifier.</param>
    /// <param name="move">The placement data to insert.</param>
    /// <param name="consecutiveTimeouts">The consecutive timeout count for the acting seat.</param>
    /// <param name="endReason">The recognized conclusion reason code.</param>
    /// <param name="winnerSeatNumber">The winning seat number, or null for a draw.</param>
    /// <param name="blackPieceCount">The confirmed final count of black pieces.</param>
    /// <param name="whitePieceCount">The confirmed final count of white pieces.</param>
    /// <param name="endedAtUtc">The UTC timestamp when the match concluded.</param>
    /// <returns>The operation status.</returns>
    public MatchStatus SaveMoveAndConclude(
        long matchId,
        MatchMove move,
        int consecutiveTimeouts,
        string endReason,
        int? winnerSeatNumber,
        int blackPieceCount,
        int whitePieceCount,
        DateTime endedAtUtc);

    /// <summary>Concludes a match without inserting a move (such as abandonment or inactivity timeouts) atomically.</summary>
    /// <param name="matchId">The target positive match identifier.</param>
    /// <param name="endReason">The recognized conclusion reason code.</param>
    /// <param name="winnerSeatNumber">The winning seat number, or null for a draw.</param>
    /// <param name="abandoningSeatNumber">The participant seat that abandoned, if applicable.</param>
    /// <param name="blackPieceCount">The confirmed final count of black pieces.</param>
    /// <param name="whitePieceCount">The confirmed final count of white pieces.</param>
    /// <param name="endedAtUtc">The UTC timestamp when the match concluded.</param>
    /// <returns>The operation status.</returns>
    public MatchStatus ConcludeMatch(
        long matchId,
        string endReason,
        int? winnerSeatNumber,
        int? abandoningSeatNumber,
        int blackPieceCount,
        int whitePieceCount,
        DateTime endedAtUtc);

    /// <summary>Retrieves historical matches in which the account participated in descending order.</summary>
    /// <param name="accountId">The authenticated account identifier.</param>
    /// <returns>The confirmed historical matches.</returns>
    public MatchResult<Match[]> GetMatchHistory(long accountId);

    /// <summary>Loads active match data for an authorized observer.</summary>
    /// <param name="accountId">The requesting active account identifier.</param>
    /// <param name="matchId">The target positive match identifier.</param>
    /// <returns>The confirmed match data for observation.</returns>
    public MatchResult<Match> GetForObservation(long accountId, long matchId);
}
