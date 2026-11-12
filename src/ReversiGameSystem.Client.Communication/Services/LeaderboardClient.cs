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

    internal async Task<LeaderboardResult> GetLeaderboard(int maximumEntries)
    {
        Proxy.LeaderboardEntry[] remoteEntries;
        try
        {
            remoteEntries = await Execute(
                client => client.GetLeaderboardAsync(maximumEntries));
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            return new LeaderboardResult(
                LeaderboardStatus.ServiceUnavailable, Array.Empty<LeaderboardEntry>());
        }
        LeaderboardEntry[] entries = new LeaderboardEntry[remoteEntries.Length];
        for (int index = 0; index < remoteEntries.Length; index++)
        {
            Proxy.LeaderboardEntry remoteEntry = remoteEntries[index];
            entries[index] = new LeaderboardEntry(
                remoteEntry.Position,
                remoteEntry.Account.Username,
                remoteEntry.Wins);
        }

        return new LeaderboardResult(LeaderboardStatus.Success, entries);
    }

    private Task<TResult> Execute<TResult>(
        Func<Proxy.LeaderboardServiceClient, Task<TResult>> operation)
    {
        Proxy.LeaderboardServiceClient client = _createClient();
        return RemoteCall.Execute(client, () => operation(client), _logger);
    }
}
