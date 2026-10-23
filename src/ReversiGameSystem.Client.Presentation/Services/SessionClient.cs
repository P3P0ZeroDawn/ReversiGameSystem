using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class SessionClient
{
    private readonly ILogger<SessionClient> _logger;
    private readonly Func<Proxy.SessionServiceClient> _createClient;

    internal SessionClient(ILogger<SessionClient> logger)
        : this(logger, () => new Proxy.SessionServiceClient("SessionEndpoint"))
    {
    }

    internal SessionClient(ILogger<SessionClient> logger,
        Func<Proxy.SessionServiceClient> createClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
    }

    internal Task<DateTime> RequestLoginCodeAsync(string loginIdentifier, string password) =>
        ExecuteAsync(client => client.RequestLoginCodeAsync(loginIdentifier, password));

    internal Task<Proxy.AuthenticatedSession> CompleteLoginAsync(
        string loginIdentifier, string code) =>
        ExecuteAsync(client => client.CompleteLoginAsync(loginIdentifier, code));

    internal Task<Proxy.GuestSession> OpenGuestSessionAsync(string displayName) =>
        ExecuteAsync(client => client.OpenGuestSessionAsync(displayName));

    internal Task CloseSessionAsync(string token) => ExecuteAsync(async client =>
    {
        await client.CloseSessionAsync(token);
        return true;
    });

    // This boundary retains one cleanup path for every request and response failure.
    private async Task<TResult> ExecuteAsync<TResult>(
        Func<Proxy.SessionServiceClient, Task<TResult>> operation)
    {
        Proxy.SessionServiceClient client = _createClient();
        string correlationId = Guid.NewGuid().ToString("D");
        try
        {
            Task<TResult> pending;
            using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
            {
                OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader(
                    "CorrelationId", "urn:reversi:diagnostics:v1", correlationId));
                pending = operation(client);
            }

            TResult result = await pending;
            try
            {
                client.Close();
            }
            catch (Exception exception) when (
                exception is CommunicationException or TimeoutException)
            {
                _logger.LogWarning("The acknowledged session channel could not close. "
                    + "CorrelationId: {CorrelationId}",
                    correlationId);
            }

            return result;
        }
        catch (FaultException<Proxy.SessionFault> fault)
        {
            _logger.LogInformation("The session request was rejected with {Reason}. "
                + "CorrelationId: {CorrelationId}",
                fault.Detail.Reason, correlationId);
            throw;
        }
        catch (FaultException<Proxy.ServiceFault> fault)
        {
            _logger.LogWarning("The server returned {FailureCode}. "
                + "ServerCorrelationId: {ServerCorrelationId}. CorrelationId: {CorrelationId}",
                fault.Detail.FailureCode, fault.Detail.CorrelationId, correlationId);
            throw;
        }
        catch (Exception exception) when (exception is CommunicationException or TimeoutException
            or SerializationException or InvalidOperationException)
        {
            _logger.LogWarning("The session request failed with {ExceptionType}. "
                + "CorrelationId: {CorrelationId}",
                exception.GetType().FullName, correlationId);
            throw;
        }
        finally
        {
            client.Abort();
        }
    }
}
