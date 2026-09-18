using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
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

    /// <summary>
    /// Initializes the account creation page.
    /// </summary>
    public AccountCreationPage()
    {
        InitializeComponent();
    }

    private void OnProfilePictureClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog fileDialog = new OpenFileDialog
        {
            Title = LocalizationManager.Instance["AccountCreation_SelectProfileImageTitle"],
            Filter = LocalizationManager.Instance["AccountCreation_ProfileImageFilterLabel"]
                + "|*.png;*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };

        Window? owner = Window.GetWindow(this);
        bool? selected = owner == null ? fileDialog.ShowDialog() : fileDialog.ShowDialog(owner);
        if (selected != true)
        {
            return;
        }

        if (!ProfilePicturePolicy.TryCreatePreview(fileDialog.FileName, out BitmapSource? preview))
        {
            ShowValidationWarning("ImageNotSupported");
            return;
        }

        profilePictureImage.Source = preview;
    }

    private void OnCreateAccountClick(object sender, RoutedEventArgs e)
    {
        string email = emailTextBox.Text.Trim();
        string password = passwordBox.Password;
        string confirmation = confirmPasswordBox.Password;

        if (HasMissingRequiredInformation())
        {
            ShowValidationWarning("MissingRequiredInformation");
            return;
        }

        EmailValidationError emailError = EmailPolicy.Validate(email);
        if (emailError != EmailValidationError.None)
        {
            ShowEmailValidationWarning(emailError);
            return;
        }

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            ShowValidationWarning("PasswordMismatch");
            return;
        }

        PasswordValidationError passwordError = PasswordPolicy.Validate(password);
        if (passwordError != PasswordValidationError.None)
        {
            ShowPasswordValidationWarning(passwordError);
            return;
        }
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
}
