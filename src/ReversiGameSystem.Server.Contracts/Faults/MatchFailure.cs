using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Identifies a recognized lobby, match or match access rejection.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum MatchFailure
{
    /// <summary>
    /// The match identifier is not positive.
    /// </summary>
    [EnumMember]
    InvalidMatchId,

    /// <summary>
    /// The proposed match configuration is absent or contains unsupported options.
    /// </summary>
    [EnumMember]
    InvalidConfiguration,

    /// <summary>
    /// The turn duration is outside the inclusive range from 10 through 120 seconds.
    /// </summary>
    [EnumMember]
    InvalidTurnDuration,

    /// <summary>
    /// The requested match does not exist.
    /// </summary>
    [EnumMember]
    MatchNotFound,

    /// <summary>
    /// The participant attempted to join its own waiting lobby.
    /// </summary>
    [EnumMember]
    CannotJoinOwnMatch,

    /// <summary>
    /// The supplied join code has an invalid format.
    /// </summary>
    [EnumMember]
    InvalidMatchCode,

    /// <summary>
    /// The join code does not identify a lobby that currently accepts joins.
    /// </summary>
    [EnumMember]
    MatchCodeUnavailable,

    /// <summary>
    /// The session lacks the role or access required for the match operation.
    /// </summary>
    [EnumMember]
    Unauthorized,

    /// <summary>
    /// The match is no longer waiting and does not allow the requested lobby action.
    /// </summary>
    [EnumMember]
    MatchNotWaiting,

    /// <summary>
    /// The requesting or affected account is no longer active.
    /// </summary>
    [EnumMember]
    InactiveAccount,

    /// <summary>
    /// The piece placement object is absent.
    /// </summary>
    [EnumMember]
    MissingPlacement,

    /// <summary>
    /// The supplied match state version is negative.
    /// </summary>
    [EnumMember]
    InvalidMatchStateVersion,

    /// <summary>
    /// The supplied match state version differs from the confirmed version.
    /// </summary>
    [EnumMember]
    StaleMatchStateVersion,

    /// <summary>
    /// The match state does not allow the requested action.
    /// </summary>
    [EnumMember]
    MatchNotInProgress,

    /// <summary>
    /// The requesting participant does not hold the turn.
    /// </summary>
    [EnumMember]
    NotPlayersTurn,

    /// <summary>
    /// The cell is outside the board, occupied or provides no legal capture.
    /// </summary>
    [EnumMember]
    InvalidPosition,

    /// <summary>
    /// Observation cannot begin in the requested match state.
    /// </summary>
    [EnumMember]
    SpectatingUnavailable,
}
