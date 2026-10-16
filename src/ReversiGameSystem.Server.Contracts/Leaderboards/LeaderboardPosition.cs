using System.Runtime.Serialization;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.Contracts.Leaderboards;

/// <summary>
/// Represents one ranked account in the leaderboard standings.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LeaderboardPosition
{
    /// <summary>
    /// Gets or sets the one-based leaderboard position.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets the ranked account.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public AccountReference Account { get; set; } = new AccountReference();

    /// <summary>
    /// Gets or sets a value indicating whether the account has a profile image.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public bool HasProfileImage { get; set; }

    /// <summary>
    /// Gets or sets the number of completed matches.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public int MatchesPlayed { get; set; }

    /// <summary>
    /// Gets or sets the number of wins.
    /// </summary>
    [DataMember(Order = 5, IsRequired = true)]
    public int Wins { get; set; }

    /// <summary>
    /// Gets or sets the ratio of wins to completed matches.
    /// </summary>
    [DataMember(Order = 6, IsRequired = true)]
    public double WinRate { get; set; }
}
