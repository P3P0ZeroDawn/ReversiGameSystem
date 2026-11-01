using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Players;
using ApplicationProfile = ReversiGameSystem.Server.Application.Players.PlayerProfile;

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
}
