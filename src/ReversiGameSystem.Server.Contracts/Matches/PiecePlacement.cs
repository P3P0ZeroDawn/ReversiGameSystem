using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Requests a piece placement against a specific confirmed match state version.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PiecePlacement
{
    /// <summary>
    /// Gets or sets the stable match identifier.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public long MatchId { get; set; }

    /// <summary>
    /// Gets or sets the zero-based board row from zero through seven.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public int Row { get; set; }

    /// <summary>
    /// Gets or sets the zero-based board column from zero through seven.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public int Column { get; set; }

    /// <summary>
    /// Gets or sets the nonnegative match state version seen by the caller.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public long ExpectedMatchStateVersion { get; set; }
}
