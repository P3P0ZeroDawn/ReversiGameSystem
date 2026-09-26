using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Accounts;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LoginRequest
{
    [DataMember(Order = 1, IsRequired = true)]
    public string Login { get; set; } = string.Empty;

    [DataMember(Order = 2, IsRequired = true)]
    public string Password { get; set; } = string.Empty;
}
