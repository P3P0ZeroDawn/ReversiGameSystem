using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Identifies the proposed or confirmed match opponent kind.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum OpponentKind
{
    /// <summary>
    /// The opponent is a human participant.
    /// </summary>
    [EnumMember]
    Human,

    /// <summary>
    /// The participant or move is controlled by the server.
    /// </summary>
    [EnumMember]
    System,
}
