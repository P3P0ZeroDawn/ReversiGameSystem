using System;
using System.Threading.Tasks;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Presentation.AccountCreation;

internal sealed class AccountCreationViewModel
{
    private readonly IAccountClient _accounts;

    internal AccountCreationViewModel(IAccountClient accounts)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    }

    internal string Username { get; set; } = string.Empty;

    internal string EmailAddress { get; set; } = string.Empty;

    internal string Password { get; set; } = string.Empty;

    internal string PasswordConfirmation { get; set; } = string.Empty;

    internal string LocaleCode { get; set; } = string.Empty;

    internal ProfileImageContent? ProfileImage { get; set; }

    internal bool IsSubmitting { get; private set; }

    internal async Task<AccountCreationStatus> CreateAccount()
    {
        if (IsSubmitting)
        {
            return AccountCreationStatus.SubmissionInProgress;
        }

        NormalizeInput();
        AccountCreationStatus status = ValidateInput();
        if (status != AccountCreationStatus.Success)
        {
            return status;
        }

        IsSubmitting = true;
        try
        {
            status = await _accounts.CreateAccount(
                Username,
                EmailAddress,
                Password,
                LocaleCode,
                ProfileImage?.Content,
                ProfileImage?.MediaType);
        }
        finally
        {
            IsSubmitting = false;
        }
        return status;
    }

    private void NormalizeInput()
    {
        Username = Username.Trim();
        EmailAddress = EmailAddress.Trim();
        LocaleCode = LocaleCode.Trim();
    }

    private AccountCreationStatus ValidateInput()
    {
        AccountCreationStatus status = AccountCreationStatus.Success;
        if (HasMissingRequiredInformation())
        {
            status = AccountCreationStatus.MissingRequiredInformation;
        }
        else if (UsernamePolicy.Validate(Username) != UsernameValidationError.None)
        {
            status = AccountCreationStatus.InvalidUsername;
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
                status = AccountCreationStatus.PasswordMismatch;
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

    private static AccountCreationStatus MapEmailError(EmailValidationError error)
    {
        AccountCreationStatus status;
        switch (error)
        {
            case EmailValidationError.TooLong:
                status = AccountCreationStatus.EmailTooLong;
                break;
            case EmailValidationError.InvalidLocalPart:
                status = AccountCreationStatus.InvalidEmailLocalPart;
                break;
            case EmailValidationError.InvalidDomain:
                status = AccountCreationStatus.InvalidEmailDomain;
                break;
            default:
                status = AccountCreationStatus.InvalidEmailFormat;
                break;
        }
        return status;
    }

    private static AccountCreationStatus MapPasswordError(PasswordValidationError error)
    {
        AccountCreationStatus status;
        switch (error)
        {
            case PasswordValidationError.TooShort:
                status = AccountCreationStatus.PasswordTooShort;
                break;
            case PasswordValidationError.TooLong:
                status = AccountCreationStatus.PasswordTooLong;
                break;
            case PasswordValidationError.MissingUppercaseLetter:
                status = AccountCreationStatus.PasswordMissingUppercase;
                break;
            case PasswordValidationError.MissingLowercaseLetter:
                status = AccountCreationStatus.PasswordMissingLowercase;
                break;
            case PasswordValidationError.MissingDigit:
                status = AccountCreationStatus.PasswordMissingDigit;
                break;
            case PasswordValidationError.MissingSymbol:
                status = AccountCreationStatus.PasswordMissingSymbol;
                break;
            default:
                status = AccountCreationStatus.PasswordUnsupportedCharacters;
                break;
        }
        return status;
    }
}
