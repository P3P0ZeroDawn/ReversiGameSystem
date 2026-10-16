using System.Runtime.Serialization;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.Contracts.Accounts;

/// <summary>
/// Represents the expected outcome of account registration.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class AccountRegistrationOutcome
{
    /// <summary>
    /// Gets or sets the registration status.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public AccountRegistrationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the registered account when registration succeeds.
    /// </summary>
    [DataMember(Order = 2, EmitDefaultValue = false)]
    public AccountReference? RegisteredAccount { get; set; }
}
