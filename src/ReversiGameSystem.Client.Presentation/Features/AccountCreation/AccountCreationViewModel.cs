using System;
using System.Threading.Tasks;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Presentation.AccountCreation;

internal sealed class AccountCreationViewModel
{
    private readonly IAccountClient _accounts;
    private NewAccount? _registration;

    internal AccountCreationViewModel(IAccountClient accounts)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    }

    internal string Username { get; set; } = string.Empty;

    internal string EmailAddress { get; set; } = string.Empty;

    internal string Password { get; set; } = string.Empty;

    internal string PasswordConfirmation { get; set; } = string.Empty;

    internal string LocaleCode { get; set; } = string.Empty;

    internal PlayerImageContent? ProfileImage { get; set; }

    internal bool IsSubmitting { get; private set; }

    internal async Task<AccountStatus> RequestRegistrationCode()
    {
        if (IsSubmitting)
        {
            return AccountStatus.SubmissionInProgress;
        }
        _registration = null;
        NormalizeInput();
        AccountStatus status = ValidateInput();
        if (status != AccountStatus.Success)
        {
            return status;
        }
        NewAccount registration = new NewAccount
        {
            Username = Username,
            EmailAddress = EmailAddress,
            Password = Password,
            PreferredLocaleCode = LocaleCode,
            ProfileImage = ProfileImage
        };
        IsSubmitting = true;
        try
        {
            status = await _accounts.RequestRegistrationCode(
                registration.Username, registration.EmailAddress);
            if (status == AccountStatus.Success)
            {
                _registration = registration;
            }
        }
        finally
        {
            IsSubmitting = false;
        }
        return status;
    }

    internal async Task<AccountStatus> CompleteRegistration(string verificationCode)
    {
        if (IsSubmitting)
        {
            return AccountStatus.SubmissionInProgress;
        }
        if (_registration == null)
        {
            return AccountStatus.MissingRequiredInformation;
        }
        string code = verificationCode?.Trim() ?? string.Empty;
        if (!VerificationCodePolicy.IsValid(code))
        {
            return AccountStatus.InvalidVerificationCode;
        }
        _registration.VerificationCode = code;
        IsSubmitting = true;
        AccountStatus status;
        try
        {
            status = await _accounts.CompleteRegistration(_registration);
            if (status == AccountStatus.Success || status == AccountStatus.ExpiredVerificationCode)
            {
                ClearRegistration();
            }
        }
        finally
        {
            IsSubmitting = false;
        }
        return status;
    }

    internal void ClearRegistration()
    {
        _registration = null;
        Password = string.Empty;
        PasswordConfirmation = string.Empty;
        ProfileImage = null;
    }

    private void NormalizeInput()
    {
        Username = Username.Trim();
        EmailAddress = EmailAddress.Trim();
        LocaleCode = LocaleCode.Trim();
    }

    private AccountStatus ValidateInput()
    {
        AccountStatus status = AccountStatus.Success;
        if (HasMissingRequiredInformation())
        {
            status = AccountStatus.MissingRequiredInformation;
        }
        else if (UsernamePolicy.Validate(Username) != UsernameValidationError.None)
        {
            status = AccountStatus.InvalidUsername;
        }
        else
        {
            EmailValidationError emailError = EmailPolicy.Validate(EmailAddress);
            if (emailError != EmailValidationError.None)
            {
                status = MapEmailError(emailError);
            }
            else if (!string.Equals(
                Password,
                PasswordConfirmation,
                StringComparison.Ordinal))
            {
                status = AccountStatus.PasswordMismatch;
            }
            else
            {
                PasswordValidationError passwordError = PasswordPolicy.Validate(Password);
                if (passwordError != PasswordValidationError.None)
                {
                    status = MapPasswordError(passwordError);
                }
            }
        }
        return status;
    }

    private bool HasMissingRequiredInformation()
    {
        return string.IsNullOrWhiteSpace(Username)
            || string.IsNullOrWhiteSpace(EmailAddress)
            || string.IsNullOrWhiteSpace(Password)
            || string.IsNullOrWhiteSpace(PasswordConfirmation);
    }

    private static AccountStatus MapEmailError(EmailValidationError error)
    {
        AccountStatus status = error switch
        {
            EmailValidationError.TooLong => AccountStatus.EmailTooLong,
            EmailValidationError.InvalidLocalPart => AccountStatus.InvalidEmailLocalPart,
            EmailValidationError.InvalidDomain => AccountStatus.InvalidEmailDomain,
            _ => AccountStatus.InvalidEmailFormat
        };
        return status;
    }

    private static AccountStatus MapPasswordError(PasswordValidationError error)
    {
        AccountStatus status = error switch
        {
            PasswordValidationError.TooShort => AccountStatus.PasswordTooShort,
            PasswordValidationError.TooLong => AccountStatus.PasswordTooLong,
            PasswordValidationError.MissingUppercaseLetter =>
                AccountStatus.PasswordMissingUppercase,
            PasswordValidationError.MissingLowercaseLetter =>
                AccountStatus.PasswordMissingLowercase,
            PasswordValidationError.MissingDigit => AccountStatus.PasswordMissingDigit,
            PasswordValidationError.MissingSymbol => AccountStatus.PasswordMissingSymbol,
            _ => AccountStatus.PasswordUnsupportedCharacters
        };
        return status;
    }
}
