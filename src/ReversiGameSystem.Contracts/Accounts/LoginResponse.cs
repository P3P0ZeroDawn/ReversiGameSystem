using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Accounts;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LoginResponse
{
    [DataMember(Order = 1, IsRequired = true)]
    public LoginResultCode ResultCode { get; set; }

    [DataMember(Order = 2, EmitDefaultValue = false)]
    public AuthenticatedSessionDto? Session { get; set; }
}
