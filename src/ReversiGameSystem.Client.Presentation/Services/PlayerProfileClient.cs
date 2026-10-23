using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class PlayerProfileClient
{
    private readonly ILogger<PlayerProfileClient> _logger;

    internal PlayerProfileClient(ILogger<PlayerProfileClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal Task<Proxy.PlayerProfile> GetPlayerProfileAsync(
        string token, long? targetAccountId = null) =>
        ExecuteAsync(client => client.GetPlayerProfileAsync(token, targetAccountId));

    internal Task<Proxy.ImageContent> GetProfileImageAsync(string token, long? targetAccountId = null) =>
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
        Proxy.PlayerProfileServiceClient client =
            new Proxy.PlayerProfileServiceClient("PlayerProfileEndpoint");
        return RemoteCall.ExecuteAsync(client, () => operation(client), _logger);
    }
}
