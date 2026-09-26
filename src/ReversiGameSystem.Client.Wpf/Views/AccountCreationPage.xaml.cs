using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays the account creation form.
/// </summary>
public partial class AccountCreationPage : Page
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private readonly ProfileImageSelectionService _profileImageSelectionService;
    private ProfileImageSelection? _pendingProfileImage;

    /// <summary>
    /// Initializes the account creation page.
    /// </summary>
    public AccountCreationPage()
    {
        InitializeComponent();
        _profileImageSelectionService = new ProfileImageSelectionService(
            LocalizationManager.Instance, _dialogService);
    }

    private void OnProfilePictureClick(object sender, RoutedEventArgs e)
    {
        if (!_profileImageSelectionService.TrySelect(
            this, "AccountCreation", out ProfileImageSelection? selection)
            || selection == null)
        {
            return;
        }

        _pendingProfileImage = selection;
        profilePictureButton.Background = new ImageBrush(selection.Preview)
        {
            Stretch = Stretch.UniformToFill
        };
    }

    private void OnCreateAccountClick(object sender, RoutedEventArgs e)
    {
        string email = emailTextBox.Text.Trim();

        if (!ValidateForm(email) || !ConfirmVerificationCode(email))
        {
            return;
        }

        // TODO: Upload _pendingProfileImage with the account creation request.
        AccountPreviewState.Start(
            email, usernameTextBox.Text.Trim(), _pendingProfileImage?.Preview);
        _dialogService.ShowInformation(
            this, "AccountCreation_SuccessTitle", "AccountCreation_SuccessMessage");
        NavigationService?.Navigate(new MainMenuPage());
    }

    private bool ValidateForm(string email)
    {
        string password = passwordBox.Password;
        string confirmation = confirmPasswordBox.Password;

        if (HasMissingRequiredInformation())
        {
            ShowValidationWarning("MissingRequiredInformation");
            return false;
        }

        if (UsernamePolicy.Validate(usernameTextBox.Text.Trim()) != UsernameValidationError.None)
        {
            ShowValidationWarning("InvalidUsernameFormat");
            return false;
        }

        EmailValidationError emailError = EmailPolicy.Validate(email);
        if (emailError != EmailValidationError.None)
        {
            ShowEmailValidationWarning(emailError);
            return false;
        }

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            ShowValidationWarning("PasswordMismatch");
            return false;
        }

        PasswordValidationError passwordError = PasswordPolicy.Validate(password);
        if (passwordError != PasswordValidationError.None)
        {
            ShowPasswordValidationWarning(passwordError);
            return false;
        }

        return true;
    }

    private bool ConfirmVerificationCode(string email)
    {
        TextInputDialogOptions options = new TextInputDialogOptions(
            "AccountCreation_VerificationCodeDialogTitle",
            "AccountCreation_VerificationCodeDialogMessage",
            "AccountCreation_VerificationCodeDialogLabel")
        {
            RequireText = true,
            ExactDigitCount = 6,
            MessageArguments = new object[] { email }
        };

        return _dialogService.TryGetText(this, options, out _);
    }

    private bool HasMissingRequiredInformation()
    {
        return string.IsNullOrWhiteSpace(usernameTextBox.Text)
            || string.IsNullOrWhiteSpace(emailTextBox.Text)
            || string.IsNullOrWhiteSpace(passwordBox.Password)
            || string.IsNullOrWhiteSpace(confirmPasswordBox.Password);
    }

    private void ShowValidationWarning(string alertName)
    {
        _dialogService.ShowWarning(
            this,
            $"AccountCreation_{alertName}Title",
            $"AccountCreation_{alertName}Message");
    }

    private void ShowEmailValidationWarning(EmailValidationError error)
    {
        string messageKey = error switch
        {
            EmailValidationError.TooLong => "AccountCreation_EmailTooLongMessage",
            EmailValidationError.InvalidLocalPart => "AccountCreation_InvalidEmailNameMessage",
            EmailValidationError.InvalidDomain => "AccountCreation_InvalidEmailDomainMessage",
            _ => "AccountCreation_InvalidEmailFormatMessage"
        };

        _dialogService.ShowWarning(this, "AccountCreation_InvalidEmailFormatTitle", messageKey);
    }

    private void ShowPasswordValidationWarning(PasswordValidationError error)
    {
        string messageKey = error switch
        {
            PasswordValidationError.TooShort => "AccountCreation_PasswordTooShortMessage",
            PasswordValidationError.TooLong => "AccountCreation_PasswordTooLongMessage",
            PasswordValidationError.MissingUppercaseLetter =>
                "AccountCreation_PasswordMissingUppercaseMessage",
            PasswordValidationError.MissingLowercaseLetter =>
                "AccountCreation_PasswordMissingLowercaseMessage",
            PasswordValidationError.MissingDigit => "AccountCreation_PasswordMissingDigitMessage",
            PasswordValidationError.MissingSymbol => "AccountCreation_PasswordMissingSymbolMessage",
            _ => "AccountCreation_PasswordUnsupportedCharactersMessage"
        };

        _dialogService.ShowWarning(this, "AccountCreation_InvalidPasswordFormatTitle", messageKey);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new LoginScreenPage());
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
    }
}
