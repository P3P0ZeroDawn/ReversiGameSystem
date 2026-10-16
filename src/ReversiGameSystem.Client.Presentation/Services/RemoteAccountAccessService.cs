using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class RemoteAccountAccessService
{
    private const string EndpointConfigurationName = "AccountAccessEndpoint";
    private const string CorrelationHeaderName = "CorrelationId";
    private const string DiagnosticsNamespace = "urn:reversi:diagnostics:v1";
    private readonly ILogger<RemoteAccountAccessService> _logger;

    internal RemoteAccountAccessService(ILogger<RemoteAccountAccessService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal async Task<AuthenticationOutcome> LoginAsync(
        string loginIdentifier,
        string password)
    {
        string correlationId = Guid.NewGuid().ToString("D");
        Proxy.AccountAccessServiceClient? client = null;
        try
        {
            client = new Proxy.AccountAccessServiceClient(EndpointConfigurationName);
            Proxy.AuthenticationCredentials credentials =
                new Proxy.AuthenticationCredentials
                {
                    LoginIdentifier = loginIdentifier,
                    Password = password
                };
            Task<Proxy.AuthenticationOutcome> operation = StartLogin(
                client,
                credentials,
                correlationId);
            Proxy.AuthenticationOutcome remoteOutcome = await operation;
            AuthenticationOutcome outcome = AuthenticationOutcomeMapper.Map(remoteOutcome);
            Close(client);
            client = null;
            return outcome;
        }
        catch (FaultException<Proxy.ServiceFault> fault)
        {
            _logger.LogWarning(
                "The server returned fault {FailureCode}. CorrelationId: {CorrelationId}",
                fault.Detail.FailureCode,
                fault.Detail.CorrelationId);
            return Unavailable();
        }
        catch (Exception exception) when (exception is CommunicationException
                                            or TimeoutException)
        {
            _logger.LogWarning(
                exception,
                "The account service could not be reached. CorrelationId: {CorrelationId}",
                correlationId);
            return Unavailable();
        }
        catch (Exception exception) when (exception is SerializationException
                                            or InvalidOperationException)
        {
            _logger.LogError(
                exception,
                "The account service response was invalid. CorrelationId: {CorrelationId}",
                correlationId);
            return Unavailable();
        }
        finally
        {
            client?.Abort();
        }
    }

    private static Task<Proxy.AuthenticationOutcome> StartLogin(
        Proxy.AccountAccessServiceClient client,
        Proxy.AuthenticationCredentials credentials,
        string correlationId)
    {
        using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
        {
            MessageHeader header = MessageHeader.CreateHeader(
                CorrelationHeaderName,
                DiagnosticsNamespace,
                correlationId);
            OperationContext.Current.OutgoingMessageHeaders.Add(header);
            return client.LoginAsync(credentials);
        }
    }

    private static void Close(Proxy.AccountAccessServiceClient client)
    {
        if (client.State == CommunicationState.Faulted)
        {
            client.Abort();
            return;
        }

        client.Close();
    }

    private static AuthenticationOutcome Unavailable()
    {
        return new AuthenticationOutcome(AuthenticationStatus.ServiceUnavailable);
    }
}
