using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using AuthenticatedSession = ReversiGameSystem.Server.Contracts.Accounts.AuthenticatedSession;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Exposes verified login and server-owned session lifetimes.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class SessionService : ISessionService
{
    private readonly ISessionHandler _handler;
    private readonly ILogger<SessionService> _logger;

    /// <summary>Initializes the session boundary.</summary>
    /// <param name="handler">The server session use cases.</param>
    /// <param name="logger">The boundary logger.</param>
    public SessionService(ISessionHandler handler, ILogger<SessionService> logger)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public DateTime RequestLoginCode(string loginIdentifier, string password) =>
        Execute(nameof(RequestLoginCode),
            () => _handler.RequestLoginCode(loginIdentifier, password));

    /// <inheritdoc />
    public AuthenticatedSession CompleteLogin(string loginIdentifier, string verificationCode) =>
        Execute(nameof(CompleteLogin),
            () => Map(_handler.CompleteLogin(loginIdentifier, verificationCode)));

    /// <inheritdoc />
    public GuestSession OpenGuestSession(string displayName) =>
        Execute(nameof(OpenGuestSession), () =>
        {
            GuestSessionRecord session = _handler.OpenGuestSession(displayName);
            return new GuestSession
            {
                SessionToken = session.SessionToken,
                DisplayName = session.DisplayName,
                ExpiresAtUtc = session.ExpiresAtUtc
            };
        });

    /// <inheritdoc />
    public void CloseSession(string sessionToken) => Execute(nameof(CloseSession), () =>
    {
        _handler.CloseSession(sessionToken);
        return true;
    });

    private TResult Execute<TResult>(string operation, Func<TResult> action)
    {
        string correlationId = CorrelationContext.GetOrCreate();
        try
        {
            return action();
        }
        catch (SessionRejectedException exception)
        {
            SessionFailure reason = exception.Reason switch
            {
                SessionRejection.InvalidCredentials => SessionFailure.InvalidCredentials,
                SessionRejection.InactiveAccount => SessionFailure.InactiveAccount,
                SessionRejection.InvalidVerificationCode => SessionFailure.InvalidVerificationCode,
                SessionRejection.ExpiredVerificationCode => SessionFailure.ExpiredVerificationCode,
                SessionRejection.InvalidGuestName => SessionFailure.InvalidGuestName,
                SessionRejection.GuestNameUnavailable => SessionFailure.GuestNameUnavailable,
                SessionRejection.MissingSessionToken => SessionFailure.MissingSessionToken,
                SessionRejection.InvalidSession => SessionFailure.InvalidSession,
                _ => throw ServiceFaultFactory.Create(operation, correlationId)
            };
            throw new FaultException<SessionFault>(
                new SessionFault { Reason = reason }, "The session request was rejected.");
        }
        catch (Exception exception)
        {
            // Exception messages from external providers may contain private request data.
            _logger.LogError(FailureDiagnostics.ForLogging(exception),
                "The {Operation} operation failed. CorrelationId: {CorrelationId}",
                operation, correlationId);
            throw ServiceFaultFactory.Create(operation, correlationId);
        }
    }

    private static AuthenticatedSession Map(AuthenticatedSessionRecord session) =>
        new AuthenticatedSession
    {
        SessionToken = session.SessionToken,
        Account = new AccountReference
        {
            AccountId = session.AccountId, Username = session.Username
        },
        EmailAddress = session.EmailAddress,
        PreferredLocaleCode = session.PreferredLocaleCode,
        ExpiresAtUtc = session.ExpiresAtUtc
    };
}
