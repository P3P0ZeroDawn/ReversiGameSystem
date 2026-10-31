using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class LeaderboardClient
{
    private readonly ILogger<LeaderboardClient> _logger;
    private readonly Func<Proxy.LeaderboardServiceClient> _createClient;

    internal LeaderboardClient(ILogger<LeaderboardClient> logger)
        : this(logger, ServerConnectionFactory.CreateLeaderboardClient)
    {
    }

    internal LeaderboardClient(
        ILogger<LeaderboardClient> logger,
        Func<Proxy.LeaderboardServiceClient> createClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
    }

    internal Task<Proxy.LeaderboardEntry[]> GetLeaderboardAsync(int maximumEntries) =>
        ExecuteAsync(client => client.GetLeaderboardAsync(maximumEntries));

    private Task<TResult> ExecuteAsync<TResult>(
        Func<Proxy.LeaderboardServiceClient, Task<TResult>> operation)
    {
        Proxy.LeaderboardServiceClient client = _createClient();
        return RemoteCall.ExecuteAsync(client, () => operation(client), _logger);
    }
}
