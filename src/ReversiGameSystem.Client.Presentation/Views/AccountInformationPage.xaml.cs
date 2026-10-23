using System;
using System.IO;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Views;

internal enum AccountInformationReturnTarget
{
    MainMenu,
    AddFriends,
    Friends
}

/// <summary>
/// Displays account details and player statistics.
/// </summary>
public partial class AccountInformationPage : Page
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private readonly ProfileImageSelectionService _profileImageSelectionService;
    private readonly ProfileImageModificationService _profileImageService =
        new ProfileImageModificationService();
    private readonly IAccountDeactivationService _accountDeactivationService =
        new AccountDeactivationService();
    private readonly AccountInformationReturnTarget _returnTarget;
    private readonly string _profileUsername;
    private readonly bool _isOwnProfile;
    private readonly FriendRequestService _friendRequestService;

    /// <summary>
    /// Initializes the account information page.
    /// </summary>
    public AccountInformationPage()
        : this(null, AccountInformationReturnTarget.MainMenu, true)
    {
    }

    internal AccountInformationPage(
        string profileUsername,
        AccountInformationReturnTarget returnTarget)
        : this(profileUsername, returnTarget, false)
    {
    }

    private AccountInformationPage(
        string? profileUsername,
        AccountInformationReturnTarget returnTarget,
        bool ownProfileRequested)
    {
        InitializeComponent();
        Loaded += OnLoaded;
        _returnTarget = returnTarget;
        string currentUsername = AccountPreviewState.Username
            ?? LocalizationManager.Instance["Game_DefaultPlayerName"];
        _profileUsername = profileUsername ?? currentUsername;
        _isOwnProfile = ownProfileRequested || string.Equals(
            _profileUsername,
            currentUsername,
            System.StringComparison.OrdinalIgnoreCase);
        _friendRequestService = FriendshipPreviewState.GetService(currentUsername);
        _profileImageSelectionService = new ProfileImageSelectionService(
            LocalizationManager.Instance, _dialogService);
        usernameTextBlock.Text = _profileUsername;
        playerStatisticsControl.PlayerId = _isOwnProfile ? null : _profileUsername;
        if (!_isOwnProfile)
        {
            ConfigurePublicProfile();
            return;
        }

        if (AccountPreviewState.Username != null)
        {
            usernameTextBlock.Text = AccountPreviewState.Username;
        }

        if (AccountPreviewState.Email != null)
        {
            emailTextBlock.Text = AccountPreviewState.Email;
        }

        if (AccountPreviewState.ProfilePicture != null)
        {
            profilePictureButton.Background = new ImageBrush(AccountPreviewState.ProfilePicture)
            {
                Stretch = Stretch.UniformToFill
            };
        }

    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        string? sessionToken = AccountPreviewState.SessionToken;
        if (!_isOwnProfile
            || sessionToken == null
            || string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        try
        {
            Proxy.PlayerProfile profile = await ClientRuntime.ProfileService.GetPlayerProfileAsync(sessionToken);
            ApplyProfile(profile);
            if (profile.HasProfileImage)
            {
                Proxy.ImageContent? image = await ClientRuntime.ProfileService.GetProfileImageAsync(sessionToken);
                System.Windows.Media.Imaging.BitmapSource? bitmap = ProfileImageModificationService.Decode(image);
                if (bitmap != null)
                {
                    AccountPreviewState.UpdateProfilePicture(bitmap);
                    profilePictureButton.Background = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
                }
            }
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception)
            || exception is IOException or NotSupportedException or ArgumentException or FormatException)
        {
            ShowProfileLoadError(exception is not FaultException && (exception is CommunicationException or TimeoutException));
        }
    }

    private void ApplyProfile(Proxy.PlayerProfile profile)
    {
        AccountPreviewState.UpdateUsername(profile.Account.Username);
        usernameTextBlock.Text = profile.Account.Username;
        playerStatisticsControl.SetStatistics(profile.Statistics);
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
        string currentUsername = AccountPreviewState.Username
            ?? LocalizationManager.Instance["AccountInformation_UnavailableValue"];
        TextInputDialogOptions options = new TextInputDialogOptions(
            "UsernameModification_DialogTitle",
            "UsernameModification_DialogMessage",
            "UsernameModification_DialogLabel")
        {
            ConfirmButtonKey = "UsernameModification_ConfirmButton",
            MessageArguments = new object[] { currentUsername },
            RequireText = false
        };

        while (_dialogService.TryGetText(this, options, out string enteredUsername))
        {
            string username = enteredUsername.Trim();
            UsernameValidationError validationError = UsernamePolicy.Validate(username);
            if (validationError != UsernameValidationError.None)
            {
                ShowUsernameValidationWarning(validationError);
                options.InitialText = enteredUsername;
                continue;
            }

            try
            {
                modifyUsernameButton.IsEnabled = false;
                await ClientRuntime.ProfileService.ChangeUsernameAsync(
                    AccountPreviewState.SessionToken ?? string.Empty, username);
            }
            catch (FaultException<Proxy.ProfileFault> fault)
            {
                if (fault.Detail.Reason == Proxy.ProfileFailure.UsernameUnavailable
                    || fault.Detail.Reason == Proxy.ProfileFailure.InvalidUsername)
                {
                    string prefix = fault.Detail.Reason == Proxy.ProfileFailure.UsernameUnavailable
                        ? "AccountCreation_UsernameAlreadyTaken" : "UsernameModification_InvalidFormat";
                    _dialogService.ShowWarning(this, prefix + "Title", prefix + "Message");
                }
                else
                {
                    ShowProfileLoadError(false);
                }
                return;
            }
            catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
            {
                ShowProfileLoadError(exception is not FaultException && (exception is CommunicationException or TimeoutException));
                return;
            }
            finally
            {
                modifyUsernameButton.IsEnabled = true;
            }
            AccountPreviewState.UpdateUsername(username);
            usernameTextBlock.Text = username;
            _dialogService.ShowInformation(
                this,
                "UsernameModification_SuccessTitle",
                "UsernameModification_SuccessMessage");
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
        if (!_profileImageSelectionService.TrySelect(
            this, "AccountInformation", out ProfileImageSelection? selection)
            || selection == null)
        {
            return;
        }

        try
        {
            profilePictureButton.IsEnabled = false;
            await _profileImageService.SaveAsync(AccountPreviewState.SessionToken ?? string.Empty, selection.FilePath);
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception)
            || exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            string prefix = exception is not FaultException && (exception is CommunicationException or TimeoutException)
                ? "ProfileImageModification_ServerConnectionFailed" : "ProfileImageModification_PersistenceError";
            _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
            return;
        }
        finally
        {
            profilePictureButton.IsEnabled = true;
        }

        AccountPreviewState.UpdateProfilePicture(selection.Preview);
        profilePictureButton.Background = new ImageBrush(selection.Preview)
        {
            Stretch = Stretch.UniformToFill
        };
        _dialogService.ShowInformation(
            this,
            "ProfileImageModification_SuccessTitle",
            "ProfileImageModification_SuccessMessage");
    }

    private void OnBackToMenuClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(_returnTarget == AccountInformationReturnTarget.AddFriends
            ? new AddFriendsPage()
            : new MainMenuPage());
    }

    private void OnSendFriendRequestClick(object sender, RoutedEventArgs e)
    {
        ShowFriendRequestResult(_friendRequestService.Send(_profileUsername));
    }

    private void ShowFriendRequestResult(FriendRequestResult result)
    {
        if (result == FriendRequestResult.Success)
        {
            _dialogService.ShowInformation(
                this,
                "FriendRequest_SentTitle",
                "FriendRequest_SentMessage",
                _profileUsername);
            return;
        }

        string prefix = result == FriendRequestResult.CannotAddYourself
            ? "FriendRequest_CannotAddYourself"
            : result == FriendRequestResult.FriendshipAlreadyExists
                ? "FriendRequest_FriendshipAlreadyExists"
                : result == FriendRequestResult.FriendRequestAlreadyPending
                    ? "FriendRequest_AlreadyPending"
                    : result == FriendRequestResult.AccountNotAvailable
                        ? "FriendRequest_AccountNotAvailable"
                        : result == FriendRequestResult.PersistenceError
                            ? "FriendRequest_PersistenceError"
                            : "FriendRequest_ServerConnectionFailed";
        _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
    }

    private void OnDeactivateAccountClick(object sender, RoutedEventArgs e)
    {
        if (!_dialogService.Confirm(
            this,
            "AccountDeactivation_ConfirmationTitle",
            "AccountDeactivation_ConfirmationMessage"))
        {
            return;
        }

        AccountDeactivationResult result = _accountDeactivationService.DeactivateOwnAccount();
        switch (result)
        {
            case AccountDeactivationResult.Success:
                AccountPreviewState.Clear();
                _dialogService.ShowInformation(
                    this,
                    "AccountDeactivation_SuccessTitle",
                    "AccountDeactivation_SuccessMessage");
                NavigationService?.Navigate(new LoginScreenPage());
                break;
            case AccountDeactivationResult.ActiveMatch:
                _dialogService.ShowWarning(
                    this,
                    "AccountDeactivation_ActiveMatchTitle",
                    "AccountDeactivation_ActiveMatchMessage");
                break;
            case AccountDeactivationResult.PersistenceError:
                _dialogService.ShowError(
                    this,
                    "AccountDeactivation_PersistenceErrorTitle",
                    "AccountDeactivation_PersistenceErrorMessage");
                break;
            default:
                _dialogService.ShowError(
                    this,
                    "AccountDeactivation_ServerConnectionFailedTitle",
                    "AccountDeactivation_ServerConnectionFailedMessage");
                break;
        }
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string previousCulture = LocalizationManager.Instance.CultureName;
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
        if (LocalizationManager.Instance.CultureName != previousCulture)
        {
            UpdatePublicProfileText();
        }
    }
}
