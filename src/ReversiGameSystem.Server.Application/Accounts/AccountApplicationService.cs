using System;
using System.Text;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>
/// Coordinates account registration, password management, and deactivation.
/// </summary>
public sealed class AccountApplicationService
{
    private const string ActiveStatusCode = "ACTIVE";
    private const int MaximumLocaleCodeLength = 35;
    private static readonly TimeSpan VerificationCodeLifetime = TimeSpan.FromMinutes(5);

    private readonly IAccountRepository _accounts;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPlayerImageRepository _images;
    private readonly VerificationCodeService _verificationCodes;
    private readonly ISessionRepository _sessions;
    private readonly ISessionLifecycleRepository _sessionLifecycle;
    private readonly IUtcClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountApplicationService"/> class.
    /// </summary>
    /// <param name="accounts">The authoritative account repository.</param>
    /// <param name="passwordHasher">The password hashing service.</param>
    /// <param name="images">The profile image repository.</param>
    /// <param name="verificationCodes">The verification code workflow.</param>
    /// <param name="sessions">The active session repository.</param>
    /// <param name="sessionLifecycle">The session revocation repository.</param>
    /// <param name="clock">The authoritative UTC clock.</param>
    /// <exception cref="ArgumentNullException">A required dependency is absent.</exception>
    public AccountApplicationService(
        IAccountRepository accounts,
        IPasswordHasher passwordHasher,
        IPlayerImageRepository images,
        VerificationCodeService verificationCodes,
        ISessionRepository sessions,
        ISessionLifecycleRepository sessionLifecycle,
        IUtcClock clock)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _passwordHasher = passwordHasher
            ?? throw new ArgumentNullException(nameof(passwordHasher));
        _images = images ?? throw new ArgumentNullException(nameof(images));
        _verificationCodes = verificationCodes
            ?? throw new ArgumentNullException(nameof(verificationCodes));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _sessionLifecycle = sessionLifecycle
            ?? throw new ArgumentNullException(nameof(sessionLifecycle));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// Validates the proposed identity and issues a registration verification code.
    /// </summary>
    /// <param name="username">The proposed public username.</param>
    /// <param name="emailAddress">The account email address.</param>
    /// <returns>The code expiration instant or a validation/availability failure.</returns>
    public AccountResult<DateTime> RequestRegistrationCode(
        string username,
        string emailAddress)
    {
        string normalizedUsername = username?.Trim() ?? string.Empty;
        if (UsernamePolicy.Validate(normalizedUsername) != UsernameValidationError.None)
        {
            return new AccountResult<DateTime>(AccountStatus.InvalidUsername);
        }

        string normalizedEmail = emailAddress?.Trim() ?? string.Empty;
        if (EmailPolicy.Validate(normalizedEmail) != EmailValidationError.None)
        {
            return new AccountResult<DateTime>(AccountStatus.InvalidEmailAddress);
        }

        AccountStatus availability = _accounts.CheckRegistrationAvailability(
            normalizedUsername,
            normalizedEmail);
        if (availability != AccountStatus.Success)
        {
            return new AccountResult<DateTime>(availability);
        }

        VerificationIdentity identity = VerificationIdentity.ForRegistration(
            normalizedUsername,
            normalizedEmail);
        DateTime expiresAtUtc = _verificationCodes.Request(identity);
        AccountResult<DateTime> result = new AccountResult<DateTime>(expiresAtUtc);
        return result;
    }

    /// <summary>
    /// Consumes a registration code and creates a new registered account.
    /// </summary>
    /// <param name="registration">The verified registration payload.</param>
    /// <returns>The created account summary or a declared failure status.</returns>
    public AccountResult<AccountSummary> CompleteRegistration(
        NewAccountRegistration? registration)
    {
        if (registration == null)
        {
            return new AccountResult<AccountSummary>(AccountStatus.MissingRegistrationData);
        }

        AccountStatus validationStatus = ValidateRegistrationInput(registration);
        if (validationStatus != AccountStatus.Success)
        {
            return new AccountResult<AccountSummary>(validationStatus);
        }

        string username = registration.Username?.Trim() ?? string.Empty;
        string emailAddress = registration.EmailAddress?.Trim() ?? string.Empty;
        string verificationCode = registration.VerificationCode?.Trim() ?? string.Empty;

        AccountStatus codeStatus = ConsumeVerificationCode(
            VerificationIdentity.ForRegistration(username, emailAddress),
            verificationCode);
        if (codeStatus != AccountStatus.Success)
        {
            return new AccountResult<AccountSummary>(codeStatus);
        }

        return SaveVerifiedAccount(registration);
    }

    /// <summary>
    /// Verifies the current password and updates the authenticated account password.
    /// </summary>
    /// <param name="sessionToken">The caller's authenticated session token.</param>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The proposed new password.</param>
    /// <returns>The expected operation status.</returns>
    public AccountStatus ChangePassword(
        string sessionToken,
        string currentPassword,
        string newPassword)
    {
        SessionOperationResult<AuthenticatedSession> sessionResult =
            _sessions.FindAuthenticated(sessionToken);
        if (sessionResult.Status != SessionOperationStatus.Success)
        {
            AccountStatus sessionFailure =
                sessionResult.Status == SessionOperationStatus.MissingSessionToken
                    ? AccountStatus.MissingSessionToken
                    : AccountStatus.InvalidSession;
            return sessionFailure;
        }

        string candidateNewPassword = newPassword ?? string.Empty;
        if (PasswordPolicy.Validate(candidateNewPassword) != PasswordValidationError.None)
        {
            return AccountStatus.InvalidPassword;
        }

        AuthenticatedSession session = sessionResult.GetValue();
        AccountResult<LoginAccount> accountResult = _accounts.FindById(session.AccountId);
        if (accountResult.Status != AccountStatus.Success
            || accountResult.Value == null
            || accountResult.Value.StatusCode != ActiveStatusCode)
        {
            return AccountStatus.InactiveAccount;
        }

        LoginAccount account = accountResult.Value;

        if (!VerifyPassword(currentPassword, account.PasswordHash))
        {
            return AccountStatus.IncorrectCurrentPassword;
        }

        string newHash = HashPassword(candidateNewPassword);
        AccountStatus updateStatus = _accounts.UpdatePasswordHash(account.AccountId, newHash);
        return updateStatus;
    }

    /// <summary>
    /// Validates the email format and issues a password reset code when an active account exists.
    /// </summary>
    /// <param name="emailAddress">The account email address.</param>
    /// <returns>
    /// A plausible code expiration instant without revealing whether the account exists.
    /// </returns>
    public AccountResult<DateTime> RequestPasswordResetCode(string emailAddress)
    {
        string normalizedEmail = emailAddress?.Trim() ?? string.Empty;
        if (EmailPolicy.Validate(normalizedEmail) != EmailValidationError.None)
        {
            return new AccountResult<DateTime>(AccountStatus.InvalidEmailAddress);
        }

        AccountResult<LoginAccount> accountResult = _accounts.FindByEmail(normalizedEmail);
        if (accountResult.Status == AccountStatus.Success
            && accountResult.Value != null
            && accountResult.Value.StatusCode == ActiveStatusCode)
        {
            LoginAccount account = accountResult.Value;
            VerificationIdentity identity = VerificationIdentity.ForPasswordReset(
                account.AccountId,
                account.EmailAddress);
            DateTime expiresAtUtc = _verificationCodes.Request(identity);
            return new AccountResult<DateTime>(expiresAtUtc);
        }

        DateTime plausibleExpiryUtc = _clock.UtcNow.Add(VerificationCodeLifetime);
        AccountResult<DateTime> hiddenResult = new AccountResult<DateTime>(plausibleExpiryUtc);
        return hiddenResult;
    }

    /// <summary>
    /// Consumes a password reset code and updates the active account password.
    /// </summary>
    /// <param name="emailAddress">The account email address.</param>
    /// <param name="verificationCode">The one-use password reset code.</param>
    /// <param name="newPassword">The proposed new password.</param>
    /// <returns>The expected operation status.</returns>
    public AccountStatus ResetPassword(
        string emailAddress,
        string verificationCode,
        string newPassword)
    {
        string normalizedEmail = emailAddress?.Trim() ?? string.Empty;
        if (EmailPolicy.Validate(normalizedEmail) != EmailValidationError.None)
        {
            return AccountStatus.InvalidEmailAddress;
        }

        string candidateNewPassword = newPassword ?? string.Empty;
        if (PasswordPolicy.Validate(candidateNewPassword) != PasswordValidationError.None)
        {
            return AccountStatus.InvalidPassword;
        }

        AccountResult<LoginAccount> accountResult = _accounts.FindByEmail(normalizedEmail);
        if (accountResult.Status != AccountStatus.Success || accountResult.Value == null)
        {
            return AccountStatus.InvalidVerificationCode;
        }

        LoginAccount account = accountResult.Value;
        if (account.StatusCode != ActiveStatusCode)
        {
            return AccountStatus.InactiveAccount;
        }

        AccountStatus codeStatus = ConsumeVerificationCode(
            VerificationIdentity.ForPasswordReset(account.AccountId, account.EmailAddress),
            verificationCode?.Trim() ?? string.Empty);
        if (codeStatus != AccountStatus.Success)
        {
            return codeStatus;
        }

        string newHash = HashPassword(candidateNewPassword);
        AccountStatus updateStatus = _accounts.UpdatePasswordHash(account.AccountId, newHash);
        if (updateStatus == AccountStatus.Success)
        {
            _sessionLifecycle.RevokeByAccountId(account.AccountId);
        }

        return updateStatus;
    }

    /// <summary>
    /// Confirms ownership and deactivates the account when no match is active.
    /// </summary>
    /// <param name="sessionToken">The caller's authenticated session token.</param>
    /// <param name="currentPassword">The current account password.</param>
    /// <returns>The expected operation status.</returns>
    public AccountStatus DeactivateAccount(string sessionToken, string currentPassword)
    {
        SessionOperationResult<AuthenticatedSession> sessionResult =
            _sessions.FindAuthenticated(sessionToken);
        if (sessionResult.Status != SessionOperationStatus.Success)
        {
            AccountStatus sessionFailure =
                sessionResult.Status == SessionOperationStatus.MissingSessionToken
                    ? AccountStatus.MissingSessionToken
                    : AccountStatus.InvalidSession;
            return sessionFailure;
        }

        AuthenticatedSession session = sessionResult.GetValue();
        AccountResult<LoginAccount> accountResult = _accounts.FindById(session.AccountId);
        if (accountResult.Status != AccountStatus.Success || accountResult.Value == null)
        {
            return AccountStatus.InactiveAccount;
        }

        LoginAccount account = accountResult.Value;
        if (account.StatusCode != ActiveStatusCode)
        {
            return AccountStatus.InactiveAccount;
        }

        if (!VerifyPassword(currentPassword, account.PasswordHash))
        {
            return AccountStatus.IncorrectCurrentPassword;
        }

        AccountStatus deactivationStatus = _accounts.DeactivateAccount(account.AccountId);
        if (deactivationStatus == AccountStatus.Success)
        {
            _sessionLifecycle.RevokeByAccountId(account.AccountId);
        }

        return deactivationStatus;
    }

    private AccountResult<AccountSummary> SaveVerifiedAccount(
        NewAccountRegistration registration)
    {
        string? imageUri = null;
        if (registration.ProfileImage != null)
        {
            ProfileResult<string> imageResult = _images.SaveImage(registration.ProfileImage);
            if (imageResult.Status != ProfileOperationStatus.Success
                || string.IsNullOrWhiteSpace(imageResult.Value))
            {
                return new AccountResult<AccountSummary>(AccountStatus.InvalidImage);
            }

            imageUri = imageResult.Value;
        }

        string username = registration.Username?.Trim() ?? string.Empty;
        string emailAddress = registration.EmailAddress?.Trim() ?? string.Empty;
        string localeCode = registration.PreferredLocaleCode?.Trim() ?? string.Empty;
        string passwordHash = HashPassword(registration.Password ?? string.Empty);

        AccountRegistrationData registrationData = new AccountRegistrationData(
            username,
            emailAddress,
            passwordHash,
            localeCode,
            imageUri,
            _clock.UtcNow);
        AccountResult<long> createResult = _accounts.CreateAccount(registrationData);
        if (createResult.Status != AccountStatus.Success)
        {
            if (imageUri != null)
            {
                _images.DeleteImage(imageUri);
            }

            return new AccountResult<AccountSummary>(createResult.Status);
        }

        AccountSummary summary = new AccountSummary(createResult.Value, username);
        AccountResult<AccountSummary> result = new AccountResult<AccountSummary>(summary);
        return result;
    }

    private static AccountStatus ValidateRegistrationInput(
        NewAccountRegistration registration)
    {
        string username = registration.Username?.Trim() ?? string.Empty;
        if (UsernamePolicy.Validate(username) != UsernameValidationError.None)
        {
            return AccountStatus.InvalidUsername;
        }

        string emailAddress = registration.EmailAddress?.Trim() ?? string.Empty;
        if (EmailPolicy.Validate(emailAddress) != EmailValidationError.None)
        {
            return AccountStatus.InvalidEmailAddress;
        }

        string password = registration.Password ?? string.Empty;
        if (PasswordPolicy.Validate(password) != PasswordValidationError.None)
        {
            return AccountStatus.InvalidPassword;
        }

        string localeCode = registration.PreferredLocaleCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(localeCode)
            || localeCode.Length > MaximumLocaleCodeLength)
        {
            return AccountStatus.InvalidLocale;
        }

        if (registration.ProfileImage != null
            && !ProfileImagePolicy.IsSupported(
                registration.ProfileImage.Content,
                registration.ProfileImage.MediaType))
        {
            return AccountStatus.InvalidImage;
        }

        return AccountStatus.Success;
    }

    private AccountStatus ConsumeVerificationCode(
        VerificationIdentity identity,
        string verificationCode)
    {
        VerificationStatus status = _verificationCodes.Consume(identity, verificationCode);
        if (status == VerificationStatus.Accepted)
        {
            return AccountStatus.Success;
        }

        if (status == VerificationStatus.Expired)
        {
            return AccountStatus.ExpiredVerificationCode;
        }

        return AccountStatus.InvalidVerificationCode;
    }

    private string HashPassword(string password)
    {
        string normalizedPassword = password.Normalize(NormalizationForm.FormC);
        return _passwordHasher.Hash(normalizedPassword);
    }

    private bool VerifyPassword(string? candidatePassword, string storedHash)
    {
        if (candidatePassword == null || candidatePassword.Length == 0)
        {
            return false;
        }

        string normalizedPassword = candidatePassword.Normalize(NormalizationForm.FormC);
        return _passwordHasher.Verify(normalizedPassword, storedHash);
    }
}
