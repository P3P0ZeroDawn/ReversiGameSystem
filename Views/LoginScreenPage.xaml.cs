using System.Windows;
using System.Windows.Controls;
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
            RequireText = true,
            MaximumLength = 32
        };

        if (_dialogService.TryGetText(this, options, out string guestName))
        {
            NavigationService?.Navigate(new MainMenuPage(guestName.Trim()));
        }
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
    }
}
