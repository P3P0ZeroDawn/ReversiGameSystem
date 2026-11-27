using System;
using System.ComponentModel;
using System.IO;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Features.Accounts;
using ReversiGameSystem.Features.Friendships;
using ReversiGameSystem.Features.Profiles;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Views;

internal enum AccountInformationReturnTarget
{
    MainMenu,
    AddFriends,
    Friends
}

public partial class AccountInformationPage : Page
{
    private PlayerProfileViewModel? _profileViewModel;
    private Brush? _defaultProfileBackground;
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private readonly ProfileImageSelectionService _profileImageSelectionService;
    private readonly AccountViewModel _accountViewModel = new AccountViewModel(
        ClientRuntime.AccountService, ClientRuntime.CurrentSession);
    private readonly AccountInformationReturnTarget _returnTarget;
    private readonly string _profileUsername;
    private readonly bool _isOwnProfile;
    private readonly long? _profileAccountId;
    private readonly FriendshipViewModel _friendshipsViewModel = new FriendshipViewModel(
        ClientRuntime.FriendshipService, ClientRuntime.CurrentSession);

    public AccountInformationPage()
        : this(null, AccountInformationReturnTarget.MainMenu, true)
    {
    }

    internal AccountInformationPage(PlayerAccount account,
        AccountInformationReturnTarget returnTarget)
        : this(account, returnTarget, false)
    {
    }

    private AccountInformationPage(
        PlayerAccount? profileAccount,
        AccountInformationReturnTarget returnTarget,
        bool ownProfileRequested)
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        _returnTarget = returnTarget;
        string currentUsername = ClientRuntime.CurrentSession.Username
            ?? LocalizationManager.Instance["Game_DefaultPlayerName"];
        _profileUsername = profileAccount?.Username ?? currentUsername;
        _profileAccountId = profileAccount?.AccountId;
        _isOwnProfile = ownProfileRequested
            || _profileAccountId == ClientRuntime.CurrentSession.AccountId;
        _profileImageSelectionService = new ProfileImageSelectionService(
            LocalizationManager.Instance, _dialogService);
        usernameTextBlock.Text = _profileUsername;
        _defaultProfileBackground = profilePictureButton.Background;
        if (!_isOwnProfile)
        {
            ConfigurePublicProfile();
            return;
        }

        if (ClientRuntime.CurrentSession.Username != null)
        {
            usernameTextBlock.Text = ClientRuntime.CurrentSession.Username;
        }

        if (ClientRuntime.CurrentSession.Email != null)
        {
            emailTextBlock.Text = ClientRuntime.CurrentSession.Email;
        }

        if (ClientRuntime.CurrentSession.ProfilePicture != null)
        {
            profilePictureButton.Background = new ImageBrush(
                ClientRuntime.CurrentSession.ProfilePicture)
            {
                Stretch = Stretch.UniformToFill
            };
        }

    }

    private PlayerProfileViewModel ProfileViewModel
    {
        get
        {
            return _profileViewModel
                ?? throw new InvalidOperationException("The profile page has not been activated.");
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _friendshipsViewModel.IsActive = false;
        if (_profileViewModel != null)
        {
            _profileViewModel.PropertyChanged -= OnProfileChanged;
            _profileViewModel.Deactivate();
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _friendshipsViewModel.IsActive = true;
        if (ClientRuntime.CurrentSession.IsGuest
            || string.IsNullOrWhiteSpace(ClientRuntime.CurrentSession.SessionToken))
        {
            return;
        }
        PlayerProfileViewModel viewModel = new PlayerProfileViewModel(
            ClientRuntime.ProfileService, ClientRuntime.CurrentSession, _profileAccountId);
        _profileViewModel = viewModel;
        viewModel.PropertyChanged += OnProfileChanged;
        usernameTextBlock.SetBinding(TextBlock.TextProperty,
            new Binding(nameof(PlayerProfileViewModel.Username)) { Source = viewModel });
        emailTextBlock.SetBinding(TextBlock.TextProperty,
            new Binding(nameof(PlayerProfileViewModel.Email)) { Source = viewModel });
        try
        {
            ProfileStatus status = await viewModel.LoadProfile();
            if (IsLoaded && viewModel.IsCurrentSession() && status != ProfileStatus.Success)
            {
                ShowProfileLoadError(status == ProfileStatus.ServiceUnavailable);
            }
        }
        catch (Exception exception) when (RemoteCall.IsDeclaredFault(exception)
            || exception is IOException or NotSupportedException or FormatException)
        {
            ReportProfileFailure(exception, nameof(PlayerProfileViewModel.LoadProfile), viewModel);
        }
    }

    private void OnProfileChanged(object? sender, PropertyChangedEventArgs e)
    {
        PlayerProfileViewModel viewModel = ProfileViewModel;
        modifyUsernameButton.IsEnabled = viewModel.CanEdit;
        profilePictureButton.IsEnabled = viewModel.CanEdit;
        profilePictureButton.Background = viewModel.Picture == null
            ? _defaultProfileBackground
            : new ImageBrush(viewModel.Picture) { Stretch = Stretch.UniformToFill };
        if (viewModel.IsReady)
        {
            playerStatisticsControl.SetStatistics(viewModel.Statistics);
        }
        else
        {
            playerStatisticsControl.ClearStatistics();
        }
    }

    private void ReportProfileFailure(Exception exception, string operation,
        PlayerProfileViewModel viewModel)
    {
        if (IsLoaded && ReferenceEquals(viewModel, _profileViewModel)
            && viewModel.IsCurrentSession())
        {
            ClientRuntime.Errors.ReportOperationFailure(exception, operation);
        }
        else
        {
            ClientRuntime.Errors.RecordFailure(exception, operation, LogLevel.Error);
        }
    }

    private void ShowProfileLoadError(bool connectionFailed)
    {
        string resourcePrefix = connectionFailed
            ? "PlayerStatistics_ServerConnectionFailed" : "PlayerStatistics_RetrievalError";
        _dialogService.ShowError(this, resourcePrefix + "Title", resourcePrefix + "Message");
    }

    private void ConfigurePublicProfile()
    {
        privateAccountPanel.Visibility = Visibility.Collapsed;
        modifyUsernameButton.Visibility = Visibility.Collapsed;
        profilePictureButton.IsEnabled = false;
        deactivateAccountButton.Visibility = Visibility.Collapsed;
        sendFriendRequestButton.Visibility = _returnTarget ==
            AccountInformationReturnTarget.Friends
                ? Visibility.Collapsed
                : Visibility.Visible;
        UpdatePublicProfileText();
    }

    private void UpdatePublicProfileText()
    {
        if (_isOwnProfile)
        {
            return;
        }

        profileTitleTextBlock.Text = LocalizationManager.Instance[
            "AccountInformation_PublicProfileTitle"];
        if (_returnTarget == AccountInformationReturnTarget.AddFriends)
        {
            backToMenuButton.Content = LocalizationManager.Instance[
                "AccountInformation_BackToFriendsButton"];
        }
    }

    private async void OnModifyUsernameClick(object sender, RoutedEventArgs e)
    {
        PlayerProfileViewModel viewModel = ProfileViewModel;
        if (!viewModel.CanEdit)
        {
            return;
        }
        string currentUsername = viewModel.Username;
        TextInputDialogOptions options = new TextInputDialogOptions(
            "UsernameModification_DialogTitle", "UsernameModification_DialogMessage",
            "UsernameModification_DialogLabel")
        {
            ConfirmButtonKey = "UsernameModification_ConfirmButton",
            MessageArguments = new object[] { currentUsername },
            RequireText = false,
            MaximumLength = UsernamePolicy.MaximumLength
        };
        while (_dialogService.TryGetText(this, options, out string enteredUsername))
        {
            try
            {
                ProfileStatus status = await viewModel.ChangeUsername(enteredUsername);
                if (!IsLoaded || !viewModel.IsCurrentSession())
                {
                    return;
                }
                if (status == ProfileStatus.InvalidUsername)
                {
                    ShowUsernameValidationWarning(viewModel.UsernameValidation);
                    options.InitialText = enteredUsername;
                    continue;
                }
                if (status == ProfileStatus.UsernameUnavailable)
                {
                    _dialogService.ShowWarning(this, "AccountCreation_UsernameAlreadyTakenTitle",
                        "AccountCreation_UsernameAlreadyTakenMessage");
                }
                else if (status == ProfileStatus.Success)
                {
                    _dialogService.ShowInformation(this, "UsernameModification_SuccessTitle",
                        "UsernameModification_SuccessMessage");
                }
                else
                {
                    ShowProfileLoadError(status == ProfileStatus.ServiceUnavailable);
                }
            }
            catch (CommunicationException exception) when (RemoteCall.IsDeclaredFault(exception))
            {
                ReportProfileFailure(exception, nameof(PlayerProfileViewModel.ChangeUsername), viewModel);
            }
            return;
        }
    }

    private void ShowUsernameValidationWarning(UsernameValidationError validationError)
    {
        string resourcePrefix = validationError == UsernameValidationError.Missing
            ? "UsernameModification_Missing"
            : "UsernameModification_InvalidFormat";
        _dialogService.ShowWarning(
            this,
            resourcePrefix + "Title",
            resourcePrefix + "Message");
    }

    private void OnChangePasswordClick(object sender, RoutedEventArgs e)
    {
        if (_dialogService.TryChangePassword(this))
        {
            _dialogService.ShowInformation(
                this,
                "PasswordModification_SuccessTitle",
                "PasswordModification_SuccessMessage");
        }
    }

    private async void OnProfilePictureClick(object sender, RoutedEventArgs e)
    {
        PlayerProfileViewModel viewModel = ProfileViewModel;
        if (!viewModel.CanEdit)
        {
            return;
        }
        ProfileImageSelection? selection = _profileImageSelectionService.Select(
            this, "AccountInformation");
        if (selection == null)
        {
            return;
        }
        try
        {
            ProfileStatus status = await viewModel.UpdateProfileImage(selection.FilePath);
            if (!IsLoaded || !viewModel.IsCurrentSession())
            {
                return;
            }
            if (status != ProfileStatus.Success)
            {
                string prefix = status == ProfileStatus.ServiceUnavailable
                    ? "ProfileImageModification_ServerConnectionFailed"
                    : "ProfileImageModification_PersistenceError";
                _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
                return;
            }
            _dialogService.ShowInformation(this, "ProfileImageModification_SuccessTitle",
                "ProfileImageModification_SuccessMessage");
        }
        catch (Exception exception) when (RemoteCall.IsDeclaredFault(exception)
            || exception is IOException or UnauthorizedAccessException
                or NotSupportedException or FormatException)
        {
            ReportProfileFailure(exception, nameof(PlayerProfileViewModel.UpdateProfileImage), viewModel);
        }
    }

    private void OnBackToMenuClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new MainMenuPage());
    }

    private async void OnSendFriendRequestClick(object sender, RoutedEventArgs e)
    {
        if (!_profileAccountId.HasValue || _isOwnProfile || _friendshipsViewModel.IsSubmitting)
        {
            return;
        }
        IsEnabled = false;
        try
        {
            FriendshipStatus status = await _friendshipsViewModel.SendFriendRequest(
                _profileAccountId.Value);
            if (!IsLoaded)
            {
                return;
            }
            if (status == FriendshipStatus.Success)
            {
                _dialogService.ShowInformation(this, "FriendRequest_SentTitle",
                    "FriendRequest_SentMessage", _profileUsername);
            }
            else if (status != FriendshipStatus.SubmissionInProgress)
            {
                _dialogService.ShowFriendshipFailure(this, status);
            }
        }
        catch (FaultException<ServerProxy.ServiceFault> exception)
        {
            if (IsLoaded)
            {
                ClientRuntime.Errors.ReportOperationFailure(
                    exception, nameof(AccountInformationPage));
            }
            else
            {
                ClientRuntime.Errors.RecordFailure(
                    exception, nameof(AccountInformationPage), LogLevel.Error);
            }
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private async void OnDeactivateAccountClick(object sender, RoutedEventArgs e)
    {
        if (_accountViewModel.IsSubmitting || !_isOwnProfile)
        {
            return;
        }
        if (!_dialogService.Confirm(this, "AccountDeactivation_ConfirmationTitle",
            "AccountDeactivation_ConfirmationMessage"))
        {
            return;
        }
        CurrentPasswordDialogWindow passwordDialog = new CurrentPasswordDialogWindow(
            LocalizationManager.Instance) { Owner = Window.GetWindow(this) };
        if (passwordDialog.ShowDialog() != true)
        {
            return;
        }
        IsEnabled = false;
        try
        {
            AccountStatus status = await _accountViewModel.DeactivateAccount(
                passwordDialog.CurrentPassword);
            if (!IsLoaded)
            {
                return;
            }
            if (status == AccountStatus.Success)
            {
                _dialogService.ShowInformation(this, "AccountDeactivation_SuccessTitle",
                    "AccountDeactivation_SuccessMessage");
                NavigationService?.Navigate(new LoginScreenPage());
            }
            else
            {
                _dialogService.ShowAccountFailure(this, status);
            }
        }
        catch (FaultException<ServerProxy.ServiceFault> exception)
        {
            if (IsLoaded)
            {
                ClientRuntime.Errors.ReportOperationFailure(
                    exception, nameof(AccountInformationPage));
            }
            else
            {
                ClientRuntime.Errors.RecordFailure(
                    exception, nameof(AccountInformationPage), LogLevel.Error);
            }
        }
        finally
        {
            passwordDialog.ClearPassword();
            IsEnabled = true;
        }
    }

    private async void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string previousCulture = LocalizationManager.Instance.CultureName;
        await LanguageChangeService.ConfirmSelection((ComboBox)sender);
        if (LocalizationManager.Instance.CultureName != previousCulture)
        {
            UpdatePublicProfileText();
        }
    }
}
