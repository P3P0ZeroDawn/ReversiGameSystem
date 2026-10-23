using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class AccountClient
{
    private readonly ILogger<AccountClient> _logger;

    internal AccountClient(ILogger<AccountClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal Task<Proxy.AccountRegistrationOutcome> RegisterAccountAsync(
        Proxy.AccountRegistrationCredentials credentials) =>
        ExecuteAsync(client => client.RegisterAccountAsync(credentials));

    private Task<TResult> ExecuteAsync<TResult>(
        Func<Proxy.AccountAccessServiceClient, Task<TResult>> operation)
    {
        Proxy.AccountAccessServiceClient client =
            new Proxy.AccountAccessServiceClient("AccountAccessEndpoint");
        return RemoteCall.ExecuteAsync(client, () => operation(client), _logger);
    }
}
