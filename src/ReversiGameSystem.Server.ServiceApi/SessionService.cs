using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using ApplicationAuthenticatedSession = ReversiGameSystem.Server.Application.Accounts.AuthenticatedSession;
using ApplicationGuestSession = ReversiGameSystem.Server.Application.Accounts.GuestSession;
using ContractAuthenticatedSession = ReversiGameSystem.Server.Contracts.Accounts.AuthenticatedSession;
using ContractGuestSession = ReversiGameSystem.Server.Contracts.Accounts.GuestSession;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Exposes verified login and server-owned session lifetimes.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class SessionService : ISessionService
{
    private readonly SessionApplicationService _applicationService;
    private readonly ILogger<SessionService> _logger;

    /// <summary>Notifies server observers after a session has been revoked.</summary>
    public event Action<string>? SessionClosed;

    /// <summary>Initializes the session boundary.</summary>
    /// <param name="applicationService">The session application service.</param>
    /// <param name="logger">The observer-failure logger.</param>
    /// <exception cref="ArgumentNullException">A required dependency is absent.</exception>
    public SessionService(
        SessionApplicationService applicationService,
        ILogger<SessionService> logger)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Verifies credentials and requests a one-use login verification code.
    /// </summary>
    /// <param name="loginIdentifier">The account username or email address.</param>
    /// <param name="password">The account password.</param>
    /// <returns>The UTC expiration instant of the verification code.</returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public DateTime RequestLoginCode(string loginIdentifier, string password)
    {
        SessionResult<DateTime> result = _applicationService.RequestLoginCode(
            loginIdentifier,
            password);
        ThrowFaultIfFailed(result.Status);

        return result.Value;
    }

    /// <summary>
    /// Consumes a login verification code and opens an authenticated session.
    /// </summary>
    /// <param name="loginIdentifier">The account username or email address.</param>
    /// <param name="verificationCode">The one-use verification code.</param>
    /// <returns>The newly created authenticated session.</returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public ContractAuthenticatedSession CompleteLogin(
        string loginIdentifier,
        string verificationCode)
    {
        SessionResult<ApplicationAuthenticatedSession> result =
            _applicationService.CompleteLogin(loginIdentifier, verificationCode);
        ThrowFaultIfFailed(result.Status);

        ApplicationAuthenticatedSession session = result.Value
            ?? throw new InvalidOperationException(
                "A successful login result must contain a session.");
        return MapAuthenticatedSession(session);
    }

    /// <summary>
    /// Reserves a guest display name and opens a temporary guest session.
    /// </summary>
    /// <param name="displayName">The requested guest display name.</param>
    /// <returns>The newly created guest session.</returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public ContractGuestSession OpenGuestSession(string displayName)
    {
        SessionResult<ApplicationGuestSession> result =
            _applicationService.OpenGuestSession(displayName);
        ThrowFaultIfFailed(result.Status);

        ApplicationGuestSession session = result.Value
            ?? throw new InvalidOperationException(
                "A successful guest session result must contain a session.");
        return MapGuestSession(session);
    }

    /// <summary>
    /// Revokes an active authenticated or guest session and notifies session observers.
    /// </summary>
    /// <param name="sessionToken">The opaque session token to close.</param>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public void CloseSession(string sessionToken)
    {
        SessionStatus status = _applicationService.CloseSession(sessionToken);
        ThrowFaultIfFailed(status);

        NotifySessionClosed(sessionToken);
    }

    private void NotifySessionClosed(string sessionToken)
    {
        Action<string>? handlers = SessionClosed;
        if (handlers == null)
        {
            return;
        }

        Delegate[] observers = handlers.GetInvocationList();
        for (int index = 0; index < observers.Length; index++)
        {
            Action<string> observer = (Action<string>)observers[index];
            try
            {
                observer(sessionToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception,
                    "A server observer could not process a confirmed session closure. "
                    + "CorrelationId: {CorrelationId}",
                    CorrelationContext.GetOrCreate());
            }
        }
    }

    private static void ThrowFaultIfFailed(SessionStatus status)
    {
        if (status == SessionStatus.Success)
        {
            return;
        }

        SessionFailure failure = status switch
        {
            SessionStatus.InvalidCredentials => SessionFailure.InvalidCredentials,
            SessionStatus.InactiveAccount => SessionFailure.InactiveAccount,
            SessionStatus.InvalidVerificationCode => SessionFailure.InvalidVerificationCode,
            SessionStatus.ExpiredVerificationCode => SessionFailure.ExpiredVerificationCode,
            SessionStatus.InvalidGuestName => SessionFailure.InvalidGuestName,
            SessionStatus.GuestNameUnavailable => SessionFailure.GuestNameUnavailable,
            SessionStatus.MissingSessionToken => SessionFailure.MissingSessionToken,
            SessionStatus.InvalidSession => SessionFailure.InvalidSession,
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                "Unsupported session operation status.")
        };

        throw ServiceFaultFactory.CreateSessionFault(failure);
    }

    private static ContractAuthenticatedSession MapAuthenticatedSession(
        ApplicationAuthenticatedSession session)
    {
        return new ContractAuthenticatedSession
        {
            SessionToken = session.SessionToken,
            Account = new AccountReference
            {
                AccountId = session.AccountId,
                Username = session.Username
            },
            EmailAddress = session.EmailAddress,
            PreferredLocaleCode = session.PreferredLocaleCode,
            ExpiresAtUtc = session.ExpiresAtUtc
        };
    }

    private static ContractGuestSession MapGuestSession(ApplicationGuestSession session)
    {
        return new ContractGuestSession
        {
            SessionToken = session.SessionToken,
            DisplayName = session.DisplayName,
            ExpiresAtUtc = session.ExpiresAtUtc
        };
    }
}
