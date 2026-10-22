using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Contains the public information needed to select and join a waiting match.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class AvailableMatch
{
    /// <summary>
    /// Gets or sets the temporary code used to join the available lobby.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string MatchCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confirmed display name of the lobby creator.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public string CreatorDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configured turn duration in seconds, from 10 through 120.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public int TurnDurationSeconds { get; set; }
}
