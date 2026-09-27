using System;
using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Contracts.Accounts;
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

    internal LoginScreenPage(string initialLogin) : this()
    {
        loginTextBox.Text = initialLogin;
    }

    private void OnCreateAccountClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new AccountCreationPage());
    }

    private async void OnLoginClick(object sender, RoutedEventArgs e)
    {
        string login = loginTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(passwordBox.Password))
        {
            _dialogService.ShowWarning(
                this,
                "LoginScreen_MissingCredentialsTitle",
                "LoginScreen_MissingCredentialsMessage");
            return;
        }

        loginButton.IsEnabled = false;
        try
        {
            LoginResponse response = await ClientRuntime.Services.AccountService.LoginAsync(
                new LoginRequest
                {
                    Login = login,
                    Password = passwordBox.Password
                });
            HandleLoginResponse(response);
        }
        catch (Exception)
        {
            ShowLoginError(
                "LoginScreen_ServiceUnavailableTitle",
                "LoginScreen_ServiceUnavailableMessage");
        }
        finally
        {
            loginButton.IsEnabled = true;
        }
    }

    private void HandleLoginResponse(LoginResponse response)
    {
        switch (response.ResultCode)
        {
            case LoginResultCode.Success when response.Session != null:
                AccountPreviewState.Start(response.Session);
                NavigationService?.Navigate(new MainMenuPage());
                break;
            case LoginResultCode.InvalidCredentials:
                ShowLoginError(
                    "LoginScreen_InvalidCredentialsTitle",
                    "LoginScreen_InvalidCredentialsMessage");
                break;
            case LoginResultCode.AccountInactive:
                ShowLoginError(
                    "LoginScreen_AccountInactiveTitle",
                    "LoginScreen_AccountInactiveMessage");
                break;
            case LoginResultCode.InvalidRequest:
                _dialogService.ShowWarning(
                    this,
                    "LoginScreen_MissingCredentialsTitle",
                    "LoginScreen_MissingCredentialsMessage");
                break;
            default:
                ShowLoginError(
                    "LoginScreen_ServiceUnavailableTitle",
                    "LoginScreen_ServiceUnavailableMessage");
                break;
        }
    }

    private void ShowLoginError(string titleKey, string messageKey)
    {
        _dialogService.ShowError(this, titleKey, messageKey);
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
        loginTextBox.Text = email;
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
            InitialText = loginTextBox.Text.Trim(),
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
