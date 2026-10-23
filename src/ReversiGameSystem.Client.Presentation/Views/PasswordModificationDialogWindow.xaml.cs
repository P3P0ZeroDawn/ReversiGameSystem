using System;
using System.Windows;
using System.Windows.Input;
using ReversiGameSystem.Validation;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Collects the current password and a validated replacement.
/// </summary>
internal partial class PasswordModificationDialogWindow : Window
{
    private readonly DialogService _dialogService;
    private readonly IPasswordModificationService _passwordService;

    internal PasswordModificationDialogWindow(
        LocalizationManager localization,
        DialogService dialogService,
        IPasswordModificationService passwordService)
    {
        InitializeComponent();
        _dialogService = dialogService;
        _passwordService = passwordService;

        Title = localization["PasswordModification_DialogTitle"];
        dialogTitleTextBlock.Text = Title;
        dialogMessageTextBlock.Text = localization["PasswordModification_DialogMessage"];
        currentPasswordLabel.Content = localization["PasswordModification_CurrentPasswordLabel"];
        newPasswordLabel.Content = localization["PasswordModification_NewPasswordLabel"];
        confirmationLabel.Content = localization["PasswordModification_ConfirmationLabel"];
        passwordHintTextBlock.Text = localization["PasswordModification_PasswordHint"];
        cancelButton.Content = localization["Common_CancelButton"];
        confirmButton.Content = localization["PasswordModification_ConfirmButton"];
        Loaded += OnDialogLoaded;
    }

    private void OnDialogLoaded(object sender, RoutedEventArgs e)
    {
        currentPasswordBox.Focus();
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
        string currentPassword = currentPasswordBox.Password;
        string newPassword = newPasswordBox.Password;
        string confirmation = confirmationBox.Password;

        if (string.IsNullOrWhiteSpace(currentPassword)
            || string.IsNullOrWhiteSpace(newPassword)
            || string.IsNullOrWhiteSpace(confirmation))
        {
            ShowWarning("MissingRequiredInformation");
            return;
        }

        if (!string.Equals(newPassword, confirmation, StringComparison.Ordinal))
        {
            ShowWarning("PasswordMismatch");
            return;
        }

        PasswordValidationError error = PasswordPolicy.Validate(newPassword);
        if (error != PasswordValidationError.None)
        {
            ShowPasswordPolicyWarning(error);
            return;
        }

        HandleServiceResult(_passwordService.ChangePassword(currentPassword, newPassword));
    }

    private void HandleServiceResult(PasswordModificationResult result)
    {
        switch (result)
        {
            case PasswordModificationResult.Success:
                DialogResult = true;
                break;
            case PasswordModificationResult.IncorrectCurrentPassword:
                ShowWarning("CurrentPasswordIncorrect");
                currentPasswordBox.Clear();
                currentPasswordBox.Focus();
                break;
            case PasswordModificationResult.BlockedNewPassword:
                ShowWarning("BlockedPassword");
                break;
            case PasswordModificationResult.PersistenceError:
                ShowError("PersistenceError");
                DialogResult = false;
                break;
            default:
                ShowError("ServerConnectionFailed");
                DialogResult = false;
                break;
        }
    }

    private void ShowPasswordPolicyWarning(PasswordValidationError error)
    {
        string messageKey = error switch
        {
            PasswordValidationError.TooShort => "PasswordModification_PasswordTooShortMessage",
            PasswordValidationError.TooLong => "PasswordModification_PasswordTooLongMessage",
            PasswordValidationError.MissingUppercaseLetter =>
                "PasswordModification_PasswordMissingUppercaseMessage",
            PasswordValidationError.MissingLowercaseLetter =>
                "PasswordModification_PasswordMissingLowercaseMessage",
            PasswordValidationError.MissingDigit =>
                "PasswordModification_PasswordMissingDigitMessage",
            PasswordValidationError.MissingSymbol =>
                "PasswordModification_PasswordMissingSymbolMessage",
            _ => "PasswordModification_PasswordUnsupportedCharactersMessage"
        };

        _dialogService.ShowWarning(this, "PasswordModification_InvalidPasswordTitle", messageKey);
    }

    private void ShowWarning(string resourceName)
    {
        _dialogService.ShowWarning(
            this,
            $"PasswordModification_{resourceName}Title",
            $"PasswordModification_{resourceName}Message");
    }

    private void ShowError(string resourceName)
    {
        _dialogService.ShowError(
            this,
            $"PasswordModification_{resourceName}Title",
            $"PasswordModification_{resourceName}Message");
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
