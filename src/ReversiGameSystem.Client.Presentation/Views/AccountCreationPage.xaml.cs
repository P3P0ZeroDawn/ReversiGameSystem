using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Presentation.AccountCreation;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays the account creation form.
/// </summary>
public partial class AccountCreationPage : Page
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private readonly ProfileImageSelectionService _profileImageSelectionService;
    private readonly AccountCreationViewModel _viewModel;
    private ProfileImageSelection? _pendingProfileImage;

    /// <summary>
    /// Initializes the account creation page.
    /// </summary>
    public AccountCreationPage()
        : this(new AccountCreationViewModel(ClientRuntime.AccountService))
    {
    }

    internal AccountCreationPage(AccountCreationViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        _profileImageSelectionService = new ProfileImageSelectionService(
            LocalizationManager.Instance, _dialogService);
    }

    private void OnProfilePictureClick(object sender, RoutedEventArgs e)
    {
        ProfileImageSelection? selection = _profileImageSelectionService.Select(
            this, "AccountCreation");
        if (selection == null)
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
        if (_viewModel.IsSubmitting)
        {
            return;
        }

        createAccountButton.IsEnabled = false;
        try
        {
            CopyFormToViewModel();
            _viewModel.ProfileImage = await CreateProfileImageUpload();
            AccountCreationStatus status = await _viewModel.CreateAccount();
            HandleCreateAccountResponse(status, _viewModel.EmailAddress);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException or NotSupportedException)
        {
            _dialogService.ShowWarning(
                this, "AccountCreation_ImageNotSupportedTitle", "AccountCreation_ImageNotSupportedMessage");
        }
        finally
        {
            createAccountButton.IsEnabled = true;
        }
    }

    private void CopyFormToViewModel()
    {
        _viewModel.Username = usernameTextBox.Text;
        _viewModel.EmailAddress = emailTextBox.Text;
        _viewModel.Password = passwordBox.Password;
        _viewModel.PasswordConfirmation = confirmPasswordBox.Password;
        _viewModel.LocaleCode = LocalizationManager.Instance.CultureName;
    }

    private async System.Threading.Tasks.Task<ProfileImageContent?> CreateProfileImageUpload()
    {
        string? imagePath = _pendingProfileImage?.FilePath;
        if (imagePath == null)
        {
            return null;
        }

        ProfileImageContent upload = await System.Threading.Tasks.Task.Run(
            () => ProfileImageModificationService.ReadUpload(imagePath));
        return upload;
    }

    private void HandleCreateAccountResponse(AccountCreationStatus status, string email)
    {
        switch (status)
        {
            case AccountCreationStatus.Success:
                _dialogService.ShowInformation(
                    this,
                    "AccountCreation_SuccessTitle",
                    "AccountCreation_SuccessMessage");
                NavigationService?.Navigate(new LoginScreenPage(email));
                break;
            case AccountCreationStatus.MissingRequiredInformation:
                ShowValidationWarning("MissingRequiredInformation");
                break;
            case AccountCreationStatus.UsernameUnavailable:
                _dialogService.ShowWarning(
                    this,
                    "AccountCreation_UsernameAlreadyTakenTitle",
                    "AccountCreation_UsernameAlreadyTakenMessage");
                break;
            case AccountCreationStatus.EmailUnavailable:
                _dialogService.ShowWarning(
                    this,
                    "AccountCreation_EmailAlreadyTakenTitle",
                    "AccountCreation_EmailAlreadyTakenMessage");
                break;
            case AccountCreationStatus.InvalidUsername:
                ShowValidationWarning("InvalidUsernameFormat");
                break;
            case AccountCreationStatus.EmailTooLong:
                ShowEmailValidationWarning("AccountCreation_EmailTooLongMessage");
                break;
            case AccountCreationStatus.InvalidEmailLocalPart:
                ShowEmailValidationWarning("AccountCreation_InvalidEmailNameMessage");
                break;
            case AccountCreationStatus.InvalidEmailDomain:
                ShowEmailValidationWarning("AccountCreation_InvalidEmailDomainMessage");
                break;
            case AccountCreationStatus.InvalidEmailFormat:
                ShowEmailValidationWarning("AccountCreation_InvalidEmailFormatMessage");
                break;
            case AccountCreationStatus.PasswordMismatch:
                ShowValidationWarning("PasswordMismatch");
                break;
            case AccountCreationStatus.InvalidProfileImage:
                _dialogService.ShowWarning(this, "AccountCreation_ImageNotSupportedTitle", "AccountCreation_ImageNotSupportedMessage");
                break;
            case AccountCreationStatus.PasswordTooShort:
                ShowPasswordValidationWarning("AccountCreation_PasswordTooShortMessage");
                break;
            case AccountCreationStatus.PasswordTooLong:
                ShowPasswordValidationWarning("AccountCreation_PasswordTooLongMessage");
                break;
            case AccountCreationStatus.PasswordMissingUppercase:
                ShowPasswordValidationWarning("AccountCreation_PasswordMissingUppercaseMessage");
                break;
            case AccountCreationStatus.PasswordMissingLowercase:
                ShowPasswordValidationWarning("AccountCreation_PasswordMissingLowercaseMessage");
                break;
            case AccountCreationStatus.PasswordMissingDigit:
                ShowPasswordValidationWarning("AccountCreation_PasswordMissingDigitMessage");
                break;
            case AccountCreationStatus.PasswordMissingSymbol:
                ShowPasswordValidationWarning("AccountCreation_PasswordMissingSymbolMessage");
                break;
            case AccountCreationStatus.PasswordUnsupportedCharacters:
                ShowPasswordValidationWarning("AccountCreation_PasswordUnsupportedCharactersMessage");
                break;
            case AccountCreationStatus.ServiceUnavailable:
                ShowServiceUnavailable();
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

    private void ShowValidationWarning(string alertName)
    {
        _dialogService.ShowWarning(
            this,
            $"AccountCreation_{alertName}Title",
            $"AccountCreation_{alertName}Message");
    }

    private void ShowEmailValidationWarning(string messageKey)
    {
        _dialogService.ShowWarning(this, "AccountCreation_InvalidEmailFormatTitle", messageKey);
    }

    private void ShowPasswordValidationWarning(string messageKey)
    {
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
