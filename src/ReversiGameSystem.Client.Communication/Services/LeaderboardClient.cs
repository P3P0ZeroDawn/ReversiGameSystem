using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class LeaderboardClient
{
    private LeaderboardEntry[] _entries = Array.Empty<LeaderboardEntry>();
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

    internal LeaderboardEntry[] ReadLeaderboard()
    {
        return (LeaderboardEntry[])_entries.Clone();
    }

    internal async Task<LeaderboardStatus> LoadLeaderboard(int maximumEntries)
    {
        Proxy.LeaderboardEntry[] remoteEntries;
        try
        {
            remoteEntries = await SendLeaderboardOperation(
                client => client.GetLeaderboardAsync(maximumEntries));
        }
        catch (FaultException<Proxy.LeaderboardFault> fault)
            when (fault.Detail.Reason == Proxy.LeaderboardFailure.InvalidMaximumEntries)
        {
            return LeaderboardStatus.InvalidMaximumEntries;
        }
        catch (CommunicationException exception) when (RemoteCall.IsConnectionFailure(exception))
        {
            return LeaderboardStatus.ServiceUnavailable;
        }
        catch (TimeoutException)
        {
            return LeaderboardStatus.ServiceUnavailable;
        }
        LeaderboardEntry[] entries = new LeaderboardEntry[remoteEntries.Length];
        for (int index = 0; index < remoteEntries.Length; index++)
        {
            Proxy.LeaderboardEntry remoteEntry = remoteEntries[index];
            entries[index] = new LeaderboardEntry(
                remoteEntry.Account.AccountId,
                remoteEntry.Position,
                remoteEntry.Account.Username,
                remoteEntry.Wins);
        }

        _entries = entries;
        return LeaderboardStatus.Success;
    }

    private Task<TResult> SendLeaderboardOperation<TResult>(
        Func<Proxy.LeaderboardServiceClient, Task<TResult>> operation)
    {
        Proxy.LeaderboardServiceClient client = _createClient();
        return RemoteCall.SendServiceCall(client, () => operation(client), _logger);
    }
}
