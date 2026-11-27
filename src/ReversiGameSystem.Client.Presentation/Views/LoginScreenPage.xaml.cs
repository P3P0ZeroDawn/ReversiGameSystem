using System;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.ServiceModel;
using ReversiGameSystem.Features.Accounts;
using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Features.Preferences;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Presentation.Authentication;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Views;

public partial class LoginScreenPage : Page
{
    private const string SavedLanguageUnavailableMessage
        = "The saved language could not be applied.";
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private readonly LoginViewModel _viewModel;
    private readonly AccountViewModel _accountViewModel = new AccountViewModel(
        ClientRuntime.AccountService, ClientRuntime.CurrentSession);

    public LoginScreenPage() : this(new LoginViewModel(ClientRuntime.SessionService))
    {
        Loaded += (_, _) => ClientRuntime.WarmUpSessionChannel();
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
            ExactDigitCount = VerificationCodePolicy.Length
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
                LanguageChangeResult languageStatus = LocalizationManager.Instance.TrySetCulture(
                    session.PreferredLocaleCode);
                if (languageStatus != LanguageChangeResult.Success)
                {
                    throw new InvalidOperationException(SavedLanguageUnavailableMessage);
                }
                ClientRuntime.CurrentSession.StartAuthenticated(session);
                SettingsViewModel preferences = new SettingsViewModel(
                    ClientRuntime.PreferencesService, ClientRuntime.CurrentSession);
                PreferencesStatus preferencesStatus = await preferences.LoadPreferences();
                if (ClientRuntime.CurrentSession.SessionToken != session.SessionToken)
                {
                    return;
                }
                if (preferencesStatus != PreferencesStatus.Success)
                {
                    ShowLoginError("Settings_PersistenceErrorTitle",
                        "Settings_PersistenceErrorMessage");
                }
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
    private async void OnForgotPasswordClick(object sender, RoutedEventArgs e)
    {
        if (_accountViewModel.IsSubmitting)
        {
            return;
        }
        SetSessionActionsEnabled(false);
        try
        {
            string emailAddress = await BeginPasswordRecovery();
            if (string.IsNullOrEmpty(emailAddress) || !IsLoaded)
            {
                return;
            }
            _dialogService.ShowInformation(
                this, "PasswordReset_CodeSentTitle", "PasswordReset_CodeSentMessage");
            await CompletePasswordRecovery(emailAddress);
        }
        catch (FaultException<ServerProxy.ServiceFault> exception)
        {
            if (IsLoaded)
            {
                ClientRuntime.Errors.ReportOperationFailure(
                    exception, nameof(LoginScreenPage));
            }
            else
            {
                ClientRuntime.Errors.RecordFailure(
                    exception, nameof(LoginScreenPage), LogLevel.Error);
            }
        }
        finally
        {
            SetSessionActionsEnabled(true);
        }
    }

    private async Task<string> BeginPasswordRecovery()
    {
        TextInputDialogOptions options = new TextInputDialogOptions(
            "PasswordReset_EmailDialogTitle", "PasswordReset_EmailDialogMessage",
            "Common_EmailLabel")
        {
            ConfirmButtonKey = "PasswordReset_ContinueButton",
            InitialText = loginTextBox.Text.Trim(),
            RequireText = true,
            MaximumLength = EmailPolicy.MaximumAddressLength
        };
        while (IsLoaded)
        {
            TextInputDialogWindow dialog = new TextInputDialogWindow(
                options, LocalizationManager.Instance) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true)
            {
                return string.Empty;
            }
            string emailAddress = dialog.EnteredText.Trim();
            AccountStatus status = await _accountViewModel.RequestPasswordResetCode(emailAddress);
            if (!IsLoaded)
            {
                return string.Empty;
            }
            if (status == AccountStatus.Success)
            {
                return emailAddress;
            }
            _dialogService.ShowAccountFailure(this, status);
            if (status != AccountStatus.InvalidEmailFormat)
            {
                return string.Empty;
            }
            options.InitialText = emailAddress;
        }
        return string.Empty;
    }

    private async Task CompletePasswordRecovery(string emailAddress)
    {
        TextInputDialogOptions options = new TextInputDialogOptions(
            "PasswordReset_CodeDialogTitle", "PasswordReset_CodeDialogMessage",
            "PasswordReset_CodeDialogLabel")
        {
            RequireText = true,
            ExactDigitCount = VerificationCodePolicy.Length,
            MessageArguments = new object[] { emailAddress }
        };
        while (IsLoaded)
        {
            TextInputDialogWindow codeDialog = new TextInputDialogWindow(
                options, LocalizationManager.Instance) { Owner = Window.GetWindow(this) };
            if (codeDialog.ShowDialog() != true)
            {
                return;
            }
            NewPasswordDialogWindow passwordDialog = new NewPasswordDialogWindow(
                LocalizationManager.Instance, _dialogService) { Owner = Window.GetWindow(this) };
            if (passwordDialog.ShowDialog() != true)
            {
                return;
            }
            AccountStatus status = await _accountViewModel.ResetPassword(
                codeDialog.EnteredText, passwordDialog.NewPassword);
            if (!IsLoaded)
            {
                return;
            }
            if (status == AccountStatus.Success)
            {
                _dialogService.ShowInformation(
                    this, "PasswordReset_SuccessTitle", "PasswordReset_SuccessMessage");
                loginTextBox.Text = emailAddress;
                passwordBox.Clear();
                return;
            }
            _dialogService.ShowAccountFailure(this, status);
            if (status != AccountStatus.InvalidVerificationCode)
            {
                return;
            }
        }
    }

    private async void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LanguageChangeService.ConfirmSelection((ComboBox)sender);
    }
}
