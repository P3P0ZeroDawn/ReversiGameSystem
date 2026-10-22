using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Identifies why a match ended.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum MatchEndReason
{
    /// <summary>
    /// Neither participant can make a legal move, even if empty cells remain.
    /// </summary>
    [EnumMember]
    NoLegalMoves,

    /// <summary>
    /// The match ended after consecutive turn timeouts.
    /// </summary>
    [EnumMember]
    Inactivity,

    /// <summary>
    /// A participant forfeited the match.
    /// </summary>
    [EnumMember]
    Abandonment,
}
