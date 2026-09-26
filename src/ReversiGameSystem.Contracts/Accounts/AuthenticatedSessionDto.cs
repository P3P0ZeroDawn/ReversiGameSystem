using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Accounts;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class AuthenticatedSessionDto
{
    [DataMember(Order = 1, IsRequired = true)]
    public string SessionToken { get; set; } = string.Empty;

    [DataMember(Order = 2, IsRequired = true)]
    public long AccountId { get; set; }

    [DataMember(Order = 3, IsRequired = true)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 4, IsRequired = true)]
    public string EmailAddress { get; set; } = string.Empty;
}
