using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using ContractEntry = ReversiGameSystem.Server.Contracts.Leaderboards.LeaderboardEntry;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Exposes server-calculated global standings to registered users and guests.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class LeaderboardService : ILeaderboardService
{
    private readonly IGetLeaderboardHandler _handler;
    private readonly ILogger<LeaderboardService> _logger;

    /// <summary>Initializes the leaderboard boundary.</summary>
    /// <param name="handler">The server-calculated leaderboard query use case.</param>
    /// <param name="logger">The boundary diagnostic logger.</param>
    /// <exception cref="ArgumentNullException">A required dependency is absent.</exception>
    public LeaderboardService(IGetLeaderboardHandler handler, ILogger<LeaderboardService> logger)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ContractEntry[] GetLeaderboard(int maximumEntries)
    {
        const string Operation = nameof(GetLeaderboard);
        string correlationId = CorrelationContext.GetOrCreate();
        try
        {
            LeaderboardResult result = _handler.ExecuteAsync(new LeaderboardQuery(maximumEntries))
                .GetAwaiter().GetResult();
            if (result.Outcome == LeaderboardOutcome.InvalidRequest)
            {
                throw new FaultException<LeaderboardFault>(new LeaderboardFault
                {
                    Reason = LeaderboardFailure.InvalidMaximumEntries
                }, "The leaderboard request was rejected.");
            }

            if (result.Outcome != LeaderboardOutcome.Success)
            {
                throw new InvalidOperationException("The leaderboard store is unavailable.");
            }

            return result.Entries.Select(PlayerDataMapper.Map).ToArray();
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
}
