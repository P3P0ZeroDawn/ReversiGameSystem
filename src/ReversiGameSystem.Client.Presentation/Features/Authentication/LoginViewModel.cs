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

        return _sessions.RequestLoginCode(loginIdentifier.Trim(), password);
    }

    internal Task<SessionResult<AuthenticatedAccountSession>> CompleteLogin(
        string loginIdentifier,
        string verificationCode)
    {
        return _sessions.CompleteLogin(loginIdentifier.Trim(), verificationCode.Trim());
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
