using System.Runtime.Serialization;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.Contracts.Leaderboards;

/// <summary>
/// Represents one ranked account in the global leaderboard.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LeaderboardEntry
{
    /// <summary>
    /// Gets or sets the consecutive one-based leaderboard position.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets the public account identifier and username.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public AccountReference Account { get; set; } = new AccountReference();

    /// <summary>
    /// Gets or sets whether the account has a custom profile image.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public bool HasProfileImage { get; set; }

    /// <summary>
    /// Gets or sets the number of completed matches counted for statistics.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public int MatchesPlayed { get; set; }

    /// <summary>
    /// Gets or sets the number of wins counted for statistics.
    /// </summary>
    [DataMember(Order = 5, IsRequired = true)]
    public int Wins { get; set; }

    /// <summary>
    /// Gets or sets the win ratio between zero and one, or zero when no matches are counted.
    /// </summary>
    [DataMember(Order = 6, IsRequired = true)]
    public double WinRate { get; set; }
}
