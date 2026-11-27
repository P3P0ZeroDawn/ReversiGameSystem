using System;
using System.Threading.Tasks;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Features.Accounts;

internal sealed class AccountViewModel
{
    private readonly IAccountClient _accounts;
    private readonly ClientSession _session;
    private string _passwordResetEmail = string.Empty;

    internal AccountViewModel(IAccountClient accounts, ClientSession session)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    internal bool IsSubmitting { get; private set; }

    internal async Task<AccountStatus> ChangePassword(
        string currentPassword, string newPassword, string confirmation)
    {
        if (IsSubmitting)
        {
            return AccountStatus.SubmissionInProgress;
        }
        string sessionToken = _session.SessionToken ?? string.Empty;
        if (_session.IsGuest || string.IsNullOrEmpty(sessionToken))
        {
            return AccountStatus.InvalidSession;
        }
        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword)
            || string.IsNullOrEmpty(confirmation))
        {
            return AccountStatus.MissingRequiredInformation;
        }
        if (!string.Equals(newPassword, confirmation, StringComparison.Ordinal))
        {
            return AccountStatus.PasswordMismatch;
        }
        if (PasswordPolicy.Validate(newPassword) != PasswordValidationError.None)
        {
            return AccountStatus.InvalidPassword;
        }
        IsSubmitting = true;
        AccountStatus status;
        try
        {
            status = await _accounts.ChangePassword(sessionToken, currentPassword, newPassword);
            if (_session.SessionToken != sessionToken)
            {
                status = AccountStatus.InvalidSession;
            }
        }
        finally
        {
            IsSubmitting = false;
        }
        return status;
    }

    internal async Task<AccountStatus> RequestPasswordResetCode(string emailAddress)
    {
        if (IsSubmitting)
        {
            return AccountStatus.SubmissionInProgress;
        }
        _passwordResetEmail = string.Empty;
        string normalizedEmail = emailAddress.Trim();
        if (EmailPolicy.Validate(normalizedEmail) != EmailValidationError.None)
        {
            return AccountStatus.InvalidEmailFormat;
        }
        IsSubmitting = true;
        AccountStatus status;
        try
        {
            status = await _accounts.RequestPasswordResetCode(normalizedEmail);
            if (status == AccountStatus.Success)
            {
                _passwordResetEmail = normalizedEmail;
            }
        }
        finally
        {
            IsSubmitting = false;
        }
        return status;
    }

    internal async Task<AccountStatus> ResetPassword(string verificationCode, string newPassword)
    {
        if (IsSubmitting)
        {
            return AccountStatus.SubmissionInProgress;
        }
        if (string.IsNullOrEmpty(_passwordResetEmail))
        {
            return AccountStatus.MissingRequiredInformation;
        }
        string code = verificationCode.Trim();
        if (!VerificationCodePolicy.IsValid(code))
        {
            return AccountStatus.InvalidVerificationCode;
        }
        if (PasswordPolicy.Validate(newPassword) != PasswordValidationError.None)
        {
            return AccountStatus.InvalidPassword;
        }
        IsSubmitting = true;
        AccountStatus status;
        try
        {
            status = await _accounts.ResetPassword(_passwordResetEmail, code, newPassword);
            if (status == AccountStatus.Success || status == AccountStatus.ExpiredVerificationCode)
            {
                _passwordResetEmail = string.Empty;
            }
        }
        finally
        {
            IsSubmitting = false;
        }
        return status;
    }

    internal async Task<AccountStatus> DeactivateAccount(string currentPassword)
    {
        if (IsSubmitting)
        {
            return AccountStatus.SubmissionInProgress;
        }
        string sessionToken = _session.SessionToken ?? string.Empty;
        if (_session.IsGuest || string.IsNullOrEmpty(sessionToken))
        {
            return AccountStatus.InvalidSession;
        }
        if (string.IsNullOrEmpty(currentPassword))
        {
            return AccountStatus.MissingRequiredInformation;
        }
        IsSubmitting = true;
        AccountStatus status;
        try
        {
            status = await _accounts.DeactivateAccount(sessionToken, currentPassword);
            if (_session.SessionToken != sessionToken)
            {
                status = AccountStatus.InvalidSession;
            }
            else if (status == AccountStatus.Success)
            {
                _session.Clear();
            }
        }
        finally
        {
            IsSubmitting = false;
        }
        return status;
    }
}
