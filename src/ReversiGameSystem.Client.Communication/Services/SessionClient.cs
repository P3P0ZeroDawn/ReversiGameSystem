using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class SessionClient : ISessionClient
{
    private const string CorrelationHeaderName = "CorrelationId";
    private const string CorrelationHeaderNamespace = "urn:reversi:diagnostics:v1";

    private readonly ILogger<SessionClient> _logger;
    private readonly Func<Proxy.SessionServiceClient> _createClient;

    internal SessionClient(ILogger<SessionClient> logger)
        : this(logger, ServerConnectionFactory.CreateSessionClient)
    {
    }

    internal SessionClient(
        ILogger<SessionClient> logger,
        Func<Proxy.SessionServiceClient> createClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
    }

    public async Task<SessionStatus> RequestLoginCode(
        string loginIdentifier,
        string password)
    {
        try
        {
            await Execute(client => client.RequestLoginCodeAsync(loginIdentifier, password));
            return SessionStatus.Success;
        }
        catch (FaultException<Proxy.SessionFault> fault)
        {
            return MapStatus(fault.Detail.Reason);
        }
        catch (Exception exception) when (IsCommunicationFailure(exception))
        {
            return SessionStatus.ServiceUnavailable;
        }
    }

    public async Task<SessionResult<AuthenticatedAccountSession>> CompleteLogin(
        string loginIdentifier,
        string verificationCode)
    {
        try
        {
            Proxy.AuthenticatedSession remoteSession = await Execute(
                client => client.CompleteLoginAsync(loginIdentifier, verificationCode));
            return new SessionResult<AuthenticatedAccountSession>(
                CreateAuthenticatedSession(remoteSession));
        }
        catch (FaultException<Proxy.SessionFault> fault)
        {
            return new SessionResult<AuthenticatedAccountSession>(
                MapStatus(fault.Detail.Reason));
        }
        catch (Exception exception) when (IsCommunicationFailure(exception))
        {
            return new SessionResult<AuthenticatedAccountSession>(
                SessionStatus.ServiceUnavailable);
        }
    }

    public async Task<SessionResult<GuestAccountSession>> OpenGuestSession(string displayName)
    {
        try
        {
            Proxy.GuestSession remoteSession = await Execute(
                client => client.OpenGuestSessionAsync(displayName));
            return new SessionResult<GuestAccountSession>(CreateGuestSession(remoteSession));
        }
        catch (FaultException<Proxy.SessionFault> fault)
        {
            return new SessionResult<GuestAccountSession>(MapStatus(fault.Detail.Reason));
        }
        catch (Exception exception) when (IsCommunicationFailure(exception))
        {
            return new SessionResult<GuestAccountSession>(SessionStatus.ServiceUnavailable);
        }
    }

    public async Task<SessionStatus> CloseSession(string sessionToken)
    {
        try
        {
            await Execute(async client =>
            {
                await client.CloseSessionAsync(sessionToken);
                return true;
            });
            return SessionStatus.Success;
        }
        catch (FaultException<Proxy.SessionFault> fault)
        {
            return MapStatus(fault.Detail.Reason);
        }
        catch (Exception exception) when (IsCommunicationFailure(exception))
        {
            return SessionStatus.ServiceUnavailable;
        }
    }

    private async Task<TResult> Execute<TResult>(
        Func<Proxy.SessionServiceClient, Task<TResult>> operation)
    {
        Proxy.SessionServiceClient client = _createClient();
        string correlationId = Guid.NewGuid().ToString("D");
        try
        {
            TResult result = await Invoke(client, operation, correlationId);
            CloseAcknowledgedChannel(client, correlationId);
            return result;
        }
        catch (FaultException<Proxy.SessionFault> fault)
        {
            _logger.LogInformation(
                "The session request was rejected with {Reason}. CorrelationId: {CorrelationId}",
                fault.Detail.Reason,
                correlationId);
            throw;
        }
        catch (FaultException<Proxy.ServiceFault> fault)
        {
            _logger.LogWarning(
                "The server returned {FailureCode}. ServerCorrelationId: "
                    + "{ServerCorrelationId}. CorrelationId: {CorrelationId}",
                fault.Detail.FailureCode,
                fault.Detail.CorrelationId,
                correlationId);
            throw;
        }
        catch (Exception exception) when (IsCommunicationFailure(exception))
        {
            _logger.LogWarning(
                FailureDiagnostics.ForLogging(exception),
                "The session request failed. CorrelationId: {CorrelationId}",
                correlationId);
            throw;
        }
        finally
        {
            client.Abort();
        }
    }

    private static Task<TResult> Invoke<TResult>(
        Proxy.SessionServiceClient client,
        Func<Proxy.SessionServiceClient, Task<TResult>> operation,
        string correlationId)
    {
        using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
        {
            OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader(
                CorrelationHeaderName,
                CorrelationHeaderNamespace,
                correlationId));
            return operation(client);
        }
    }

    private static AuthenticatedAccountSession CreateAuthenticatedSession(
        Proxy.AuthenticatedSession remoteSession)
    {
        if (remoteSession?.Account == null
            || remoteSession.Account.AccountId <= 0
            || string.IsNullOrWhiteSpace(remoteSession.SessionToken)
            || string.IsNullOrWhiteSpace(remoteSession.Account.Username)
            || string.IsNullOrWhiteSpace(remoteSession.EmailAddress)
            || remoteSession.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new SerializationException("The session service returned an invalid session.");
        }

        return new AuthenticatedAccountSession(
            remoteSession.SessionToken,
            remoteSession.Account.AccountId,
            remoteSession.Account.Username,
            remoteSession.EmailAddress,
            remoteSession.PreferredLocaleCode ?? string.Empty,
            remoteSession.ExpiresAtUtc);
    }

    private static GuestAccountSession CreateGuestSession(Proxy.GuestSession remoteSession)
    {
        if (remoteSession == null
            || string.IsNullOrWhiteSpace(remoteSession.SessionToken)
            || string.IsNullOrWhiteSpace(remoteSession.DisplayName)
            || remoteSession.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new SerializationException(
                "The session service returned an invalid guest session.");
        }

        return new GuestAccountSession(
            remoteSession.SessionToken,
            remoteSession.DisplayName,
            remoteSession.ExpiresAtUtc);
    }

    private static SessionStatus MapStatus(Proxy.SessionFailure failure)
    {
        SessionStatus status;
        switch (failure)
        {
            case Proxy.SessionFailure.InvalidCredentials:
                status = SessionStatus.InvalidCredentials;
                break;
            case Proxy.SessionFailure.InactiveAccount:
                status = SessionStatus.InactiveAccount;
                break;
            case Proxy.SessionFailure.InvalidVerificationCode:
                status = SessionStatus.InvalidVerificationCode;
                break;
            case Proxy.SessionFailure.ExpiredVerificationCode:
                status = SessionStatus.ExpiredVerificationCode;
                break;
            case Proxy.SessionFailure.InvalidGuestName:
                status = SessionStatus.InvalidGuestName;
                break;
            case Proxy.SessionFailure.GuestNameUnavailable:
                status = SessionStatus.GuestNameUnavailable;
                break;
            case Proxy.SessionFailure.MissingSessionToken:
                status = SessionStatus.MissingSessionToken;
                break;
            case Proxy.SessionFailure.InvalidSession:
                status = SessionStatus.InvalidSession;
                break;
            default:
                throw new SerializationException("The session service returned an unknown failure.");
        }

        return status;
    }

    private static bool IsCommunicationFailure(Exception exception)
    {
        return exception is CommunicationException
            || exception is TimeoutException
            || exception is SerializationException
            || exception is InvalidOperationException;
    }

    private void CloseAcknowledgedChannel(
        Proxy.SessionServiceClient client,
        string correlationId)
    {
        try
        {
            client.Close();
        }
        catch (Exception exception) when (
            exception is CommunicationException || exception is TimeoutException)
        {
            _logger.LogWarning(
                FailureDiagnostics.ForLogging(exception),
                "The acknowledged session channel could not close. "
                    + "CorrelationId: {CorrelationId}",
                correlationId);
        }
    }
}
