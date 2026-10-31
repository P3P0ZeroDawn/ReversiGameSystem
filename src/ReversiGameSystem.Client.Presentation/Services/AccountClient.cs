using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class AccountClient
{
    private readonly ILogger<AccountClient> _logger;
    private readonly Func<Proxy.AccountAccessServiceClient> _createClient;

    internal AccountClient(ILogger<AccountClient> logger)
        : this(logger, ServerConnectionFactory.CreateAccountClient)
    {
    }

    internal AccountClient(
        ILogger<AccountClient> logger,
        Func<Proxy.AccountAccessServiceClient> createClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
    }

    internal Task<Proxy.AccountRegistrationOutcome> RegisterAccountAsync(
        Proxy.AccountRegistrationCredentials credentials) =>
        ExecuteAsync(client => client.RegisterAccountAsync(credentials));

    private Task<TResult> ExecuteAsync<TResult>(
        Func<Proxy.AccountAccessServiceClient, Task<TResult>> operation)
    {
        Proxy.AccountAccessServiceClient client = _createClient();
        return RemoteCall.ExecuteAsync(client, () => operation(client), _logger);
    }
}
