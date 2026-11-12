using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        string currentUsername = ClientRuntime.CurrentSession.Username
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
            profilePictureButton.Background = new ImageBrush(ClientRuntime.CurrentSession.ProfilePicture)
            {
                Stretch = Stretch.UniformToFill
            };
        }

    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        string? sessionToken = ClientRuntime.CurrentSession.SessionToken;
        if (!_isOwnProfile
            || sessionToken == null
            || string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        ProfileResult<PlayerProfile> profileResult =
            await ClientRuntime.ProfileService.GetPlayerProfile(sessionToken);
        if (profileResult.Status != ProfileStatus.Success)
        {
            ShowProfileLoadError(profileResult.Status == ProfileStatus.ServiceUnavailable);
            return;
        }
        PlayerProfile profile = profileResult.Value
            ?? throw new InvalidOperationException(
                "A successful profile result must contain its value.");
        ApplyProfile(profile);
        if (!profile.HasProfileImage)
        {
            return;
        }
        ProfileResult<ProfileImageContent> imageResult =
            await ClientRuntime.ProfileService.GetProfileImage(sessionToken);
        if (imageResult.Status != ProfileStatus.Success)
        {
            ShowProfileLoadError(imageResult.Status == ProfileStatus.ServiceUnavailable);
            return;
        }
        try
        {
            System.Windows.Media.Imaging.BitmapSource? bitmap =
                ProfileImageModificationService.Decode(imageResult.Value);
            if (bitmap != null)
            {
                ClientRuntime.CurrentSession.UpdateProfilePicture(bitmap);
                profilePictureButton.Background = new ImageBrush(bitmap)
                {
                    Stretch = Stretch.UniformToFill
                };
            }
        }
        catch (Exception exception) when (exception is IOException
            or NotSupportedException or ArgumentException or FormatException)
        {
            ShowProfileLoadError(false);
        }
    }

    private void ApplyProfile(PlayerProfile profile)
    {
        ClientRuntime.CurrentSession.UpdateUsername(profile.Username);
        usernameTextBlock.Text = profile.Username;
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
        string currentUsername = ClientRuntime.CurrentSession.Username
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

            modifyUsernameButton.IsEnabled = false;
            ProfileStatus status = await ClientRuntime.ProfileService.ChangeUsername(
                ClientRuntime.CurrentSession.SessionToken ?? string.Empty, username);
            modifyUsernameButton.IsEnabled = true;
            if (status != ProfileStatus.Success)
            {
                if (status == ProfileStatus.UsernameUnavailable
                    || status == ProfileStatus.InvalidUsername)
                {
                    string prefix = status == ProfileStatus.UsernameUnavailable
                        ? "AccountCreation_UsernameAlreadyTaken" : "UsernameModification_InvalidFormat";
                    _dialogService.ShowWarning(this, prefix + "Title", prefix + "Message");
                }
                else
                {
                    ShowProfileLoadError(status == ProfileStatus.ServiceUnavailable);
                }
                return;
            }
            ClientRuntime.CurrentSession.UpdateUsername(username);
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
        ProfileImageSelection? selection = _profileImageSelectionService.Select(
            this, "AccountInformation");
        if (selection == null)
        {
            return;
        }

        try
        {
            profilePictureButton.IsEnabled = false;
            ProfileStatus status = await _profileImageService.Save(
                ClientRuntime.CurrentSession.SessionToken ?? string.Empty,
                selection.FilePath);
            if (status != ProfileStatus.Success)
            {
                string prefix = status == ProfileStatus.ServiceUnavailable
                    ? "ProfileImageModification_ServerConnectionFailed"
                    : "ProfileImageModification_PersistenceError";
                _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
                return;
            }
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException or NotSupportedException)
        {
            _dialogService.ShowError(
                this,
                "ProfileImageModification_PersistenceErrorTitle",
                "ProfileImageModification_PersistenceErrorMessage");
            return;
        }
        finally
        {
            profilePictureButton.IsEnabled = true;
        }

        ClientRuntime.CurrentSession.UpdateProfilePicture(selection.Preview);
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
                ClientRuntime.CurrentSession.Clear();
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
