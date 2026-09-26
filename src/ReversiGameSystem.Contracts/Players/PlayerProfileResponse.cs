using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Players;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerProfileResponse
{
    [DataMember(Order = 1, IsRequired = true)]
    public PlayerProfileResultCode ResultCode { get; set; }

    [DataMember(Order = 2, EmitDefaultValue = false)]
    public PlayerProfileDto? Profile { get; set; }
}
