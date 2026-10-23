using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>
/// Exposes account access use cases through WCF.
/// </summary>
[ServiceBehavior(
    InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple,
    UseSynchronizationContext = false)]
public sealed class AccountAccessService : IAccountAccessService
{
    private readonly ICreateAccountHandler _createAccountHandler;
    private readonly ILogger<AccountAccessService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountAccessService"/> class.
    /// </summary>
    /// <param name="createAccountHandler">The account registration use case.</param>
    /// <param name="logger">The local service logger.</param>
    public AccountAccessService(
        ICreateAccountHandler createAccountHandler,
        ILogger<AccountAccessService> logger)
    {
        _createAccountHandler = createAccountHandler
            ?? throw new ArgumentNullException(nameof(createAccountHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<AccountRegistrationOutcome> RegisterAccountAsync(
        AccountRegistrationCredentials credentials)
    {
        return ExecuteAsync(
            nameof(RegisterAccountAsync),
            async () =>
            {
                CreateAccountCommand command = AccountContractMapper.Map(credentials);
                CreateAccountResult result = await _createAccountHandler.ExecuteAsync(command);
                return AccountContractMapper.Map(result, credentials);
            });
    }

    private async Task<TResult> ExecuteAsync<TResult>(
        string operationName,
        Func<Task<TResult>> operation)
    {
        string correlationId = CorrelationContext.GetOrCreate();
        using (IDisposable? scope = _logger.BeginScope(
            new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            try
            {
                return await operation();
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "The {Operation} WCF operation failed. CorrelationId: {CorrelationId}",
                    operationName,
                    correlationId);
                throw ServiceFaultFactory.Create(operationName, correlationId);
            }
        }
    }
}
