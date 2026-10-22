using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Describes a recognized lobby, match or match access rejection.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class MatchFault
{
    /// <summary>
    /// Gets or sets the recognized rejection reason.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public MatchFailure Reason { get; set; }
}
