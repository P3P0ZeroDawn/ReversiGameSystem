using System;
using System.Text;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Accounts;

public sealed class LoginHandler : ILoginHandler
{
    private const string ActiveStatusCode = "ACTIVE";
    private const int MaximumLoginLength = 254;
    private readonly IAccountLoginStore accountStore;
    private readonly IPasswordHasher passwordHasher;
    private readonly IAuthenticatedSessionStore sessionStore;

    public LoginHandler(
        IAccountLoginStore accountStore,
        IPasswordHasher passwordHasher,
        IAuthenticatedSessionStore sessionStore)
    {
        this.accountStore = accountStore ?? throw new ArgumentNullException(nameof(accountStore));
        this.passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        this.sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
    }

    public async Task<LoginResult> ExecuteAsync(LoginCommand command)
    {
        if (command == null)
        {
            return new LoginResult(LoginOutcome.InvalidRequest);
        }

        string login = command.Login?.Trim() ?? string.Empty;
        string password = command.Password ?? string.Empty;
        if (string.IsNullOrWhiteSpace(login)
            || login.Length > MaximumLoginLength
            || string.IsNullOrEmpty(password))
        {
            return new LoginResult(LoginOutcome.InvalidRequest);
        }

        AccountLoginLookupResult lookup;
        try
        {
            lookup = await accountStore.FindByLoginAsync(login);
        }
        catch (Exception)
        {
            return new LoginResult(LoginOutcome.ServiceUnavailable);
        }

        if (lookup.Outcome == AccountLoginLookupOutcome.Unavailable)
        {
            return new LoginResult(LoginOutcome.ServiceUnavailable);
        }

        if (lookup.Outcome == AccountLoginLookupOutcome.NotFound || lookup.Account == null)
        {
            return new LoginResult(LoginOutcome.InvalidCredentials);
        }

        bool passwordIsValid;
        try
        {
            passwordIsValid = passwordHasher.Verify(
                password.Normalize(NormalizationForm.FormC),
                lookup.Account.PasswordHash);
        }
        catch (Exception)
        {
            return new LoginResult(LoginOutcome.ServiceUnavailable);
        }

        if (!passwordIsValid)
        {
            return new LoginResult(LoginOutcome.InvalidCredentials);
        }

        if (!string.Equals(
            lookup.Account.StatusCode,
            ActiveStatusCode,
            StringComparison.Ordinal))
        {
            return new LoginResult(LoginOutcome.AccountInactive);
        }

        try
        {
            AuthenticatedSessionRecord session = await sessionStore.CreateAsync(
                lookup.Account.AccountId,
                lookup.Account.Username,
                lookup.Account.EmailAddress);

            return new LoginResult(
                LoginOutcome.Success,
                new AuthenticatedSession(
                    session.SessionToken,
                    session.AccountId,
                    session.Username,
                    session.EmailAddress));
        }
        catch (Exception)
        {
            return new LoginResult(LoginOutcome.ServiceUnavailable);
        }
    }
}
