using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Accounts;

/// <summary>
/// Represents create account response.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class CreateAccountResponse
{
    /// <summary>
    /// Gets or sets the result code.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public CreateAccountResultCode ResultCode { get; set; }

    /// <summary>
    /// Gets or sets the account id.
    /// </summary>
    [DataMember(Order = 2, EmitDefaultValue = false)]
    public long? AccountId { get; set; }
}
