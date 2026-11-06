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

    /// <inheritdoc />
    public DateTime RequestLoginCode(string loginIdentifier, string password)
    {
        SessionOperationResult<DateTime> result = _applicationService.RequestLoginCode(
            loginIdentifier,
            password);
        if (result.Status != SessionOperationStatus.Success)
        {
            throw MapFailure(result.Status);
        }

        return result.GetValue();
    }

    /// <inheritdoc />
    public ContractAuthenticatedSession CompleteLogin(
        string loginIdentifier,
        string verificationCode)
    {
        SessionOperationResult<ApplicationAuthenticatedSession> result =
            _applicationService.CompleteLogin(loginIdentifier, verificationCode);
        if (result.Status != SessionOperationStatus.Success)
        {
            throw MapFailure(result.Status);
        }

        return MapAuthenticatedSession(result.GetValue());
    }

    /// <inheritdoc />
    public ContractGuestSession OpenGuestSession(string displayName)
    {
        SessionOperationResult<ApplicationGuestSession> result =
            _applicationService.OpenGuestSession(displayName);
        if (result.Status != SessionOperationStatus.Success)
        {
            throw MapFailure(result.Status);
        }

        return MapGuestSession(result.GetValue());
    }

    /// <inheritdoc />
    public void CloseSession(string sessionToken)
    {
        SessionOperationStatus status = _applicationService.CloseSession(sessionToken);
        if (status != SessionOperationStatus.Success)
        {
            throw MapFailure(status);
        }

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

    private static FaultException<SessionFault> MapFailure(SessionOperationStatus status)
    {
        SessionFailure failure;
        if (status == SessionOperationStatus.InvalidCredentials)
        {
            failure = SessionFailure.InvalidCredentials;
        }
        else if (status == SessionOperationStatus.InactiveAccount)
        {
            failure = SessionFailure.InactiveAccount;
        }
        else if (status == SessionOperationStatus.InvalidVerificationCode)
        {
            failure = SessionFailure.InvalidVerificationCode;
        }
        else if (status == SessionOperationStatus.ExpiredVerificationCode)
        {
            failure = SessionFailure.ExpiredVerificationCode;
        }
        else if (status == SessionOperationStatus.InvalidGuestName)
        {
            failure = SessionFailure.InvalidGuestName;
        }
        else if (status == SessionOperationStatus.GuestNameUnavailable)
        {
            failure = SessionFailure.GuestNameUnavailable;
        }
        else if (status == SessionOperationStatus.MissingSessionToken)
        {
            failure = SessionFailure.MissingSessionToken;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        return SessionFaultFactory.Create(failure);
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
