using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Describes a recognized registration, password or deactivation rejection.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class AccountFault
{
    /// <summary>
    /// Gets or sets the recognized rejection reason.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public AccountFailure Reason { get; set; }
}
