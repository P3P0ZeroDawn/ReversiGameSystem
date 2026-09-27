using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Accounts;

public interface ILoginHandler
{
    Task<LoginResult> ExecuteAsync(LoginCommand command);
}

public sealed class LoginCommand
{
    public LoginCommand(string login, string password)
    {
        Login = login;
        Password = password;
    }

    public string Login { get; }

    public string Password { get; }
}

public sealed class LoginResult
{
    public LoginResult(LoginOutcome outcome, AuthenticatedSession? session = null)
    {
        Outcome = outcome;
        Session = session;
    }

    public LoginOutcome Outcome { get; }

    public AuthenticatedSession? Session { get; }
}

public sealed class AuthenticatedSession
{
    public AuthenticatedSession(
        string sessionToken,
        long accountId,
        string username,
        string emailAddress)
    {
        SessionToken = sessionToken;
        AccountId = accountId;
        Username = username;
        EmailAddress = emailAddress;
    }

    public string SessionToken { get; }

    public long AccountId { get; }

    public string Username { get; }

    public string EmailAddress { get; }
}

public enum LoginOutcome
{
    Success,
    InvalidRequest,
    InvalidCredentials,
    AccountInactive,
    ServiceUnavailable
}
