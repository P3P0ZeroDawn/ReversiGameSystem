using System;
using System.Security.Cryptography;
using System.Text;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Application.Accounts;

public sealed class SessionApplicationService
{
    private const string ActiveStatusCode = "ACTIVE";
    private const string GuestNamePrefix = "Guest-";
    private const int MaximumLoginIdentifierLength = 254;
    private const int SessionTokenByteLength = 32;

    private readonly IAccountRepository _accounts;
    private readonly VerificationCodeService _verificationCodes;
    private readonly SessionStore _sessions;

    public SessionApplicationService(
        IAccountRepository accounts,
        VerificationCodeService verificationCodes,
        SessionStore sessions)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _verificationCodes = verificationCodes
            ?? throw new ArgumentNullException(nameof(verificationCodes));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    }

    public SessionResult<DateTime> RequestLoginCode(
        string loginIdentifier,
        string password)
    {
        SessionResult<LoginAccount> accountResult = ValidateCredentials(
            loginIdentifier,
            password);
        if (accountResult.Status != SessionStatus.Success || accountResult.Value == null)
        {
            return new SessionResult<DateTime>(accountResult.Status);
        }

        LoginAccount account = accountResult.Value;
        DateTime expiresAtUtc = _verificationCodes.Request(
            VerificationIdentity.ForLogin(account.AccountId, account.EmailAddress));
        return new SessionResult<DateTime>(expiresAtUtc);
    }

    public SessionResult<AuthenticatedSession> CompleteLogin(
        string loginIdentifier,
        string verificationCode)
    {
        SessionResult<LoginAccount> initialResult = ResolveAccount(
            loginIdentifier,
            SessionStatus.InvalidVerificationCode);
        if (initialResult.Status != SessionStatus.Success || initialResult.Value == null)
        {
            return new SessionResult<AuthenticatedSession>(initialResult.Status);
        }

        LoginAccount initialAccount = initialResult.Value;
        VerificationIdentity initialIdentity = VerificationIdentity.ForLogin(
            initialAccount.AccountId,
            initialAccount.EmailAddress);
        VerificationStatus verification = _verificationCodes.Consume(
            initialIdentity,
            verificationCode);
        if (verification != VerificationStatus.Accepted)
        {
            SessionStatus failureStatus =
                verification == VerificationStatus.Expired
                    ? SessionStatus.ExpiredVerificationCode
                    : SessionStatus.InvalidVerificationCode;
            return new SessionResult<AuthenticatedSession>(failureStatus);
        }

        return CreateSessionAfterVerification(loginIdentifier, initialIdentity);
    }

    public SessionResult<GuestSession> OpenGuestSession(string displayName)
    {
        string normalizedName = displayName?.Trim() ?? string.Empty;
        if (UsernamePolicy.Validate(normalizedName) != UsernameValidationStatus.Success)
        {
            return new SessionResult<GuestSession>(SessionStatus.InvalidGuestName);
        }

        GuestSession guestSession = new GuestSession(
            CreateToken(),
            GuestNamePrefix + normalizedName,
            _sessions.CalculateExpirationUtc());
        if (!_sessions.TryReserveGuest(guestSession))
        {
            return new SessionResult<GuestSession>(SessionStatus.GuestNameUnavailable);
        }

        return new SessionResult<GuestSession>(guestSession);
    }

    public SessionStatus CloseSession(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return SessionStatus.MissingSessionToken;
        }

        _sessions.RevokeSession(sessionToken);
        return SessionStatus.Success;
    }

    private SessionResult<LoginAccount> ValidateCredentials(
        string loginIdentifier,
        string password)
    {
        SessionResult<LoginAccount> result = ResolveAccount(
            loginIdentifier,
            SessionStatus.InvalidCredentials);
        if (result.Status != SessionStatus.Success || result.Value == null)
        {
            return result;
        }

        LoginAccount account = result.Value;
        string candidate = password ?? string.Empty;
        bool isPasswordValid = candidate.Length > 0
            && PasswordHasher.Verify(
                candidate.Normalize(NormalizationForm.FormC),
                account.PasswordHash);
        if (!isPasswordValid)
        {
            return new SessionResult<LoginAccount>(SessionStatus.InvalidCredentials);
        }

        if (!string.Equals(account.StatusCode, ActiveStatusCode, StringComparison.Ordinal))
        {
            return new SessionResult<LoginAccount>(SessionStatus.InactiveAccount);
        }

        return result;
    }

    private SessionResult<LoginAccount> ResolveAccount(
        string loginIdentifier,
        SessionStatus missingAccountStatus)
    {
        string identifier = loginIdentifier?.Trim() ?? string.Empty;
        if (identifier.Length == 0 || identifier.Length > MaximumLoginIdentifierLength)
        {
            return new SessionResult<LoginAccount>(missingAccountStatus);
        }

        AccountResult<LoginAccount> lookup = _accounts.FindByLogin(identifier);
        if (lookup.Status != AccountStatus.Success || lookup.Value == null)
        {
            return new SessionResult<LoginAccount>(missingAccountStatus);
        }

        return new SessionResult<LoginAccount>(lookup.Value);
    }

    private SessionResult<AuthenticatedSession> CreateSessionAfterVerification(
        string loginIdentifier,
        VerificationIdentity initialIdentity)
    {
        SessionResult<LoginAccount> currentResult = ResolveAccount(
            loginIdentifier,
            SessionStatus.InvalidVerificationCode);
        if (currentResult.Status != SessionStatus.Success || currentResult.Value == null)
        {
            return new SessionResult<AuthenticatedSession>(currentResult.Status);
        }

        LoginAccount currentAccount = currentResult.Value;
        VerificationIdentity currentIdentity = VerificationIdentity.ForLogin(
            currentAccount.AccountId,
            currentAccount.EmailAddress);
        bool sameIdentity = string.Equals(
            initialIdentity.Key,
            currentIdentity.Key,
            StringComparison.Ordinal);
        if (!sameIdentity)
        {
            return new SessionResult<AuthenticatedSession>(
                SessionStatus.InvalidVerificationCode);
        }

        if (!string.Equals(currentAccount.StatusCode, ActiveStatusCode, StringComparison.Ordinal))
        {
            return new SessionResult<AuthenticatedSession>(
                SessionStatus.InactiveAccount);
        }

        AuthenticatedSession session = StoreNewAuthenticatedSession(currentAccount);
        return new SessionResult<AuthenticatedSession>(session);
    }

    private AuthenticatedSession StoreNewAuthenticatedSession(LoginAccount account)
    {
        AuthenticatedSession session;
        do
        {
            session = new AuthenticatedSession(
                CreateToken(),
                account.AccountId,
                account.Username,
                account.EmailAddress,
                account.PreferredLocaleCode,
                _sessions.CalculateExpirationUtc());
        }
        while (!_sessions.TryAddAuthenticated(session));

        return session;
    }

    private static string CreateToken()
    {
        byte[] tokenBytes = new byte[SessionTokenByteLength];
        using (RandomNumberGenerator random = RandomNumberGenerator.Create())
        {
            random.GetBytes(tokenBytes);
        }

        string token = Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return token;
    }
}
