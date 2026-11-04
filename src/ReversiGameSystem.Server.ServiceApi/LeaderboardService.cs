using System;
using System.Linq;
using System.ServiceModel;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using ApplicationEntry = ReversiGameSystem.Server.Application.Leaderboards.LeaderboardEntry;
using ContractEntry = ReversiGameSystem.Server.Contracts.Leaderboards.LeaderboardEntry;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Exposes server-calculated global standings to registered users and guests.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class LeaderboardService : ILeaderboardService
{
    private readonly LeaderboardApplicationService _applicationService;

    /// <summary>Initializes the leaderboard boundary.</summary>
    /// <param name="applicationService">The leaderboard application service.</param>
    /// <exception cref="ArgumentNullException">A required dependency is absent.</exception>
    public LeaderboardService(LeaderboardApplicationService applicationService)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
    }

    /// <inheritdoc />
    public ContractEntry[] GetLeaderboard(int maximumEntries)
    {
        LeaderboardResult result = _applicationService.GetAsync(maximumEntries)
            .GetAwaiter().GetResult();
        if (result.Status == LeaderboardStatus.InvalidMaximumEntries)
        {
            throw new FaultException<LeaderboardFault>(new LeaderboardFault
            {
                Reason = LeaderboardFailure.InvalidMaximumEntries
            }, "The leaderboard request was rejected.");
        }

        return result.Entries.Select(MapEntry).ToArray();
    }

    private static ContractEntry MapEntry(ApplicationEntry entry)
    {
        return new ContractEntry
        {
            Position = entry.Position,
            Account = new AccountReference
            {
                AccountId = entry.AccountId,
                Username = entry.Username
            },
            HasProfileImage = !string.IsNullOrWhiteSpace(entry.ProfileImageUri),
            MatchesPlayed = entry.MatchesPlayed,
            Wins = entry.Wins,
            WinRate = entry.WinRate
        };
    }
}
