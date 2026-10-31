using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class PlayerProfileClient
{
    private readonly ILogger<PlayerProfileClient> _logger;
    private readonly Func<Proxy.PlayerProfileServiceClient> _createClient;

    internal PlayerProfileClient(ILogger<PlayerProfileClient> logger)
        : this(logger, ServerConnectionFactory.CreateProfileClient)
    {
    }

    internal PlayerProfileClient(
        ILogger<PlayerProfileClient> logger,
        Func<Proxy.PlayerProfileServiceClient> createClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
    }

    internal Task<Proxy.PlayerProfile> GetPlayerProfileAsync(
        string token, long? targetAccountId = null) =>
        ExecuteAsync(client => client.GetPlayerProfileAsync(token, targetAccountId));

    internal Task<Proxy.ImageContent> GetProfileImageAsync(
        string token, long? targetAccountId = null) =>
        ExecuteAsync(client => client.GetProfileImageAsync(token, targetAccountId));

    internal Task ChangeUsernameAsync(string token, string username) => ExecuteAsync(async client =>
    {
        await client.ChangeUsernameAsync(token, username);
        return true;
    });

    internal Task UpdateProfileImageAsync(string token, Proxy.ImageContent image) =>
        ExecuteAsync(async client =>
    {
        await client.UpdateProfileImageAsync(token, image);
        return true;
    });

    private Task<TResult> ExecuteAsync<TResult>(
        Func<Proxy.PlayerProfileServiceClient, Task<TResult>> operation)
    {
        Proxy.PlayerProfileServiceClient client = _createClient();
        return RemoteCall.ExecuteAsync(client, () => operation(client), _logger);
    }
}
