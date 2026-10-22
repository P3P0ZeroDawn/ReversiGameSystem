using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Matches;

/// <summary>
/// Contains the confirmed participants, configuration and state of a match lobby.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LobbySnapshot
{
    /// <summary>
    /// Gets or sets the stable match identifier.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public long MatchId { get; set; }

    /// <summary>
    /// Gets or sets the temporary join code, or null when the lobby no longer accepts joins.
    /// </summary>
    [DataMember(Order = 2, EmitDefaultValue = false)]
    public string? MatchCode { get; set; }

    /// <summary>
    /// Gets or sets the confirmed participants and their seats within the match.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public MatchParticipantSummary[] Participants { get; set; } =
        Array.Empty<MatchParticipantSummary>();

    /// <summary>
    /// Gets or sets the confirmed lobby configuration.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public MatchConfiguration Configuration { get; set; } = new MatchConfiguration();

    /// <summary>
    /// Gets or sets the confirmed match lifecycle state.
    /// </summary>
    [DataMember(Order = 5, IsRequired = true)]
    public MatchState State { get; set; }
}
