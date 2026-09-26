using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Players;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerSummaryResponse
{
    [DataMember(Order = 1, IsRequired = true)]
    public PlayerSummaryResultCode ResultCode { get; set; }

    [DataMember(Order = 2, EmitDefaultValue = false)]
    public PlayerSummaryDto? Summary { get; set; }
}
