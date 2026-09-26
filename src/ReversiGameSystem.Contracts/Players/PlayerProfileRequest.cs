using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Players;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerProfileRequest
{
    [DataMember(Order = 1, IsRequired = true)]
    public string SessionToken { get; set; } = string.Empty;

    [DataMember(Order = 2, EmitDefaultValue = false)]
    public long? AccountId { get; set; }
}
