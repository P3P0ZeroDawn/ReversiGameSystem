using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Accounts;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class CreateAccountResponse
{
    [DataMember(Order = 1, IsRequired = true)]
    public CreateAccountResultCode ResultCode { get; set; }

    [DataMember(Order = 2, EmitDefaultValue = false)]
    public long? AccountId { get; set; }
}
