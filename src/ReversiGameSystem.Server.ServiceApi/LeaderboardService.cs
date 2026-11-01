using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
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
    private readonly GetLeaderboardUseCase _getLeaderboard;
    private readonly ILogger<LeaderboardService> _logger;

    /// <summary>Initializes the leaderboard boundary.</summary>
    /// <param name="getLeaderboard">The server-calculated leaderboard use case.</param>
    /// <param name="logger">The boundary diagnostic logger.</param>
    /// <exception cref="ArgumentNullException">A required dependency is absent.</exception>
    public LeaderboardService(
        GetLeaderboardUseCase getLeaderboard,
        ILogger<LeaderboardService> logger)
    {
        _getLeaderboard = getLeaderboard
            ?? throw new ArgumentNullException(nameof(getLeaderboard));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ContractEntry[] GetLeaderboard(int maximumEntries)
    {
        const string Operation = nameof(GetLeaderboard);
        string correlationId = CorrelationContext.GetOrCreate();
        try
        {
            LeaderboardResult result = _getLeaderboard.GetAsync(maximumEntries)
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
        catch (FaultException<LeaderboardFault>)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "The {Operation} operation failed. "
                + "CorrelationId: {CorrelationId}",
                Operation, correlationId);
            throw ServiceFaultFactory.Create(Operation, correlationId);
        }
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
