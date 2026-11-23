using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class SessionClient : ISessionClient
{
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
            await SendSessionOperation(client =>
                client.RequestLoginCodeAsync(loginIdentifier, password));
            return SessionStatus.Success;
        }
        catch (FaultException<Proxy.SessionFault> fault)
            when (Enum.IsDefined(typeof(Proxy.SessionFailure), fault.Detail.Reason))
        {
            return MapSessionStatus(fault.Detail.Reason);
        }
        catch (Exception exception) when (RemoteCall.IsConnectionFailure(exception))
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
            Proxy.AuthenticatedSession remoteSession = await SendSessionOperation(
                client => client.CompleteLoginAsync(loginIdentifier, verificationCode));
            return new SessionResult<AuthenticatedAccountSession>(
                CreateAuthenticatedSession(remoteSession));
        }
        catch (FaultException<Proxy.SessionFault> fault)
            when (Enum.IsDefined(typeof(Proxy.SessionFailure), fault.Detail.Reason))
        {
            return new SessionResult<AuthenticatedAccountSession>(
                MapSessionStatus(fault.Detail.Reason));
        }
        catch (Exception exception) when (RemoteCall.IsConnectionFailure(exception))
        {
            return new SessionResult<AuthenticatedAccountSession>(
                SessionStatus.ServiceUnavailable);
        }
    }

    public async Task<SessionResult<GuestAccountSession>> OpenGuestSession(string displayName)
    {
        try
        {
            Proxy.GuestSession remoteSession = await SendSessionOperation(
                client => client.OpenGuestSessionAsync(displayName));
            return new SessionResult<GuestAccountSession>(CreateGuestSession(remoteSession));
        }
        catch (FaultException<Proxy.SessionFault> fault)
            when (Enum.IsDefined(typeof(Proxy.SessionFailure), fault.Detail.Reason))
        {
            return new SessionResult<GuestAccountSession>(MapSessionStatus(fault.Detail.Reason));
        }
        catch (Exception exception) when (RemoteCall.IsConnectionFailure(exception))
        {
            return new SessionResult<GuestAccountSession>(SessionStatus.ServiceUnavailable);
        }
    }

    public async Task<SessionStatus> CloseSession(string sessionToken)
    {
        try
        {
            await SendSessionOperation(client => client.CloseSessionAsync(sessionToken));
            return SessionStatus.Success;
        }
        catch (FaultException<Proxy.SessionFault> fault)
            when (Enum.IsDefined(typeof(Proxy.SessionFailure), fault.Detail.Reason))
        {
            return MapSessionStatus(fault.Detail.Reason);
        }
        catch (Exception exception) when (RemoteCall.IsConnectionFailure(exception))
        {
            return SessionStatus.ServiceUnavailable;
        }
    }

    private Task<TResult> SendSessionOperation<TResult>(
        Func<Proxy.SessionServiceClient, Task<TResult>> operation)
    {
        Proxy.SessionServiceClient client = _createClient();
        return RemoteCall.SendServiceCall(client, () => operation(client), _logger);
    }

    private Task SendSessionOperation(Func<Proxy.SessionServiceClient, Task> operation)
    {
        Proxy.SessionServiceClient client = _createClient();
        return RemoteCall.SendServiceCall(client, () => operation(client), _logger);
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

    private static SessionStatus MapSessionStatus(Proxy.SessionFailure failure)
    {
        SessionStatus status = failure switch
        {
            Proxy.SessionFailure.InvalidCredentials => SessionStatus.InvalidCredentials,
            Proxy.SessionFailure.InactiveAccount => SessionStatus.InactiveAccount,
            Proxy.SessionFailure.InvalidVerificationCode => SessionStatus.InvalidVerificationCode,
            Proxy.SessionFailure.ExpiredVerificationCode => SessionStatus.ExpiredVerificationCode,
            Proxy.SessionFailure.InvalidGuestName => SessionStatus.InvalidGuestName,
            Proxy.SessionFailure.GuestNameUnavailable => SessionStatus.GuestNameUnavailable,
            Proxy.SessionFailure.MissingSessionToken => SessionStatus.MissingSessionToken,
            Proxy.SessionFailure.InvalidSession => SessionStatus.InvalidSession,
            _ =>
                throw new SerializationException("The session service returned an unknown failure.")
        };

        return status;
    }
}
