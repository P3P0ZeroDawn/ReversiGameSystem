using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Describes a recognized player profile rejection.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class ProfileFault
{
    /// <summary>
    /// Gets or sets the recognized rejection reason.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public ProfileFailure Reason { get; set; }
}
