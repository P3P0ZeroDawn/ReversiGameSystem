using System;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>
/// Defines the login use-case contract.
/// </summary>
public interface ILoginHandler
{
    /// <summary>
    /// Performs the execute async operation.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The result of the operation.</returns>
    Task<LoginResult> ExecuteAsync(LoginCommand command);
}

/// <summary>
/// Represents login command.
/// </summary>
public sealed class LoginCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginCommand"/> class.
    /// </summary>
    /// <param name="login">The login.</param>
    /// <param name="password">The password.</param>
    public LoginCommand(string login, string password)
    {
        Login = login;
        Password = password;
    }

    /// <summary>
    /// Gets the login.
    /// </summary>
    public string Login { get; }

    /// <summary>
    /// Gets the password.
    /// </summary>
    public string Password { get; }
}

/// <summary>
/// Represents login result.
/// </summary>
public sealed class LoginResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginResult"/> class.
    /// </summary>
    /// <param name="outcome">The outcome.</param>
    /// <param name="session">The session.</param>
    public LoginResult(LoginOutcome outcome, AuthenticatedSession? session = null)
    {
        Outcome = outcome;
        Session = session;
    }

    /// <summary>
    /// Gets the outcome.
    /// </summary>
    public LoginOutcome Outcome { get; }

    /// <summary>
    /// Gets the session.
    /// </summary>
    public AuthenticatedSession? Session { get; }
}

/// <summary>
/// Represents authenticated session.
/// </summary>
public sealed class AuthenticatedSession
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticatedSession"/> class.
    /// </summary>
    /// <param name="sessionToken">The session token.</param>
    /// <param name="accountId">The account id.</param>
    /// <param name="username">The username.</param>
    /// <param name="emailAddress">The email address.</param>
    /// <param name="preferredLocaleCode">The preferred locale code.</param>
    /// <param name="expiresAtUtc">The UTC expiration time.</param>
    public AuthenticatedSession(
        string sessionToken,
        long accountId,
        string username,
        string emailAddress,
        string preferredLocaleCode,
        DateTime expiresAtUtc)
    {
        SessionToken = sessionToken;
        AccountId = accountId;
        Username = username;
        EmailAddress = emailAddress;
        PreferredLocaleCode = preferredLocaleCode;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>
    /// Gets the session token.
    /// </summary>
    public string SessionToken { get; }

    /// <summary>
    /// Gets the account id.
    /// </summary>
    public long AccountId { get; }

    /// <summary>
    /// Gets the username.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Gets the email address.
    /// </summary>
    public string EmailAddress { get; }

    /// <summary>
    /// Gets the preferred locale code.
    /// </summary>
    public string PreferredLocaleCode { get; }

    /// <summary>
    /// Gets the UTC expiration time.
    /// </summary>
    public DateTime ExpiresAtUtc { get; }
}

/// <summary>
/// Defines the supported login outcome values.
/// </summary>
public enum LoginOutcome
{
    /// <summary>
    /// Represents success.
    /// </summary>
    Success,
    /// <summary>
    /// Represents invalid request.
    /// </summary>
    InvalidRequest,
    /// <summary>
    /// Represents invalid credentials.
    /// </summary>
    InvalidCredentials,
    /// <summary>
    /// Represents account inactive.
    /// </summary>
    AccountInactive,
    /// <summary>
    /// Represents service unavailable.
    /// </summary>
    ServiceUnavailable
}
