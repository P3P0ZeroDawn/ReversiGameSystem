using System;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>Creates authenticated sessions only after consuming an authorized code.</summary>
public sealed class SessionHandler : ISessionHandler
{
    private readonly VerifiedLoginService _loginService;
    private readonly VerificationCodeService _verificationCodes;
    private readonly SessionRegistry _sessions;

    /// <summary>Creates the server session use case.</summary>
    /// <param name="loginService">The authoritative account validation service.</param>
    /// <param name="verificationCodes">The durable verification workflow.</param>
    /// <param name="sessions">The active session registry.</param>
    public SessionHandler(VerifiedLoginService loginService,
        VerificationCodeService verificationCodes, SessionRegistry sessions)
    {
        _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
        _verificationCodes = verificationCodes
            ?? throw new ArgumentNullException(nameof(verificationCodes));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    }

    /// <inheritdoc />
    public DateTime RequestLoginCode(string loginIdentifier, string password)
    {
        LoginAccount account = _loginService.ValidateCredentials(loginIdentifier, password);
        return _verificationCodes.Request(
            VerificationIdentity.ForLogin(account.AccountId, account.EmailAddress));
    }

    /// <inheritdoc />
    public AuthenticatedSessionRecord CompleteLogin(string loginIdentifier, string verificationCode)
    {
        LoginAccount account = _loginService.Resolve(
            loginIdentifier, SessionRejection.InvalidVerificationCode);
        VerificationIdentity identity = VerificationIdentity.ForLogin(
            account.AccountId, account.EmailAddress);
        VerificationResult result = _verificationCodes.Consume(identity, verificationCode);
        if (result != VerificationResult.Accepted)
        {
            throw new SessionRejectedException(result == VerificationResult.Expired
                ? SessionRejection.ExpiredVerificationCode
                : SessionRejection.InvalidVerificationCode);
        }
        // Re-read the account after consumption so a disabled account cannot open a session.
        LoginAccount current = _loginService.Resolve(
            loginIdentifier, SessionRejection.InvalidVerificationCode);
        VerificationIdentity currentIdentity = VerificationIdentity.ForLogin(
            current.AccountId, current.EmailAddress);
        if (!string.Equals(identity.Key, currentIdentity.Key, StringComparison.Ordinal))
        {
            throw new SessionRejectedException(SessionRejection.InvalidVerificationCode);
        }
        VerifiedLoginService.EnsureActive(current);
        return _sessions.CreateAuthenticated(current);
    }

    /// <inheritdoc />
    public GuestSessionRecord OpenGuestSession(string displayName)
    {
        return _sessions.CreateGuest(displayName);
    }

    /// <inheritdoc />
    public void CloseSession(string sessionToken) => _sessions.Close(sessionToken);
}
