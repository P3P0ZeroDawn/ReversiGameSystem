using System;
using System.IO;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Presentation.AccountCreation;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Views;

public partial class AccountCreationPage : Page
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private readonly ProfileImageSelectionService _profileImageSelectionService;
    private readonly AccountCreationViewModel _viewModel;
    private ProfileImageSelection? _pendingProfileImage;

    public AccountCreationPage()
        : this(new AccountCreationViewModel(ClientRuntime.AccountService))
    {
    }

    internal AccountCreationPage(AccountCreationViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        Unloaded += OnPageUnloaded;
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
        IsEnabled = false;
        try
        {
            CopyFormToViewModel();
            if (_pendingProfileImage != null)
            {
                try
                {
                    _viewModel.ProfileImage = await PlayerImageService.ReadImageFile(
                        _pendingProfileImage.FilePath);
                }
                catch (Exception exception) when (exception is IOException
                    or UnauthorizedAccessException or NotSupportedException)
                {
                    _dialogService.ShowWarning(this, "AccountCreation_ImageNotSupportedTitle",
                        "AccountCreation_ImageNotSupportedMessage");
                    return;
                }
            }
            AccountStatus status = await _viewModel.RequestRegistrationCode();
            if (!IsLoaded)
            {
                return;
            }
            if (status == AccountStatus.Success)
            {
                await ConfirmRegistration();
            }
            else
            {
                ShowRegistrationStatus(status, _viewModel.EmailAddress);
            }
        }
        catch (FaultException<ServerProxy.ServiceFault> exception)
        {
            if (IsLoaded)
            {
                ClientRuntime.Errors.ReportOperationFailure(exception, nameof(AccountCreationPage));
            }
            else
            {
                ClientRuntime.Errors.RecordFailure(
                    exception, nameof(AccountCreationPage), LogLevel.Error);
            }
        }
        finally
        {
            _viewModel.ClearRegistration();
            IsEnabled = true;
        }
    }

    private async Task ConfirmRegistration()
    {
        TextInputDialogOptions options = new TextInputDialogOptions(
            "AccountCreation_VerificationCodeDialogTitle",
            "AccountCreation_VerificationCodeDialogMessage",
            "AccountCreation_VerificationCodeDialogLabel")
        {
            RequireText = true,
            ExactDigitCount = VerificationCodePolicy.Length,
            MessageArguments = new object[] { _viewModel.EmailAddress }
        };
        while (_dialogService.TryGetText(this, options, out string verificationCode))
        {
            AccountStatus status = await _viewModel.CompleteRegistration(verificationCode);
            if (!IsLoaded)
            {
                return;
            }
            ShowRegistrationStatus(status, _viewModel.EmailAddress);
            if (status != AccountStatus.InvalidVerificationCode)
            {
                return;
            }
        }
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearRegistration();
        passwordBox.Clear();
        confirmPasswordBox.Clear();
    }

    private void CopyFormToViewModel()
    {
        _viewModel.Username = usernameTextBox.Text;
        _viewModel.EmailAddress = emailTextBox.Text;
        _viewModel.Password = passwordBox.Password;
        _viewModel.PasswordConfirmation = confirmPasswordBox.Password;
        _viewModel.LocaleCode = LocalizationManager.Instance.CultureName;
    }

    private void ShowRegistrationStatus(AccountStatus status, string email)
    {
        switch (status)
        {
            case AccountStatus.Success:
                _dialogService.ShowInformation(
                    this,
                    "AccountCreation_SuccessTitle",
                    "AccountCreation_SuccessMessage");
                NavigationService?.Navigate(new LoginScreenPage(email));
                break;
            case AccountStatus.MissingRequiredInformation:
                ShowValidationWarning("MissingRequiredInformation");
                break;
            case AccountStatus.UsernameUnavailable:
                _dialogService.ShowWarning(
                    this,
                    "AccountCreation_UsernameAlreadyTakenTitle",
                    "AccountCreation_UsernameAlreadyTakenMessage");
                break;
            case AccountStatus.EmailUnavailable:
                _dialogService.ShowWarning(
                    this,
                    "AccountCreation_EmailAlreadyTakenTitle",
                    "AccountCreation_EmailAlreadyTakenMessage");
                break;
            case AccountStatus.InvalidUsername:
                ShowValidationWarning("InvalidUsernameFormat");
                break;
            case AccountStatus.EmailTooLong:
                ShowEmailValidationWarning("AccountCreation_EmailTooLongMessage");
                break;
            case AccountStatus.InvalidEmailLocalPart:
                ShowEmailValidationWarning("AccountCreation_InvalidEmailNameMessage");
                break;
            case AccountStatus.InvalidEmailDomain:
                ShowEmailValidationWarning("AccountCreation_InvalidEmailDomainMessage");
                break;
            case AccountStatus.InvalidEmailFormat:
                ShowEmailValidationWarning("AccountCreation_InvalidEmailFormatMessage");
                break;
            case AccountStatus.PasswordMismatch:
                ShowValidationWarning("PasswordMismatch");
                break;
            case AccountStatus.InvalidProfileImage:
                _dialogService.ShowWarning(this, "AccountCreation_ImageNotSupportedTitle",
                    "AccountCreation_ImageNotSupportedMessage");
                break;
            case AccountStatus.PasswordTooShort:
                ShowPasswordValidationWarning("AccountCreation_PasswordTooShortMessage");
                break;
            case AccountStatus.PasswordTooLong:
                ShowPasswordValidationWarning("AccountCreation_PasswordTooLongMessage");
                break;
            case AccountStatus.PasswordMissingUppercase:
                ShowPasswordValidationWarning("AccountCreation_PasswordMissingUppercaseMessage");
                break;
            case AccountStatus.PasswordMissingLowercase:
                ShowPasswordValidationWarning("AccountCreation_PasswordMissingLowercaseMessage");
                break;
            case AccountStatus.PasswordMissingDigit:
                ShowPasswordValidationWarning("AccountCreation_PasswordMissingDigitMessage");
                break;
            case AccountStatus.PasswordMissingSymbol:
                ShowPasswordValidationWarning("AccountCreation_PasswordMissingSymbolMessage");
                break;
            case AccountStatus.PasswordUnsupportedCharacters:
                ShowPasswordValidationWarning(
                    "AccountCreation_PasswordUnsupportedCharactersMessage");
                break;
            case AccountStatus.InvalidVerificationCode:
                ShowValidationWarning("InvalidConfirmationCode");
                break;
            case AccountStatus.ExpiredVerificationCode:
                ShowValidationWarning("ExpiredConfirmationCode");
                break;
            case AccountStatus.InvalidPassword:
                _dialogService.ShowWarning(this, "AccountCreation_InvalidPasswordFormatTitle",
                    "AccountCreation_InvalidPasswordMessage",
                    PasswordPolicy.MinimumLength, PasswordPolicy.MaximumLength);
                break;
            case AccountStatus.InvalidLocale:
                _dialogService.ShowWarning(this,
                    "LanguageSettings_NotAvailableTitle", "LanguageSettings_NotAvailableMessage");
                break;
            case AccountStatus.SubmissionInProgress:
                break;
            case AccountStatus.ServiceUnavailable:
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

    private async void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LanguageChangeService.ConfirmSelection((ComboBox)sender);
    }
}
