using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Identifies what initiated a confirmed placement.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum MoveOrigin
{
    /// <summary>
    /// The placement was requested by a human participant.
    /// </summary>
    [EnumMember]
    Manual,

    /// <summary>
    /// The server placed a piece after the turn timed out.
    /// </summary>
    [EnumMember]
    Timeout,

    /// <summary>
    /// The participant or move is controlled by the server.
    /// </summary>
    [EnumMember]
    System,
}
