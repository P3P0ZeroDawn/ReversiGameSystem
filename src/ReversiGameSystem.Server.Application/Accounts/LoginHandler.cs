using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>
/// Represents login handler.
/// </summary>
public sealed class LoginHandler : ILoginHandler
{
    private const string ActiveStatusCode = "ACTIVE";
    private const int MaximumLoginLength = 254;
    private readonly IAccountLoginStore _accountStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthenticatedSessionStore _sessionStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginHandler"/> class.
    /// </summary>
    /// <param name="accountStore">The account store.</param>
    /// <param name="passwordHasher">The password hasher.</param>
    /// <param name="sessionStore">The session store.</param>
    public LoginHandler(
        IAccountLoginStore accountStore,
        IPasswordHasher passwordHasher,
        IAuthenticatedSessionStore sessionStore)
    {
        _accountStore = accountStore ?? throw new ArgumentNullException(nameof(accountStore));
        _passwordHasher = passwordHasher
            ?? throw new ArgumentNullException(nameof(passwordHasher));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
    }

    /// <summary>
    /// Performs the execute async operation.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<LoginResult> ExecuteAsync(LoginCommand command)
    {
        if (!TryNormalize(command, out LoginCredentials credentials))
        {
            return new LoginResult(LoginOutcome.InvalidRequest);
        }

        AccountLoginLookupResult lookup = await _accountStore.FindByLoginAsync(
            credentials.Login);
        if (lookup.Outcome == AccountLoginLookupOutcome.Unavailable)
        {
            return new LoginResult(LoginOutcome.ServiceUnavailable);
        }

        if (lookup.Outcome == AccountLoginLookupOutcome.NotFound
            || lookup.Account == null)
        {
            return new LoginResult(LoginOutcome.InvalidCredentials);
        }

        LoginAccount account = lookup.Account;
        PasswordVerificationOutcome verificationOutcome = VerifyPassword(
            credentials.Password,
            account.PasswordHash);
        if (verificationOutcome != PasswordVerificationOutcome.Valid)
        {
            LoginOutcome outcome = verificationOutcome ==
                PasswordVerificationOutcome.Invalid
                    ? LoginOutcome.InvalidCredentials
                    : LoginOutcome.ServiceUnavailable;
            return new LoginResult(outcome);
        }

        if (!string.Equals(account.StatusCode, ActiveStatusCode, StringComparison.Ordinal))
        {
            return new LoginResult(LoginOutcome.AccountInactive);
        }

        return await CreateSessionResultAsync(account);
    }

    private static bool TryNormalize(
        LoginCommand? command,
        out LoginCredentials credentials)
    {
        string login = command?.Login?.Trim() ?? string.Empty;
        string password = command?.Password ?? string.Empty;
        credentials = new LoginCredentials(login, password);
        return !string.IsNullOrWhiteSpace(login)
            && login.Length <= MaximumLoginLength
            && !string.IsNullOrEmpty(password);
    }

    private PasswordVerificationOutcome VerifyPassword(
        string password,
        string passwordHash)
    {
        try
        {
            bool isValid = _passwordHasher.Verify(
                password.Normalize(NormalizationForm.FormC),
                passwordHash);
            return isValid
                ? PasswordVerificationOutcome.Valid
                : PasswordVerificationOutcome.Invalid;
        }
        catch (ArgumentException)
        {
            return PasswordVerificationOutcome.Unavailable;
        }
        catch (FormatException)
        {
            return PasswordVerificationOutcome.Unavailable;
        }
        catch (CryptographicException)
        {
            return PasswordVerificationOutcome.Unavailable;
        }
    }

    private async Task<LoginResult> CreateSessionResultAsync(LoginAccount account)
    {
        AuthenticatedSessionRecord session = await _sessionStore.CreateAsync(
            account.AccountId,
            account.Username,
            account.EmailAddress);

        return new LoginResult(
            LoginOutcome.Success,
            new AuthenticatedSession(
                session.SessionToken,
                session.AccountId,
                session.Username,
                session.EmailAddress));
    }

    private sealed class LoginCredentials
    {
        internal LoginCredentials(string login, string password)
        {
            Login = login;
            Password = password;
        }

        internal string Login { get; }

        internal string Password { get; }
    }

    private enum PasswordVerificationOutcome
    {
        Valid,
        Invalid,
        Unavailable
    }
}
