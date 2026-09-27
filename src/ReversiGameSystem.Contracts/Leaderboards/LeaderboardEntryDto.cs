using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Leaderboards;

/// <summary>
/// Represents leaderboard entry dto.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LeaderboardEntryDto
{
    /// <summary>
    /// Gets or sets the rank.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public int Rank { get; set; }

    /// <summary>
    /// Gets or sets the account id.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public long AccountId { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the profile image uri.
    /// </summary>
    [DataMember(Order = 4, EmitDefaultValue = false)]
    public string? ProfileImageUri { get; set; }

    /// <summary>
    /// Gets or sets the matches played.
    /// </summary>
    [DataMember(Order = 5, IsRequired = true)]
    public int MatchesPlayed { get; set; }

    /// <summary>
    /// Gets or sets the wins.
    /// </summary>
    [DataMember(Order = 6, IsRequired = true)]
    public int Wins { get; set; }

    /// <summary>
    /// Gets or sets the win ratio from 0.0 to 1.0.
    /// </summary>
    [DataMember(Order = 7, IsRequired = true)]
    public double WinRate { get; set; }
}
