using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Players;

/// <summary>
/// Represents player profile response.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerProfileResponse
{
    /// <summary>
    /// Gets or sets the result code.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public PlayerProfileResultCode ResultCode { get; set; }

    /// <summary>
    /// Gets or sets the profile.
    /// </summary>
    [DataMember(Order = 2, EmitDefaultValue = false)]
    public PlayerProfileDto? Profile { get; set; }
}
