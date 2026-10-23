using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Players;
using ApplicationProfile = ReversiGameSystem.Server.Application.Players.PlayerProfile;
using ApplicationEntry = ReversiGameSystem.Server.Application.Leaderboards.LeaderboardEntry;
using ContractEntry = ReversiGameSystem.Server.Contracts.Leaderboards.LeaderboardEntry;

namespace ReversiGameSystem.Server.ServiceApi;

internal static class PlayerDataMapper
{
    internal static PlayerProfile Map(ApplicationProfile profile) => new PlayerProfile
    {
        Account = new AccountReference
        {
            AccountId = profile.AccountId, Username = profile.Username
        },
        HasProfileImage = !string.IsNullOrWhiteSpace(profile.ProfileImageUri),
        Statistics = new PlayerStatistics
        {
            MatchesPlayed = profile.Statistics.MatchesPlayed,
            Wins = profile.Statistics.Wins,
            Losses = profile.Statistics.Losses,
            Draws = profile.Statistics.Draws,
            WinRate = profile.Statistics.WinRate
        }
    };

    internal static ContractEntry Map(ApplicationEntry entry) => new ContractEntry
    {
        Position = entry.Rank,
        Account = new AccountReference { AccountId = entry.AccountId, Username = entry.Username },
        HasProfileImage = !string.IsNullOrWhiteSpace(entry.ProfileImageUri),
        MatchesPlayed = entry.MatchesPlayed,
        Wins = entry.Wins,
        WinRate = entry.WinRate
    };
}
