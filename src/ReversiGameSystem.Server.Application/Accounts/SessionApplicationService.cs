using System;
using System.Text;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>Coordinates verified login, guest access, and session revocation.</summary>
public sealed class SessionApplicationService
{
    private readonly IAccountLoginRepository _accounts;
    private readonly IPasswordHasher _passwordHasher;
    private readonly VerificationCodeService _verificationCodes;
    private readonly ISessionRepository _sessions;

    /// <summary>Creates the server session application service.</summary>
    /// <param name="accounts">The authoritative account repository.</param>
    /// <param name="passwordHasher">The stored credential verifier.</param>
    /// <param name="verificationCodes">The durable verification workflow.</param>
    /// <param name="sessions">The shared active-session repository.</param>
    /// <exception cref="ArgumentNullException">A required dependency is absent.</exception>
    public SessionApplicationService(
        IAccountLoginRepository accounts,
        IPasswordHasher passwordHasher,
        VerificationCodeService verificationCodes,
        ISessionRepository sessions)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _passwordHasher = passwordHasher
            ?? throw new ArgumentNullException(nameof(passwordHasher));
        _verificationCodes = verificationCodes
            ?? throw new ArgumentNullException(nameof(verificationCodes));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    }

    /// <summary>Validates credentials and sends a single-use login code.</summary>
    /// <param name="loginIdentifier">The account username or email address.</param>
    /// <param name="password">The account password.</param>
    /// <returns>The expected request result.</returns>
    public SessionOperationResult<DateTime> RequestLoginCode(
        string loginIdentifier,
        string password)
    {
        SessionOperationResult<LoginAccount> accountResult = ValidateCredentials(
            loginIdentifier,
            password);
        if (accountResult.Status != SessionOperationStatus.Success)
        {
            return SessionOperationResult<DateTime>.CreateFailure(accountResult.Status);
        }

        LoginAccount account = accountResult.GetValue();
        DateTime expiresAtUtc = _verificationCodes.Request(
            VerificationIdentity.ForLogin(account.AccountId, account.EmailAddress));
        return SessionOperationResult<DateTime>.CreateSuccess(expiresAtUtc);
    }

    /// <summary>Consumes a pending code and creates one authenticated session.</summary>
    /// <param name="loginIdentifier">The account username or email address.</param>
    /// <param name="verificationCode">The code delivered to the account owner.</param>
    /// <returns>The expected completion result.</returns>
    public SessionOperationResult<AuthenticatedSession> CompleteLogin(
        string loginIdentifier,
        string verificationCode)
    {
        SessionOperationResult<LoginAccount> initialResult = ResolveAccount(
            loginIdentifier,
            SessionOperationStatus.InvalidVerificationCode);
        if (initialResult.Status != SessionOperationStatus.Success)
        {
            return SessionOperationResult<AuthenticatedSession>.CreateFailure(
                initialResult.Status);
        }

        LoginAccount initialAccount = initialResult.GetValue();
        VerificationIdentity initialIdentity = VerificationIdentity.ForLogin(
            initialAccount.AccountId,
            initialAccount.EmailAddress);
        VerificationStatus verification = _verificationCodes.Consume(
            initialIdentity,
            verificationCode);
        if (verification != VerificationStatus.Accepted)
        {
            SessionOperationStatus status = GetVerificationStatus(verification);
            return SessionOperationResult<AuthenticatedSession>.CreateFailure(status);
        }

        return CreateSessionAfterVerification(loginIdentifier, initialIdentity);
    }

    /// <summary>Validates and reserves one temporary guest identity.</summary>
    /// <param name="displayName">The requested unprefixed guest name.</param>
    /// <returns>The expected guest-session result.</returns>
    public SessionOperationResult<GuestSession> OpenGuestSession(string displayName)
    {
        string name = displayName?.Trim() ?? string.Empty;
        if (UsernamePolicy.Validate(name) != UsernameValidationError.None)
        {
            return SessionOperationResult<GuestSession>.CreateFailure(
                SessionOperationStatus.InvalidGuestName);
        }

        return _sessions.CreateGuest("Guest-" + name);
    }

    /// <summary>Revokes an authenticated or guest session idempotently.</summary>
    /// <param name="sessionToken">The opaque session token.</param>
    /// <returns>The expected closure status.</returns>
    public SessionOperationStatus CloseSession(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return SessionOperationStatus.MissingSessionToken;
        }

        _sessions.Revoke(sessionToken);
        return SessionOperationStatus.Success;
    }

    private SessionOperationResult<LoginAccount> ValidateCredentials(
        string loginIdentifier,
        string password)
    {
        SessionOperationResult<LoginAccount> result = ResolveAccount(
            loginIdentifier,
            SessionOperationStatus.InvalidCredentials);
        if (result.Status != SessionOperationStatus.Success)
        {
            return result;
        }

        LoginAccount account = result.GetValue();
        string candidate = password ?? string.Empty;
        if (candidate.Length == 0
            || !_passwordHasher.Verify(
                candidate.Normalize(NormalizationForm.FormC),
                account.PasswordHash))
        {
            return SessionOperationResult<LoginAccount>.CreateFailure(
                SessionOperationStatus.InvalidCredentials);
        }
        if (!IsActive(account))
        {
            return SessionOperationResult<LoginAccount>.CreateFailure(
                SessionOperationStatus.InactiveAccount);
        }

        return result;
    }

    private SessionOperationResult<LoginAccount> ResolveAccount(
        string loginIdentifier,
        SessionOperationStatus missingAccountStatus)
    {
        string identifier = loginIdentifier?.Trim() ?? string.Empty;
        if (identifier.Length == 0 || identifier.Length > 254)
        {
            return SessionOperationResult<LoginAccount>.CreateFailure(missingAccountStatus);
        }

        AccountLoginLookupResult lookup = _accounts.FindByLoginAsync(identifier)
            .GetAwaiter().GetResult();
        if (!lookup.IsFound)
        {
            return SessionOperationResult<LoginAccount>.CreateFailure(missingAccountStatus);
        }

        return SessionOperationResult<LoginAccount>.CreateSuccess(lookup.GetFoundAccount());
    }

    private SessionOperationResult<AuthenticatedSession> CreateSessionAfterVerification(
        string loginIdentifier,
        VerificationIdentity initialIdentity)
    {
        SessionOperationResult<LoginAccount> currentResult = ResolveAccount(
            loginIdentifier,
            SessionOperationStatus.InvalidVerificationCode);
        if (currentResult.Status != SessionOperationStatus.Success)
        {
            return SessionOperationResult<AuthenticatedSession>.CreateFailure(
                currentResult.Status);
        }

        LoginAccount currentAccount = currentResult.GetValue();
        VerificationIdentity currentIdentity = VerificationIdentity.ForLogin(
            currentAccount.AccountId,
            currentAccount.EmailAddress);
        if (!string.Equals(
            initialIdentity.Key,
            currentIdentity.Key,
            StringComparison.Ordinal))
        {
            return SessionOperationResult<AuthenticatedSession>.CreateFailure(
                SessionOperationStatus.InvalidVerificationCode);
        }
        if (!IsActive(currentAccount))
        {
            return SessionOperationResult<AuthenticatedSession>.CreateFailure(
                SessionOperationStatus.InactiveAccount);
        }

        AuthenticatedSession session = _sessions.Create(currentAccount);
        return SessionOperationResult<AuthenticatedSession>.CreateSuccess(session);
    }

    private static bool IsActive(LoginAccount account)
    {
        return string.Equals(account.StatusCode, "ACTIVE", StringComparison.Ordinal);
    }

    private static SessionOperationStatus GetVerificationStatus(
        VerificationStatus verification)
    {
        SessionOperationStatus status = SessionOperationStatus.InvalidVerificationCode;
        if (verification == VerificationStatus.Expired)
        {
            status = SessionOperationStatus.ExpiredVerificationCode;
        }

        return status;
    }
}
