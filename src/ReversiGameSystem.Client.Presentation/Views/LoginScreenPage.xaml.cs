using System;
using System.ServiceModel;
using Proxy = ReversiGameSystem.ServerProxy;
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

        SetSessionActionsEnabled(false);
        try
        {
            await ClientRuntime.SessionService.RequestLoginCodeAsync(login, passwordBox.Password);
            passwordBox.Clear();
            TextInputDialogOptions options = new TextInputDialogOptions(
                "LoginScreen_VerificationCodeDialogTitle",
                "LoginScreen_VerificationCodeDialogMessage",
                "LoginScreen_VerificationCodeDialogLabel")
            {
                RequireText = true,
                ExactDigitCount = 6
            };
            while (_dialogService.TryGetText(this, options, out string code))
            {
                try
                {
                    Proxy.AuthenticatedSession session =
                        await ClientRuntime.SessionService.CompleteLoginAsync(login, code);
                    HandleLoginOutcome(AuthenticationOutcomeMapper.Map(session));
                    return;
                }
                catch (FaultException<Proxy.SessionFault> fault) when (
                    fault.Detail.Reason == Proxy.SessionFailure.InvalidVerificationCode)
                {
                    ShowLoginError("LoginScreen_InvalidCodeTitle",
                        "LoginScreen_InvalidCodeMessage");
                }
            }
        }
        catch (FaultException<Proxy.SessionFault> fault)
        {
            ShowSessionRejection(fault.Detail.Reason);
        }
        catch (Exception exception) when (exception is CommunicationException or TimeoutException
            or System.Runtime.Serialization.SerializationException or InvalidOperationException)
        {
            ShowLoginError("LoginScreen_ServiceUnavailableTitle",
                "LoginScreen_ServiceUnavailableMessage");
        }
        finally
        {
            SetSessionActionsEnabled(true);
        }
    }

    private void HandleLoginOutcome(AuthenticationOutcome outcome)
    {
        switch (outcome.Status)
        {
            case AuthenticationStatus.Success when outcome.Session != null:
                AccountPreviewState.Start(outcome.Session);
                NavigationService?.Navigate(new MainMenuPage());
                break;
            case AuthenticationStatus.InvalidCredentials:
                ShowLoginError(
                    "LoginScreen_InvalidCredentialsTitle",
                    "LoginScreen_InvalidCredentialsMessage");
                break;
            case AuthenticationStatus.InactiveAccount:
                ShowLoginError(
                    "LoginScreen_AccountInactiveTitle",
                    "LoginScreen_AccountInactiveMessage");
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

    private async void OnContinueAsGuestClick(object sender, RoutedEventArgs e)
    {
        TextInputDialogOptions options = new TextInputDialogOptions(
            "LoginScreen_GuestDialogTitle", "LoginScreen_GuestDialogMessage",
            "LoginScreen_GuestDialogNameLabel")
        {
            ConfirmButtonKey = "LoginScreen_GuestDialogEnterButton",
            RequireText = true,
            MaximumLength = UsernamePolicy.MaximumLength
        };
        SetSessionActionsEnabled(false);
        try
        {
            while (_dialogService.TryGetText(this, options, out string guestName))
            {
                try
                {
                    Proxy.GuestSession session =
                        await ClientRuntime.SessionService.OpenGuestSessionAsync(guestName.Trim());
                    if (string.IsNullOrWhiteSpace(session.SessionToken)
                        || string.IsNullOrWhiteSpace(session.DisplayName)
                        || session.ExpiresAtUtc <= DateTime.UtcNow)
                    {
                        throw new System.Runtime.Serialization.SerializationException(
                            "The guest session is invalid.");
                    }

                    SessionState.GuestToken = session.SessionToken;
                    AccountPreviewState.Clear();
                    NavigationService?.Navigate(new MainMenuPage(session.DisplayName));
                    return;
                }
                catch (FaultException<Proxy.SessionFault> fault)
                {
                    ShowSessionRejection(fault.Detail.Reason);
                    options.InitialText = guestName;
                }
            }
        }
        catch (Exception exception) when (exception is CommunicationException or TimeoutException
            or System.Runtime.Serialization.SerializationException or InvalidOperationException)
        {
            ShowLoginError("GuestAccess_SessionCreationErrorTitle",
                "GuestAccess_SessionCreationErrorMessage");
        }
        finally
        {
            SetSessionActionsEnabled(true);
        }
    }

    private void SetSessionActionsEnabled(bool enabled)
    {
        loginButton.IsEnabled = enabled;
        loginAsGuestButton.IsEnabled = enabled;
        createAccountButton.IsEnabled = enabled;
        forgotPasswordButton.IsEnabled = enabled;
    }

    private void ShowSessionRejection(Proxy.SessionFailure reason)
    {
        (string title, string message) = reason switch
        {
            Proxy.SessionFailure.InvalidCredentials =>
                ("LoginScreen_InvalidCredentialsTitle", "LoginScreen_InvalidCredentialsMessage"),
            Proxy.SessionFailure.InactiveAccount =>
                ("LoginScreen_AccountInactiveTitle", "LoginScreen_AccountInactiveMessage"),
            Proxy.SessionFailure.InvalidVerificationCode =>
                ("LoginScreen_InvalidCodeTitle", "LoginScreen_InvalidCodeMessage"),
            Proxy.SessionFailure.ExpiredVerificationCode =>
                ("LoginScreen_ExpiredCodeTitle", "LoginScreen_ExpiredCodeMessage"),
            Proxy.SessionFailure.InvalidGuestName =>
                ("GuestAccess_InvalidNameTitle", "GuestAccess_InvalidNameMessage"),
            Proxy.SessionFailure.GuestNameUnavailable =>
                ("GuestAccess_NameAlreadyInUseTitle", "GuestAccess_NameAlreadyInUseMessage"),
            _ => ("LoginScreen_ServiceUnavailableTitle", "LoginScreen_ServiceUnavailableMessage")
        };
        ShowLoginError(title, message);
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
