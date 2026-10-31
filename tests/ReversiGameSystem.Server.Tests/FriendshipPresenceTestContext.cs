using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Friendships;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

internal sealed class FriendshipPresenceTestContext : IDisposable, IAuthenticatedSessionStore,
    IFriendshipRepository
{
    private readonly ConcurrentDictionary<string, AuthenticatedSessionRecord> _sessions =
        new ConcurrentDictionary<string, AuthenticatedSessionRecord>();
    private readonly ConcurrentDictionary<string, bool> _expired =
        new ConcurrentDictionary<string, bool>();
    private readonly List<FriendshipPresenceTestClient> _clients = new List<FriendshipPresenceTestClient>();
    private readonly ServiceHost _host;
    private readonly Uri _address = new Uri("net.pipe://localhost/ReversiFriendshipTests/"
        + Guid.NewGuid().ToString("N") + "/Friendship");
    internal bool Inactive;
    internal bool FailRepository;
    internal volatile bool FailRelationshipChecks;
    internal int FriendshipReads;
    internal bool FriendshipActive = true;
    internal int RelationshipChecks;
    internal int FailedRelationshipChecks;
    internal readonly Exception RepositoryFailure =
        new InvalidOperationException("private-provider-diagnostic");
    internal readonly FriendshipService Service;

    internal FriendshipPresenceTestContext() : this(NullLogger<FriendshipService>.Instance)
    {
    }

    internal FriendshipPresenceTestContext(ILogger<FriendshipService> logger)
    {
        Service = new FriendshipService(this, this, logger);
        _host = new ServiceHost(Service, _address);
        _host.AddServiceEndpoint(typeof(IFriendshipService), Binding(), string.Empty);
        _host.Open();
        AddSession("one", 1);
        AddSession("two", 2);
        AddSession("stranger", 3);
    }

    internal void AddSession(string token, long accountId) =>
        _sessions[token] = new AuthenticatedSessionRecord(token, accountId,
            "Player" + accountId, "unused", "en-US", DateTime.UtcNow.AddHours(1));

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

    public void Revoke(string sessionToken) => _sessions.TryRemove(sessionToken, out _);
    internal void Expire(string token) => _expired[token] = true;
    public bool TryGet(string sessionToken, out AuthenticatedSessionRecord? session)
    {
        session = null;
        return !_expired.ContainsKey(sessionToken ?? string.Empty)
            && _sessions.TryGetValue(sessionToken ?? string.Empty, out session);
    }
    public AuthenticatedSessionRecord Create(LoginAccount account) => throw new NotSupportedException();

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
        System.Threading.Interlocked.Increment(ref FriendshipReads);
        if (accountId == 1) { return new[] { new FriendAccount(2, "Player2", true) }; }
        if (accountId == 2) { return new[] { new FriendAccount(1, "Player1", false) }; }
        return Array.Empty<FriendAccount>();
    }

    public bool AreFriends(long accountId, long friendAccountId)
    {
        System.Threading.Interlocked.Increment(ref RelationshipChecks);
        if (FailRelationshipChecks && accountId == 1 && friendAccountId == 2)
        {
            System.Threading.Interlocked.Increment(ref FailedRelationshipChecks);
            throw new InvalidOperationException("Private relationship provider diagnostic");
        }
        return FriendshipActive && !Inactive && ((accountId == 1 && friendAccountId == 2)
            || (accountId == 2 && friendAccountId == 1));
    }

    public void Dispose()
    {
        foreach (FriendshipPresenceTestClient client in _clients) { client.Dispose(); }
        _host.Abort();
        Service.Dispose();
    }

    internal static NetNamedPipeBinding Binding() => new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
    {
        SendTimeout = TimeSpan.FromSeconds(2), CloseTimeout = TimeSpan.FromSeconds(2)
    };
}

[CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
internal sealed class FriendshipPresenceTestClient : IFriendshipServiceCallback, IDisposable
{
    private readonly DuplexChannelFactory<IFriendshipService> _factory;
    private readonly ConcurrentQueue<Tuple<long, FriendChange>> _changes =
        new ConcurrentQueue<Tuple<long, FriendChange>>();
    internal IFriendshipService Channel { get; }

    internal FriendshipPresenceTestClient(Uri address)
    {
        _factory = new DuplexChannelFactory<IFriendshipService>(new InstanceContext(this),
            FriendshipPresenceTestContext.Binding(), new EndpointAddress(address));
        Channel = _factory.CreateChannel();
    }

    internal int Count(long accountId, FriendChange change) =>
        _changes.Count(item => item.Item1 == accountId && item.Item2 == change);
    internal void Close() => ((ICommunicationObject)Channel).Close();
    public void OnFriendRequestReceived(FriendRequest request) { }
    public void OnFriendRequestResponded(string requestId, bool accepted) { }
    public void OnFriendChanged(long accountId, FriendChange change) =>
        _changes.Enqueue(Tuple.Create(accountId, change));
    public void Dispose()
    {
        ((ICommunicationObject)Channel).Abort();
        _factory.Abort();
    }
}
