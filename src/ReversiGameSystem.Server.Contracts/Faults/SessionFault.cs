using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Describes a recognized session or access rejection.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class SessionFault
{
    /// <summary>
    /// Gets or sets the recognized rejection reason.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public SessionFailure Reason { get; set; }
}
