using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Contracts.Leaderboards;
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
    private bool _leaderboardLoaded;

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

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_guestName == null && !_friendsLoaded)
        {
            LoadFriends();
        }

        if (!_leaderboardLoaded)
        {
            await LoadLeaderboardAsync();
        }
    }

    private async System.Threading.Tasks.Task LoadLeaderboardAsync()
    {
        _leaderboardLoaded = true;
        LeaderboardResponse response = await ClientRuntime.Services.PlayerService
            .GetLeaderboardAsync(new LeaderboardRequest { MaximumEntries = 10 });
        if (response.ResultCode != LeaderboardResultCode.Success)
        {
            ShowLeaderboardError();
            return;
        }

        CultureInfo culture = CultureInfo.GetCultureInfo(
            LocalizationManager.Instance.CultureName);
        List<LeaderboardItem> items = new List<LeaderboardItem>();
        foreach (LeaderboardEntryDto entry in response.Entries)
        {
            items.Add(CreateLeaderboardItem(entry, culture));
        }

        topWinnersItemsControl.ItemsSource = items;
        topWinnersEmptyTextBlock.Visibility = items.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static LeaderboardItem CreateLeaderboardItem(
        LeaderboardEntryDto entry,
        CultureInfo culture)
    {
        return new LeaderboardItem
        {
            Position = "#" + entry.Rank.ToString("N0", culture),
            Username = entry.Username,
            WinsText = string.Format(
                culture,
                LocalizationManager.Instance["MainMenu_TopWinnersWinsFormat"],
                entry.Wins)
        };
    }

    private void ShowLeaderboardError()
    {
        topWinnersItemsControl.ItemsSource = null;
        topWinnersEmptyTextBlock.Visibility = Visibility.Visible;
        _dialogService.ShowError(
            this,
            "MainMenu_TopWinnersUnavailableTitle",
            "MainMenu_TopWinnersUnavailableMessage");
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

    private async void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        IsEnabled = false;
        try
        {
            string? token = AccountPreviewState.SessionToken ?? SessionState.GuestToken;
            if (token != null && !string.IsNullOrWhiteSpace(token))
            {
                await ClientRuntime.SessionService.CloseSessionAsync(token);
            }
        }
        catch (System.Exception exception) when (exception is System.ServiceModel.CommunicationException
            or System.TimeoutException or System.InvalidOperationException)
        {
            _dialogService.ShowError(this, "Session_CloseFailedTitle", "Session_CloseFailedMessage");
            return;
        }
        finally
        {
            IsEnabled = true;
        }

        SessionState.GuestToken = null;
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

    private async void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string previousCulture = LocalizationManager.Instance.CultureName;
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
        if (LocalizationManager.Instance.CultureName != previousCulture)
        {
            UpdateGuestIdentity();
            await LoadLeaderboardAsync();
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

    private sealed class LeaderboardItem
    {
        public string Position { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string WinsText { get; set; } = string.Empty;
    }
}
