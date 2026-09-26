using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays the main navigation and placeholders for player lists.
/// </summary>
public partial class MainMenuPage : Page
{
    private readonly string? _guestName;
    private readonly DialogService _dialogService =
        new DialogService(LocalizationManager.Instance);
    private readonly InstructionsService _instructionsService =
        new InstructionsService(key => LocalizationManager.Instance[key]);
    private bool _friendsLoaded;

    /// <summary>
    /// Initializes the main menu page.
    /// </summary>
    public MainMenuPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the menu for a guest with a temporary display name.
    /// </summary>
    /// <param name="guestName">The name shown while the guest is in the application.</param>
    public MainMenuPage(string guestName) : this()
    {
        _guestName = guestName;
        UpdateGuestIdentity();
        guestIdentityPanel.Visibility = Visibility.Visible;
        friendsCardBorder.Visibility = Visibility.Collapsed;
        addFriendButton.IsEnabled = false;
        matchHistoryButton.Visibility = Visibility.Collapsed;
        myProfileButton.Visibility = Visibility.Collapsed;
        settingsButton.Visibility = Visibility.Collapsed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_guestName == null && !_friendsLoaded)
        {
            LoadFriends();
        }
    }

    private void LoadFriends()
    {
        _friendsLoaded = true;
        string username = AccountPreviewState.Username
            ?? LocalizationManager.Instance["Game_DefaultPlayerName"];
        FriendsService service = new FriendsService(
            username,
            FriendshipPreviewState.GetStore(username));
        FriendsRetrievalResult result = service.TryGetFriends(
            out IReadOnlyList<FriendCandidate> friends);
        if (result != FriendsRetrievalResult.Success)
        {
            friendsItemsControl.ItemsSource = null;
            friendsEmptyTextBlock.Visibility = Visibility.Collapsed;
            string prefix = result == FriendsRetrievalResult.ServerConnectionFailed
                ? "Friends_ServerConnectionFailed"
                : "Friends_RetrievalError";
            _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
            return;
        }

        List<FriendItem> items = new List<FriendItem>();
        foreach (FriendCandidate friend in friends)
        {
            items.Add(new FriendItem { Username = friend.Username });
        }

        friendsItemsControl.ItemsSource = items;
        friendsEmptyTextBlock.Visibility = items.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnCreateGameClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(_guestName == null
            ? new MatchCreationPage()
            : new MatchCreationPage(_guestName));
    }

    private void OnJoinGameClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(_guestName == null
            ? new AvailableMatchesPage()
            : new AvailableMatchesPage(_guestName));
    }

    private void OnMatchHistoryClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new MatchHistoryPage());
    }

    private void OnAddFriendClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new AddFriendsPage());
    }

    private void OnViewFriendRequestsClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new FriendRequestsPage());
    }

    private void OnFriendProfileClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string username)
        {
            NavigationService?.Navigate(new AccountInformationPage(
                username,
                AccountInformationReturnTarget.Friends));
        }
    }

    private void OnRemoveFriendClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string friendUsername)
        {
            return;
        }

        ConfirmationDialogOptions options = new ConfirmationDialogOptions(
            "FriendRemoval_ConfirmationTitle",
            "FriendRemoval_ConfirmationMessage",
            "FriendRemoval_ConfirmButton",
            "Common_CancelButton")
        {
            MessageArguments = new object[] { friendUsername }
        };
        if (!_dialogService.Confirm(this, options))
        {
            return;
        }

        string username = AccountPreviewState.Username
            ?? LocalizationManager.Instance["Game_DefaultPlayerName"];
        FriendRemovalService service = new FriendRemovalService(
            username,
            FriendshipPreviewState.GetStore(username));
        HandleFriendRemoval(service.Remove(friendUsername), friendUsername);
    }

    private void HandleFriendRemoval(
        FriendRemovalResult result,
        string friendUsername)
    {
        switch (result)
        {
            case FriendRemovalResult.Success:
                _dialogService.ShowInformation(
                    this,
                    "FriendRemoval_SuccessTitle",
                    "FriendRemoval_SuccessMessage",
                    friendUsername);
                LoadFriends();
                break;
            case FriendRemovalResult.NotFound:
                _dialogService.ShowInformation(
                    this,
                    "FriendRemoval_NotFoundTitle",
                    "FriendRemoval_NotFoundMessage");
                LoadFriends();
                break;
            case FriendRemovalResult.ServerConnectionFailed:
                _dialogService.ShowError(
                    this,
                    "FriendRemoval_ServerConnectionFailedTitle",
                    "FriendRemoval_ServerConnectionFailedMessage");
                break;
            case FriendRemovalResult.PersistenceError:
                _dialogService.ShowError(
                    this,
                    "FriendRemoval_PersistenceErrorTitle",
                    "FriendRemoval_PersistenceErrorMessage");
                break;
        }
    }

    private void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        GuestSessionPreviewState.EndCurrent();
        AccountPreviewState.Clear();
        NavigationService?.Navigate(new LoginScreenPage());
    }

    private void OnMyProfileClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new AccountInformationPage());
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new SettingsPage());
    }

    private void OnInstructionsClick(object sender, RoutedEventArgs e)
    {
        InstructionsLoadResult result = _instructionsService.TryLoadGeneral(
            out InstructionsContent? content);
        if (result != InstructionsLoadResult.Success || content == null)
        {
            _dialogService.ShowError(
                this,
                "Instructions_LoadingErrorTitle",
                "Instructions_LoadingErrorMessage");
            return;
        }

        _dialogService.ShowInformationText(this, content.Title, content.Message);
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string previousCulture = LocalizationManager.Instance.CultureName;
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
        if (LocalizationManager.Instance.CultureName != previousCulture)
        {
            UpdateGuestIdentity();
        }
    }

    private void UpdateGuestIdentity()
    {
        if (_guestName == null)
        {
            return;
        }

        string displayName = GuestNameFormatter.Format(_guestName);
        guestNameTextBlock.Text = displayName;
        guestNameTextBlock.ToolTip = displayName;
    }

    private sealed class FriendItem
    {
        public string Username { get; set; } = string.Empty;
    }
}
