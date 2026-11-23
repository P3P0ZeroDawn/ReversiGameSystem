using System;
using System.Text;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Application.Accounts;

public sealed class AccountApplicationService
{
    private const string ActiveStatusCode = "ACTIVE";
    private const int MaximumLocaleCodeLength = 35;

    private readonly IAccountRepository _accounts;
    private readonly IPlayerImageRepository _images;
    private readonly VerificationCodeService _verificationCodes;
    private readonly SessionStore _sessions;

    public AccountApplicationService(
        IAccountRepository accounts,
        IPlayerImageRepository images,
        VerificationCodeService verificationCodes,
        SessionStore sessions)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _images = images ?? throw new ArgumentNullException(nameof(images));
        _verificationCodes = verificationCodes
            ?? throw new ArgumentNullException(nameof(verificationCodes));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    }

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

    public AccountStatus ChangePassword(
        string sessionToken,
        string currentPassword,
        string newPassword)
    {
        SessionResult<AuthenticatedSession> sessionResult =
            _sessions.FindAuthenticated(sessionToken);
        if (sessionResult.Status != SessionStatus.Success || sessionResult.Value == null)
        {
            AccountStatus sessionFailure =
                sessionResult.Status == SessionStatus.MissingSessionToken
                    ? AccountStatus.MissingSessionToken
                    : AccountStatus.InvalidSession;
            return sessionFailure;
        }

        string candidateNewPassword = newPassword ?? string.Empty;
        if (PasswordPolicy.Validate(candidateNewPassword) != PasswordValidationError.None)
        {
            return AccountStatus.InvalidPassword;
        }

        AuthenticatedSession session = sessionResult.Value;
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

        DateTime plausibleExpiryUtc = _verificationCodes.CalculatePlausibleExpiry();
        AccountResult<DateTime> hiddenResult = new AccountResult<DateTime>(plausibleExpiryUtc);
        return hiddenResult;
    }

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
            _sessions.RevokeAccountSessions(account.AccountId);
        }

        return updateStatus;
    }

    public AccountStatus DeactivateAccount(string sessionToken, string currentPassword)
    {
        SessionResult<AuthenticatedSession> sessionResult =
            _sessions.FindAuthenticated(sessionToken);
        if (sessionResult.Status != SessionStatus.Success || sessionResult.Value == null)
        {
            AccountStatus sessionFailure =
                sessionResult.Status == SessionStatus.MissingSessionToken
                    ? AccountStatus.MissingSessionToken
                    : AccountStatus.InvalidSession;
            return sessionFailure;
        }

        AuthenticatedSession session = sessionResult.Value;
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

        AccountStatus deactivationStatus = _accounts.DeactivateAccount(account.AccountId);
        if (deactivationStatus == AccountStatus.Success)
        {
            _sessions.RevokeAccountSessions(account.AccountId);
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
            if (imageResult.Status != ProfileStatus.Success
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
            imageUri);
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

    private static string HashPassword(string password)
    {
        string normalizedPassword = password.Normalize(NormalizationForm.FormC);
        return PasswordHasher.Hash(normalizedPassword);
    }

    private static bool VerifyPassword(string? candidatePassword, string storedHash)
    {
        if (candidatePassword == null || candidatePassword.Length == 0)
        {
            return false;
        }

        string normalizedPassword = candidatePassword.Normalize(NormalizationForm.FormC);
        return PasswordHasher.Verify(normalizedPassword, storedHash);
    }
}
