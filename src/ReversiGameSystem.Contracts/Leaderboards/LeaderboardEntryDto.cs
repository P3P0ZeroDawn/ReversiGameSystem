using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Leaderboards;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LeaderboardEntryDto
{
    [DataMember(Order = 1, IsRequired = true)]
    public int Rank { get; set; }

    [DataMember(Order = 2, IsRequired = true)]
    public long AccountId { get; set; }

    [DataMember(Order = 3, IsRequired = true)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 4, EmitDefaultValue = false)]
    public string? ProfileImageUri { get; set; }

    [DataMember(Order = 5, IsRequired = true)]
    public int MatchesPlayed { get; set; }

    [DataMember(Order = 6, IsRequired = true)]
    public int Wins { get; set; }

    /// <summary>
    /// Gets or sets the win ratio from 0.0 to 1.0.
    /// </summary>
    [DataMember(Order = 7, IsRequired = true)]
    public double WinRate { get; set; }
}
