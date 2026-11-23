using System;
using System.ServiceModel;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>
/// Exposes account registration, password management, and deactivation through WCF.
/// </summary>
[ServiceBehavior(
    InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple,
    UseSynchronizationContext = false)]
public sealed class AccountService : IAccountService
{
    private readonly AccountApplicationService _applicationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountService"/> class.
    /// </summary>
    /// <param name="applicationService">The account application service.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="applicationService"/> is null.
    /// </exception>
    public AccountService(AccountApplicationService applicationService)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
    }

    /// <summary>
    /// Validates the proposed identity and requests a one-use registration code.
    /// </summary>
    /// <param name="username">The proposed public account username.</param>
    /// <param name="emailAddress">The account email address for registration.</param>
    /// <returns>The UTC expiration of the code accepted for delivery.</returns>
    /// <exception cref="FaultException{AccountFault}">
    /// A declared account rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public DateTime RequestRegistrationCode(string username, string emailAddress)
    {
        AccountResult<DateTime> result = _applicationService.RequestRegistrationCode(
            username,
            emailAddress);
        if (result.Status != AccountStatus.Success)
        {
            throw CreateFault(result.Status);
        }

        return result.Value;
    }

    /// <summary>
    /// Consumes a valid registration code and saves the new account without opening a session.
    /// </summary>
    /// <param name="account">
    /// The complete verified registration data and optional profile image.
    /// </param>
    /// <returns>The public identity of the account whose registration was saved.</returns>
    /// <exception cref="FaultException{AccountFault}">
    /// A declared account rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public AccountReference CompleteRegistration(NewAccount account)
    {
        NewAccountRegistration? registration = MapRegistration(account);
        AccountResult<AccountSummary> result =
            _applicationService.CompleteRegistration(registration);
        if (result.Status != AccountStatus.Success || result.Value == null)
        {
            throw CreateFault(result.Status);
        }

        AccountReference reference = MapAccountReference(result.Value);
        return reference;
    }

    /// <summary>
    /// Verifies the current password and changes the authenticated account password.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="currentPassword">
    /// The current account password used to confirm ownership.
    /// </param>
    /// <param name="newPassword">
    /// The proposed new password, subject to the server password policy.
    /// </param>
    /// <exception cref="FaultException{AccountFault}">
    /// A declared account rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public void ChangePassword(string sessionToken, string currentPassword, string newPassword)
    {
        AccountStatus status = _applicationService.ChangePassword(
            sessionToken,
            currentPassword,
            newPassword);
        if (status != AccountStatus.Success)
        {
            throw CreateFault(status);
        }
    }

    /// <summary>
    /// Validates the email format and requests a one-use password reset code.
    /// </summary>
    /// <param name="emailAddress">The account email address for recovery.</param>
    /// <returns>
    /// A plausible UTC expiration instant without revealing whether an active account exists.
    /// </returns>
    /// <exception cref="FaultException{AccountFault}">
    /// A declared account rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public DateTime RequestPasswordResetCode(string emailAddress)
    {
        AccountResult<DateTime> result =
            _applicationService.RequestPasswordResetCode(emailAddress);
        if (result.Status != AccountStatus.Success)
        {
            throw CreateFault(result.Status);
        }

        return result.Value;
    }

    /// <summary>
    /// Verifies a recovery code and changes the active account password.
    /// </summary>
    /// <param name="emailAddress">The account email address for recovery.</param>
    /// <param name="verificationCode">
    /// The one-use code for the password recovery process.
    /// </param>
    /// <param name="newPassword">
    /// The proposed new password, subject to the server password policy.
    /// </param>
    /// <exception cref="FaultException{AccountFault}">
    /// A declared account rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public void ResetPassword(string emailAddress, string verificationCode, string newPassword)
    {
        AccountStatus status = _applicationService.ResetPassword(
            emailAddress,
            verificationCode,
            newPassword);
        if (status != AccountStatus.Success)
        {
            throw CreateFault(status);
        }
    }

    /// <summary>
    /// Confirms account ownership and deactivates the account while preserving its history.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="currentPassword">
    /// The current account password used to confirm ownership.
    /// </param>
    /// <exception cref="FaultException{AccountFault}">
    /// A declared account rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public void DeactivateAccount(string sessionToken, string currentPassword)
    {
        AccountStatus status = _applicationService.DeactivateAccount(
            sessionToken,
            currentPassword);
        if (status != AccountStatus.Success)
        {
            throw CreateFault(status);
        }
    }

    private static Exception CreateFault(AccountStatus status)
    {
        if (status == AccountStatus.MissingSessionToken)
        {
            return ServiceFaultFactory.CreateSessionFault(SessionFailure.MissingSessionToken);
        }

        if (status == AccountStatus.InvalidSession)
        {
            return ServiceFaultFactory.CreateSessionFault(SessionFailure.InvalidSession);
        }

        AccountFailure failure = status switch
        {
            AccountStatus.MissingRegistrationData => AccountFailure.MissingRegistrationData,
            AccountStatus.InvalidUsername => AccountFailure.InvalidUsername,
            AccountStatus.InvalidEmailAddress => AccountFailure.InvalidEmailAddress,
            AccountStatus.InvalidPassword => AccountFailure.InvalidPassword,
            AccountStatus.InvalidLocale => AccountFailure.InvalidLocale,
            AccountStatus.InvalidImage => AccountFailure.InvalidImage,
            AccountStatus.UsernameUnavailable => AccountFailure.UsernameUnavailable,
            AccountStatus.EmailUnavailable => AccountFailure.EmailUnavailable,
            AccountStatus.InvalidVerificationCode => AccountFailure.InvalidVerificationCode,
            AccountStatus.ExpiredVerificationCode => AccountFailure.ExpiredVerificationCode,
            AccountStatus.IncorrectCurrentPassword => AccountFailure.IncorrectCurrentPassword,
            AccountStatus.InactiveAccount => AccountFailure.InactiveAccount,
            AccountStatus.ActiveMatchInProgress => AccountFailure.ActiveMatchInProgress,
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                "Unsupported account status.")
        };

        return ServiceFaultFactory.CreateAccountFault(failure);
    }

    private static NewAccountRegistration? MapRegistration(NewAccount? account)
    {
        if (account == null)
        {
            return null;
        }

        ProfileImageData? profileImage = null;
        if (account.ProfileImage != null)
        {
            byte[] content = account.ProfileImage.Content == null
                ? Array.Empty<byte>()
                : (byte[])account.ProfileImage.Content.Clone();
            profileImage = new ProfileImageData(
                content,
                account.ProfileImage.MediaType ?? string.Empty);
        }

        NewAccountRegistration registration = new NewAccountRegistration(
            account.VerificationCode ?? string.Empty,
            account.Username ?? string.Empty,
            account.EmailAddress ?? string.Empty,
            account.Password ?? string.Empty,
            account.PreferredLocaleCode ?? string.Empty,
            profileImage);
        return registration;
    }

    private static AccountReference MapAccountReference(AccountSummary summary)
    {
        AccountReference reference = new AccountReference
        {
            AccountId = summary.AccountId,
            Username = summary.Username
        };
        return reference;
    }
}
