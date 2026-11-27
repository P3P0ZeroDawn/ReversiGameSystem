using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Friendships;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services;

internal sealed class FriendshipPresenceTestContext : IDisposable, IFriendshipRepository
{
    private const string PrivateRelationshipDiagnostic =
        "Private relationship provider diagnostic";
    private readonly SessionStore _sessions = new SessionStore(new TestUtcClock());
    private readonly List<FriendshipPresenceTestClient> _clients =
        new List<FriendshipPresenceTestClient>();
    private readonly ServiceHost _host;
    private readonly Timer _sweepTimer;
    private readonly Uri _address = new Uri("net.pipe://localhost/ReversiFriendshipTests/"
        + Guid.NewGuid().ToString("N") + "/Friendship");
    private volatile bool _inactive = false;
    private volatile bool _failRepository = false;
    private volatile bool _failRelationshipChecks;
    private int _friendshipReads;
    private volatile bool _friendshipActive = true;
    private int _relationshipChecks;
    private int _failedRelationshipChecks;

    internal bool Inactive
    {
        get
        {
            return _inactive;
        }
        set
        {
            _inactive = value;
        }
    }

    internal bool FailRepository
    {
        get
        {
            return _failRepository;
        }
        set
        {
            _failRepository = value;
        }
    }

    internal bool FailRelationshipChecks
    {
        get
        {
            return _failRelationshipChecks;
        }
        set
        {
            _failRelationshipChecks = value;
        }
    }

    internal int FriendshipReads
    {
        get
        {
            return Volatile.Read(ref _friendshipReads);
        }
    }

    internal bool FriendshipActive
    {
        get
        {
            return _friendshipActive;
        }
        set
        {
            _friendshipActive = value;
        }
    }

    internal int RelationshipChecks
    {
        get
        {
            return Volatile.Read(ref _relationshipChecks);
        }
    }

    internal int FailedRelationshipChecks
    {
        get
        {
            return Volatile.Read(ref _failedRelationshipChecks);
        }
    }

    internal Exception RepositoryFailure { get; } =
        new InvalidOperationException("private-provider-diagnostic");

    internal FriendshipService Service { get; }

    internal AccountPresenceRegistry PresenceRegistry { get; }

    internal FriendshipSubscriptions Subscriptions { get; }

    internal FriendshipApplicationService ApplicationService { get; }

    internal FriendshipPresenceTestContext()
        : this(NullLogger<FriendshipSubscriptions>.Instance)
    {
    }

    internal FriendshipPresenceTestContext(
        ILogger<FriendshipSubscriptions> logger,
        ILogger<ServiceErrorBehavior>? behaviorLogger = null)
    {
        PresenceRegistry = new AccountPresenceRegistry();
        Subscriptions = new FriendshipSubscriptions(logger);
        ApplicationService = new FriendshipApplicationService(_sessions, this);
        PresenceRegistry.PresenceChanged += (accountId, isOnline) =>
        {
            try
            {
                long[] friendIds = ApplicationService.ReadConfirmedFriendIds(accountId);
                if (friendIds.Length == 0)
                {
                    return;
                }

                HashSet<long> targets = new HashSet<long>(friendIds);
                FriendChange change = isOnline
                    ? FriendChange.Connected
                    : FriendChange.Disconnected;
                Subscriptions.PublishToAccounts(
                    targets,
                    callback => callback.OnFriendChanged(accountId, change));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish presence change in test context.");
            }
        };
        _sweepTimer = new Timer(
            _ => Subscriptions.SweepChannels(
                64,
                token => ApplicationService.EvaluateChannelAccess(token).Status
                    == FriendshipStatus.Success),
            null,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1));
        Service = new FriendshipService(ApplicationService, PresenceRegistry, Subscriptions);
        _host = new ServiceHost(Service, _address);
        _host.Description.Behaviors.Add(new ServiceErrorBehavior(
            behaviorLogger ?? NullLogger<ServiceErrorBehavior>.Instance));
        _host.AddServiceEndpoint(typeof(IFriendshipService), Binding(), string.Empty);
        _host.Open();
        AddSession("one", 1);
        AddSession("two", 2);
        AddSession("stranger", 3);
    }

    internal void AddSession(string token, long accountId)
    {
        _sessions.TryAddAuthenticated(new AuthenticatedSession(
            token,
            accountId,
            "Player" + accountId,
            "unused",
            "en-US",
            _sessions.CalculateExpirationUtc()));
    }

    internal FriendshipPresenceTestClient Connect(string token)
    {
        FriendshipPresenceTestClient client = new FriendshipPresenceTestClient(_address);
        _clients.Add(client);
        client.Channel.OpenFriendshipChannel(token);
        return client;
    }

    internal FriendshipPresenceTestClient CreateClient()
    {
        FriendshipPresenceTestClient client = new FriendshipPresenceTestClient(_address);
        _clients.Add(client);
        return client;
    }

    internal void Expire(string token)
    {
        _sessions.RevokeSession(token);
    }

    public bool IsAccountActive(long accountId)
    {
        if (FailRepository)
        {
            throw RepositoryFailure;
        }

        return !Inactive;
    }

    public FriendAccount[] GetFriends(long accountId)
    {
        Interlocked.Increment(ref _friendshipReads);
        if (FailRepository)
        {
            throw RepositoryFailure;
        }

        if (accountId == 1)
        {
            return new[] { new FriendAccount(2, "Player2", true) };
        }

        if (accountId == 2)
        {
            return new[] { new FriendAccount(1, "Player1", false) };
        }

        return Array.Empty<FriendAccount>();
    }

    public bool AreFriends(long accountId, long friendAccountId)
    {
        Interlocked.Increment(ref _relationshipChecks);
        if (FailRelationshipChecks && accountId == 1 && friendAccountId == 2)
        {
            Interlocked.Increment(ref _failedRelationshipChecks);
            throw new InvalidOperationException(PrivateRelationshipDiagnostic);
        }

        return FriendshipActive && !Inactive && ((accountId == 1 && friendAccountId == 2)
            || (accountId == 2 && friendAccountId == 1));
    }

    public long[] ReadConfirmedFriendIds(long accountId)
    {
        Interlocked.Increment(ref _relationshipChecks);
        if (FailRelationshipChecks)
        {
            Interlocked.Increment(ref _failedRelationshipChecks);
            throw new InvalidOperationException(PrivateRelationshipDiagnostic);
        }

        if (!FriendshipActive || Inactive)
        {
            return Array.Empty<long>();
        }

        if (accountId == 1)
        {
            return new[] { 2L };
        }

        if (accountId == 2)
        {
            return new[] { 1L };
        }

        return Array.Empty<long>();
    }

    public void Dispose()
    {
        _sweepTimer.Dispose();
        foreach (FriendshipPresenceTestClient client in _clients)
        {
            client.Dispose();
        }

        _host.Abort();
        Subscriptions.Dispose();
    }

    internal static NetNamedPipeBinding Binding()
    {
        return new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
        {
            SendTimeout = TimeSpan.FromSeconds(2),
            CloseTimeout = TimeSpan.FromSeconds(2)
        };
    }
}

internal interface IFriendshipServiceChannel : IFriendshipService, IClientChannel
{
}

[CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
internal sealed class FriendshipPresenceTestClient : IFriendshipServiceCallback, IDisposable
{
    private readonly DuplexChannelFactory<IFriendshipServiceChannel> _factory;
    private readonly ConcurrentQueue<Tuple<long, FriendChange>> _changes =
        new ConcurrentQueue<Tuple<long, FriendChange>>();
    internal IFriendshipServiceChannel Channel { get; }

    internal CommunicationState ChannelState
    {
        get
        {
            return Channel.State;
        }
    }

    internal FriendshipPresenceTestClient(Uri address)
    {
        _factory = new DuplexChannelFactory<IFriendshipServiceChannel>(new InstanceContext(this),
            FriendshipPresenceTestContext.Binding(), new EndpointAddress(address));
        Channel = _factory.CreateChannel();
    }

    internal int Count(long accountId, FriendChange change)
    {
        return _changes.Count(item => item.Item1 == accountId && item.Item2 == change);
    }

    internal void Close()
    {
        Channel.Close();
    }

    public void OnFriendRequestReceived(FriendRequest request)
    {
    }

    public void OnFriendRequestResponded(string requestId, bool accepted)
    {
    }

    public void OnFriendChanged(long accountId, FriendChange change)
    {
        _changes.Enqueue(Tuple.Create(accountId, change));
    }

    public void Dispose()
    {
        Channel.Abort();
        _factory.Abort();
    }
}
