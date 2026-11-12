using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Presentation.Authentication;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays the sign-in form.
/// </summary>
public partial class LoginScreenPage : Page
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private readonly LoginViewModel _viewModel;

    /// <summary>
    /// Initializes the sign-in page.
    /// </summary>
    public LoginScreenPage() : this(new LoginViewModel(ClientRuntime.SessionService))
    {
    }

    internal LoginScreenPage(LoginViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
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
        SetSessionActionsEnabled(false);
        try
        {
            SessionStatus status = await _viewModel.RequestLoginCode(
                login,
                passwordBox.Password);
            if (status == SessionStatus.Success)
            {
                passwordBox.Clear();
                await ConfirmLogin(login);
            }
            else if (status == SessionStatus.MissingCredentials)
            {
                _dialogService.ShowWarning(
                    this,
                    "LoginScreen_MissingCredentialsTitle",
                    "LoginScreen_MissingCredentialsMessage");
            }
            else
            {
                ShowSessionRejection(status);
            }
        }
        finally
        {
            SetSessionActionsEnabled(true);
        }
    }

    private async Task ConfirmLogin(string login)
    {
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
            SessionResult<AuthenticatedAccountSession> result =
                await _viewModel.CompleteLogin(login, code);
            if (result.Status == SessionStatus.Success)
            {
                AuthenticatedAccountSession session = result.Value
                    ?? throw new InvalidOperationException(
                        "A successful session result must contain its value.");
                ClientRuntime.CurrentSession.StartAuthenticated(session);
                NavigationService?.Navigate(new MainMenuPage());
                return;
            }

            if (result.Status == SessionStatus.InvalidVerificationCode)
            {
                ShowLoginError("LoginScreen_InvalidCodeTitle", "LoginScreen_InvalidCodeMessage");
                continue;
            }

            ShowSessionRejection(result.Status);
            return;
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
                SessionResult<GuestAccountSession> result =
                    await _viewModel.OpenGuestSession(guestName);
                if (result.Status == SessionStatus.Success)
                {
                    GuestAccountSession session = result.Value
                        ?? throw new InvalidOperationException(
                            "A successful guest session result must contain its value.");
                    ClientRuntime.CurrentSession.StartGuest(session);
                    NavigationService?.Navigate(new MainMenuPage(session.DisplayName));
                    return;
                }

                ShowSessionRejection(result.Status);
                options.InitialText = guestName;
            }
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

    private void ShowSessionRejection(SessionStatus reason)
    {
        (string title, string message) = reason switch
        {
            SessionStatus.InvalidCredentials =>
                ("LoginScreen_InvalidCredentialsTitle", "LoginScreen_InvalidCredentialsMessage"),
            SessionStatus.InactiveAccount =>
                ("LoginScreen_AccountInactiveTitle", "LoginScreen_AccountInactiveMessage"),
            SessionStatus.InvalidVerificationCode =>
                ("LoginScreen_InvalidCodeTitle", "LoginScreen_InvalidCodeMessage"),
            SessionStatus.ExpiredVerificationCode =>
                ("LoginScreen_ExpiredCodeTitle", "LoginScreen_ExpiredCodeMessage"),
            SessionStatus.InvalidGuestName =>
                ("GuestAccess_InvalidNameTitle", "GuestAccess_InvalidNameMessage"),
            SessionStatus.GuestNameUnavailable =>
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
