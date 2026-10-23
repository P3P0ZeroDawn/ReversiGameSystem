using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class LeaderboardClient
{
    private readonly ILogger<LeaderboardClient> _logger;

    internal LeaderboardClient(ILogger<LeaderboardClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal Task<Proxy.LeaderboardEntry[]> GetLeaderboardAsync(int maximumEntries) =>
        ExecuteAsync(client => client.GetLeaderboardAsync(maximumEntries));

    private Task<TResult> ExecuteAsync<TResult>(
        Func<Proxy.LeaderboardServiceClient, Task<TResult>> operation)
    {
        Proxy.LeaderboardServiceClient client = new Proxy.LeaderboardServiceClient("LeaderboardEndpoint");
        return RemoteCall.ExecuteAsync(client, () => operation(client), _logger);
    }
}
