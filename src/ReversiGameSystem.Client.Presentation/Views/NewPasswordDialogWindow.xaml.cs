using System;
using System.Windows;
using System.Windows.Input;
using ReversiGameSystem.Domain.Accounts;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Collects and validates a new password and its confirmation.
/// </summary>
internal partial class NewPasswordDialogWindow : Window
{
    private readonly DialogService _dialogService;

    internal NewPasswordDialogWindow(LocalizationManager localization, DialogService dialogService)
    {
        InitializeComponent();

        _dialogService = dialogService;
        Title = localization["PasswordReset_NewPasswordDialogTitle"];
        dialogTitleTextBlock.Text = Title;
        dialogMessageTextBlock.Text = localization["PasswordReset_NewPasswordDialogMessage"];
        newPasswordLabel.Content = localization["PasswordReset_NewPasswordLabel"];
        confirmationLabel.Content = localization["PasswordReset_ConfirmationLabel"];
        passwordHintTextBlock.Text = localization["PasswordReset_PasswordHint"];
        cancelButton.Content = localization["Common_CancelButton"];
        confirmButton.Content = localization["Common_ConfirmButton"];

        Loaded += OnDialogLoaded;
    }

    internal string NewPassword { get; private set; } = string.Empty;

    private void OnDialogLoaded(object sender, RoutedEventArgs e)
    {
        newPasswordBox.Focus();
    }

    private void OnTitleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        string password = newPasswordBox.Password;
        string confirmation = confirmationBox.Password;

        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmation))
        {
            _dialogService.ShowWarning(
                this, "PasswordReset_MissingPasswordTitle", "PasswordReset_MissingPasswordMessage");
            return;
        }

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            _dialogService.ShowWarning(
                this,
                "PasswordReset_PasswordMismatchTitle",
                "PasswordReset_PasswordMismatchMessage");
            return;
        }

        PasswordValidationError error = PasswordPolicy.Validate(password);
        if (error != PasswordValidationError.None)
        {
            ShowPasswordPolicyWarning(error);
            return;
        }

        NewPassword = password;
        DialogResult = true;
    }

    private void ShowPasswordPolicyWarning(PasswordValidationError error)
    {
        string messageKey = error switch
        {
            PasswordValidationError.TooShort => "PasswordReset_PasswordTooShortMessage",
            PasswordValidationError.TooLong => "PasswordReset_PasswordTooLongMessage",
            PasswordValidationError.MissingUppercaseLetter =>
                "PasswordReset_PasswordMissingUppercaseMessage",
            PasswordValidationError.MissingLowercaseLetter =>
                "PasswordReset_PasswordMissingLowercaseMessage",
            PasswordValidationError.MissingDigit => "PasswordReset_PasswordMissingDigitMessage",
            PasswordValidationError.MissingSymbol => "PasswordReset_PasswordMissingSymbolMessage",
            _ => "PasswordReset_PasswordUnsupportedCharactersMessage"
        };

        _dialogService.ShowWarning(this, "PasswordReset_InvalidPasswordTitle", messageKey);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
