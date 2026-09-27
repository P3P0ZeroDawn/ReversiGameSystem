using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Domain.Accounts;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays the sign-in form.
/// </summary>
public partial class LoginScreenPage : Page
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);

    /// <summary>
    /// Initializes the sign-in page.
    /// </summary>
    public LoginScreenPage()
    {
        InitializeComponent();
    }

    private void OnCreateAccountClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new AccountCreationPage());
    }

    private void OnLoginClick(object sender, RoutedEventArgs e)
    {
        string email = emailTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(passwordBox.Password))
        {
            _dialogService.ShowWarning(
                this,
                "LoginScreen_MissingCredentialsTitle",
                "LoginScreen_MissingCredentialsMessage");
            return;
        }

        if (EmailPolicy.Validate(email) != EmailValidationError.None)
        {
            _dialogService.ShowWarning(
                this, "LoginScreen_InvalidEmailTitle", "LoginScreen_InvalidEmailMessage");
            return;
        }

        TextInputDialogOptions options = new TextInputDialogOptions(
            "LoginScreen_VerificationCodeDialogTitle",
            "LoginScreen_VerificationCodeDialogMessage",
            "LoginScreen_VerificationCodeDialogLabel")
        {
            RequireText = true,
            ExactDigitCount = 6,
            MessageArguments = new object[] { email }
        };

        if (_dialogService.TryGetText(this, options, out _))
        {
            // TODO: Send and verify the code on the server before granting account access.
            AccountPreviewState.Start(email, null);
            NavigationService?.Navigate(new MainMenuPage());
        }
    }

    private void OnContinueAsGuestClick(object sender, RoutedEventArgs e)
    {
        TextInputDialogOptions options = new TextInputDialogOptions(
            "LoginScreen_GuestDialogTitle",
            "LoginScreen_GuestDialogMessage",
            "LoginScreen_GuestDialogNameLabel")
        {
            ConfirmButtonKey = "LoginScreen_GuestDialogEnterButton",
            RequireText = false,
            MaximumLength = UsernamePolicy.MaximumLength
        };

        while (_dialogService.TryGetText(this, options, out string guestName))
        {
            GuestSessionCreationResult result = GuestSessionPreviewState.TryStart(
                guestName,
                out GuestSession? session);
            if (result == GuestSessionCreationResult.Success && session != null)
            {
                AccountPreviewState.Clear();
                NavigationService?.Navigate(new MainMenuPage(session.Name));
                return;
            }

            if (result == GuestSessionCreationResult.MissingName)
            {
                _dialogService.ShowWarning(
                    this,
                    "GuestAccess_MissingNameTitle",
                    "GuestAccess_MissingNameMessage");
            }
            else if (result == GuestSessionCreationResult.InvalidNameFormat)
            {
                _dialogService.ShowWarning(
                    this,
                    "GuestAccess_InvalidNameTitle",
                    "GuestAccess_InvalidNameMessage");
            }
            else if (result == GuestSessionCreationResult.NameAlreadyInUse)
            {
                _dialogService.ShowWarning(
                    this,
                    "GuestAccess_NameAlreadyInUseTitle",
                    "GuestAccess_NameAlreadyInUseMessage");
            }
            else
            {
                _dialogService.ShowError(
                    this,
                    "GuestAccess_SessionCreationErrorTitle",
                    "GuestAccess_SessionCreationErrorMessage");
                return;
            }

            options.InitialText = GuestUsernamePolicy.Normalize(guestName);
        }
    }

    private void OnForgotPasswordClick(object sender, RoutedEventArgs e)
    {
        if (!TryGetPasswordResetEmail(out string email))
        {
            return;
        }

        // TODO: Check the account and send a reset code from the server.
        _dialogService.ShowInformation(
            this, "PasswordReset_CodeSentTitle", "PasswordReset_CodeSentMessage");

        if (!ConfirmPasswordResetCode(email)
            || !_dialogService.TryGetNewPassword(this, out _))
        {
            return;
        }

        // TODO: Verify the reset code and persist the new password on the server.
        _dialogService.ShowInformation(
            this, "PasswordReset_SuccessTitle", "PasswordReset_SuccessMessage");
        emailTextBox.Text = email;
        passwordBox.Clear();
    }

    private bool TryGetPasswordResetEmail(out string email)
    {
        TextInputDialogOptions options = new TextInputDialogOptions(
            "PasswordReset_EmailDialogTitle",
            "PasswordReset_EmailDialogMessage",
            "Common_EmailLabel")
        {
            ConfirmButtonKey = "PasswordReset_ContinueButton",
            InitialText = emailTextBox.Text.Trim(),
            RequireText = true,
            MaximumLength = 0
        };

        while (_dialogService.TryGetText(this, options, out string enteredEmail))
        {
            email = enteredEmail.Trim();
            if (EmailPolicy.Validate(email) == EmailValidationError.None)
            {
                return true;
            }

            _dialogService.ShowWarning(
                this, "PasswordReset_InvalidEmailTitle", "PasswordReset_InvalidEmailMessage");
            options.InitialText = email;
        }

        email = string.Empty;
        return false;
    }

    private bool ConfirmPasswordResetCode(string email)
    {
        TextInputDialogOptions options = new TextInputDialogOptions(
            "PasswordReset_CodeDialogTitle",
            "PasswordReset_CodeDialogMessage",
            "PasswordReset_CodeDialogLabel")
        {
            RequireText = true,
            ExactDigitCount = 6,
            MessageArguments = new object[] { email }
        };

        return _dialogService.TryGetText(this, options, out _);
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
    }
}
