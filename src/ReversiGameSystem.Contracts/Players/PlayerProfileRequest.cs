using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Players;

/// <summary>
/// Represents player profile request.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerProfileRequest
{
    /// <summary>
    /// Gets or sets the session token.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account id.
    /// </summary>
    [DataMember(Order = 2, EmitDefaultValue = false)]
    public long? AccountId { get; set; }
}
