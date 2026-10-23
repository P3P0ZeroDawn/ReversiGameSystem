using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;
using Proxy = ReversiGameSystem.ServerProxy;

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

    private async void OnCreateAccountClick(object sender, RoutedEventArgs e)
    {
        string email = emailTextBox.Text.Trim();

        if (!ValidateForm(email))
        {
            return;
        }

        Proxy.AccountRegistrationCredentials credentials = new Proxy.AccountRegistrationCredentials
        {
            Username = usernameTextBox.Text.Trim(),
            EmailAddress = email,
            Password = passwordBox.Password,
            PreferredLocaleCode = LocalizationManager.Instance.CultureName
        };
        createAccountButton.IsEnabled = false;
        try
        {
            credentials.ProfileImage = await CreateProfileImageUploadAsync();
            Proxy.AccountRegistrationOutcome response = await ClientRuntime.AccountService
                .RegisterAccountAsync(credentials);
            HandleCreateAccountResponse(response, email);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException or NotSupportedException)
        {
            _dialogService.ShowWarning(
                this, "AccountCreation_ImageNotSupportedTitle", "AccountCreation_ImageNotSupportedMessage");
        }
        catch (System.ServiceModel.FaultException<Proxy.ServiceFault>)
        {
            _dialogService.ShowError(this, "AccountCreation_PersistenceErrorTitle", "AccountCreation_PersistenceErrorMessage");
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            ShowServiceUnavailable();
        }
        finally
        {
            createAccountButton.IsEnabled = true;
        }
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

    private async System.Threading.Tasks.Task<Proxy.ProfileImageContent?> CreateProfileImageUploadAsync()
    {
        string? imagePath = _pendingProfileImage?.FilePath;
        if (imagePath == null)
        {
            return null;
        }

        Proxy.ImageContent upload = await System.Threading.Tasks.Task.Run(
            () => ProfileImageModificationService.ReadUpload(imagePath));
        return new Proxy.ProfileImageContent
        {
            Content = upload.Content,
            MediaType = upload.MediaType
        };
    }

    private void HandleCreateAccountResponse(Proxy.AccountRegistrationOutcome response, string email)
    {
        switch (response.Status)
        {
            case Proxy.AccountRegistrationStatus.Success:
                _dialogService.ShowInformation(
                    this,
                    "AccountCreation_SuccessTitle",
                    "AccountCreation_SuccessMessage");
                NavigationService?.Navigate(new LoginScreenPage(email));
                break;
            case Proxy.AccountRegistrationStatus.UsernameUnavailable:
                _dialogService.ShowWarning(
                    this,
                    "AccountCreation_UsernameAlreadyTakenTitle",
                    "AccountCreation_UsernameAlreadyTakenMessage");
                break;
            case Proxy.AccountRegistrationStatus.EmailUnavailable:
                _dialogService.ShowWarning(
                    this,
                    "AccountCreation_EmailAlreadyTakenTitle",
                    "AccountCreation_EmailAlreadyTakenMessage");
                break;
            case Proxy.AccountRegistrationStatus.InvalidUsername:
                ShowValidationWarning("InvalidUsernameFormat");
                break;
            case Proxy.AccountRegistrationStatus.InvalidEmail:
                ShowEmailValidationWarning(EmailValidationError.InvalidStructure);
                break;
            case Proxy.AccountRegistrationStatus.InvalidProfileImage:
                _dialogService.ShowWarning(this, "AccountCreation_ImageNotSupportedTitle", "AccountCreation_ImageNotSupportedMessage");
                break;
            case Proxy.AccountRegistrationStatus.InvalidPassword:
                ShowPasswordValidationWarning(PasswordValidationError.UnsupportedCharacter);
                break;
            default:
                _dialogService.ShowError(
                    this,
                    "AccountCreation_PersistenceErrorTitle",
                    "AccountCreation_PersistenceErrorMessage");
                break;
        }
    }

    private void ShowServiceUnavailable()
    {
        _dialogService.ShowError(
            this,
            "AccountCreation_ServerConnectionFailedTitle",
            "AccountCreation_ServerConnectionFailedMessage");
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
