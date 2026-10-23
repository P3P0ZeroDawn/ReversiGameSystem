using System;
using System.Text;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>Validates account credentials and resolves current account state.</summary>
public sealed class VerifiedLoginService
{
    private readonly IAccountLoginStore _accounts;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>Creates the account credential validator.</summary>
    /// <param name="accounts">The authoritative account lookup.</param>
    /// <param name="passwordHasher">The stored credential verifier.</param>
    public VerifiedLoginService(IAccountLoginStore accounts, IPasswordHasher passwordHasher)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    /// <summary>Validates credentials before checking whether an account is active.</summary>
    /// <param name="loginIdentifier">The username or email address.</param>
    /// <param name="password">The account password.</param>
    /// <returns>The validated active account.</returns>
    /// <exception cref="SessionRejectedException">
    /// Credentials are invalid or the verified account is inactive.
    /// </exception>
    public LoginAccount ValidateCredentials(string loginIdentifier, string password)
    {
        LoginAccount account = Resolve(loginIdentifier, SessionRejection.InvalidCredentials);
        if (string.IsNullOrEmpty(password)
            || !_passwordHasher.Verify(password.Normalize(NormalizationForm.FormC),
                account.PasswordHash))
        {
            throw new SessionRejectedException(SessionRejection.InvalidCredentials);
        }
        EnsureActive(account);
        return account;
    }

    /// <summary>Resolves an account without interpreting its activity state.</summary>
    /// <param name="loginIdentifier">The username or email address.</param>
    /// <param name="rejection">The rejection used when no account can be resolved.</param>
    /// <returns>The current account state.</returns>
    /// <exception cref="SessionRejectedException">The account cannot be resolved.</exception>
    /// <exception cref="InvalidOperationException">The account lookup is unavailable.</exception>
    public LoginAccount Resolve(string loginIdentifier, SessionRejection rejection)
    {
        string identifier = loginIdentifier?.Trim() ?? string.Empty;
        if (identifier.Length == 0 || identifier.Length > 254)
        {
            throw new SessionRejectedException(rejection);
        }
        AccountLoginLookupResult lookup = _accounts.FindByLoginAsync(identifier)
            .GetAwaiter().GetResult();
        if (lookup.Outcome == AccountLoginLookupOutcome.Unavailable)
        {
            throw new InvalidOperationException("Account lookup is unavailable.");
        }
        return lookup.Account ?? throw new SessionRejectedException(rejection);
    }

    /// <summary>Rejects an account that is no longer active.</summary>
    /// <param name="account">The account already verified by credentials or code.</param>
    /// <exception cref="SessionRejectedException">The account is inactive.</exception>
    public static void EnsureActive(LoginAccount account)
    {
        if (!string.Equals(account.StatusCode, "ACTIVE", StringComparison.Ordinal))
        {
            throw new SessionRejectedException(SessionRejection.InactiveAccount);
        }
    }
}
