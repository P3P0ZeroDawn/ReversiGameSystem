using System.Threading.Tasks;

namespace ReversiGameSystem.Services;

internal interface ISessionClient
{
    Task<SessionStatus> RequestLoginCode(string loginIdentifier, string password);

    Task<SessionResult<AuthenticatedAccountSession>> CompleteLogin(
        string loginIdentifier,
        string verificationCode);

    Task<SessionResult<GuestAccountSession>> OpenGuestSession(string displayName);

    Task<SessionStatus> CloseSession(string sessionToken);
}
