using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Identifies how a match participant is represented.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum ParticipantKind
{
    /// <summary>
    /// The participant represents a registered account.
    /// </summary>
    [EnumMember]
    Registered,

    /// <summary>
    /// The participant has a guest session.
    /// </summary>
    [EnumMember]
    Guest,

    /// <summary>
    /// The participant or move is controlled by the server.
    /// </summary>
    [EnumMember]
    System,
}
