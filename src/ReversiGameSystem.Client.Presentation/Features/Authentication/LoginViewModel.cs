using System;
using System.Threading.Tasks;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Presentation.Authentication;

internal sealed class LoginViewModel
{
    private readonly ISessionClient _sessions;

    internal LoginViewModel(ISessionClient sessions)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    }

    internal Task<SessionStatus> RequestLoginCode(string loginIdentifier, string password)
    {
        if (string.IsNullOrWhiteSpace(loginIdentifier)
            || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(SessionStatus.MissingCredentials);
        }

        string identifier = loginIdentifier.Trim();
        if (UsernamePolicy.Validate(identifier) != UsernameValidationError.None
            && EmailPolicy.Validate(identifier) != EmailValidationError.None)
        {
            return Task.FromResult(SessionStatus.InvalidCredentials);
        }
        return _sessions.RequestLoginCode(identifier, password);
    }

    internal Task<SessionResult<AuthenticatedAccountSession>> CompleteLogin(
        string loginIdentifier,
        string verificationCode)
    {
        string code = verificationCode.Trim();
        if (!VerificationCodePolicy.IsValid(code))
        {
            return Task.FromResult(new SessionResult<AuthenticatedAccountSession>(
                SessionStatus.InvalidVerificationCode));
        }
        return _sessions.CompleteLogin(loginIdentifier.Trim(), code);
    }

    internal Task<SessionResult<GuestAccountSession>> OpenGuestSession(string displayName)
    {
        string normalizedName = GuestUsernamePolicy.Normalize(displayName);
        if (GuestUsernamePolicy.Validate(normalizedName) != UsernameValidationError.None)
        {
            return Task.FromResult(
                new SessionResult<GuestAccountSession>(SessionStatus.InvalidGuestName));
        }

        return _sessions.OpenGuestSession(normalizedName);
    }
}
