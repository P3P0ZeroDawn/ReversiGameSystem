using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Identifies the lifecycle state of a lobby or match.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum MatchState
{
    /// <summary>
    /// The lobby is waiting for a second participant.
    /// </summary>
    [EnumMember]
    Waiting,

    /// <summary>
    /// The match is in progress.
    /// </summary>
    [EnumMember]
    InProgress,

    /// <summary>
    /// The match has a confirmed conclusion.
    /// </summary>
    [EnumMember]
    Finished,

    /// <summary>
    /// The waiting lobby was cancelled.
    /// </summary>
    [EnumMember]
    Cancelled,
}
