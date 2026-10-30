using System;
using System.Collections.Generic;
using System.Globalization;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays main navigation and server-provided player lists.
/// </summary>
public partial class MainMenuPage : Page
{
    private readonly string? _guestName;
    private readonly DialogService _dialogService =
        new DialogService(LocalizationManager.Instance);
    private readonly InstructionsService _instructionsService =
        new InstructionsService(key => LocalizationManager.Instance[key]);
    private bool _friendsLoading;
    private bool _friendsSubscribed;
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
        matchHistoryButton.Visibility = Visibility.Collapsed;
        myProfileButton.Visibility = Visibility.Collapsed;
        settingsButton.Visibility = Visibility.Collapsed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_guestName == null)
        {
            SubscribeFriends();
            await LoadFriendsAsync();
        }

        if (!_leaderboardLoaded)
        {
            await LoadLeaderboardAsync();
        }
    }

    private async System.Threading.Tasks.Task LoadLeaderboardAsync()
    {
        _leaderboardLoaded = true;
        Proxy.LeaderboardEntry[] entries;
        try
        {
            entries = await ClientRuntime.LeaderboardService.GetLeaderboardAsync(10);
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            _leaderboardLoaded = false;
            ShowLeaderboardError();
            return;
        }

        CultureInfo culture = CultureInfo.GetCultureInfo(
            LocalizationManager.Instance.CultureName);
        List<LeaderboardItem> items = new List<LeaderboardItem>();
        foreach (Proxy.LeaderboardEntry entry in entries)
        {
            items.Add(CreateLeaderboardItem(entry, culture));
        }

        topWinnersItemsControl.ItemsSource = items;
        topWinnersEmptyTextBlock.Visibility = items.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static LeaderboardItem CreateLeaderboardItem(
        Proxy.LeaderboardEntry entry,
        CultureInfo culture)
    {
        return new LeaderboardItem
        {
            Position = "#" + entry.Position.ToString("N0", culture),
            Username = entry.Account.Username,
            WinsText = string.Format(
                culture,
                LocalizationManager.Instance["MainMenu_TopWinnersWinsFormat"],
                entry.Wins)
        };
    }

    private void ShowLeaderboardError()
    {
        topWinnersItemsControl.ItemsSource = null;
        topWinnersEmptyTextBlock.Visibility = Visibility.Collapsed;
        _dialogService.ShowError(
            this,
            "MainMenu_TopWinnersUnavailableTitle",
            "MainMenu_TopWinnersUnavailableMessage");
    }

    private void SubscribeFriends()
    {
        if (_friendsSubscribed)
        {
            return;
        }
        ClientRuntime.FriendshipService.FriendsChanged += OnFriendsChanged;
        ClientRuntime.FriendshipService.ConnectionLost += OnFriendshipConnectionLost;
        _friendsSubscribed = true;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_friendsSubscribed)
        {
            ClientRuntime.FriendshipService.FriendsChanged -= OnFriendsChanged;
            ClientRuntime.FriendshipService.ConnectionLost -= OnFriendshipConnectionLost;
            _friendsSubscribed = false;
        }
    }

    private async System.Threading.Tasks.Task LoadFriendsAsync()
    {
        if (_friendsLoading)
        {
            return;
        }
        _friendsLoading = true;
        refreshFriendsButton.IsEnabled = false;
        friendsStatusTextBlock.Text = LocalizationManager.Instance["Friends_Loading"];
        try
        {
            await ClientRuntime.FriendshipService.GetFriendsAsync(
                AccountPreviewState.SessionToken ?? string.Empty);
            if (IsLoaded)
            {
                RenderFriends();
            }
        }
        catch (FaultException<Proxy.SessionFault>)
        {
            ShowFriendsUnavailable("Friends_SessionUnavailable");
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception)
            || exception is ArgumentException)
        {
            ShowFriendsUnavailable("Friends_PresenceUnavailable");
        }
        finally
        {
            _friendsLoading = false;
            refreshFriendsButton.IsEnabled = true;
        }
    }

    private async void OnRefreshFriendsClick(object sender, RoutedEventArgs e) => await LoadFriendsAsync();

    private void OnFriendsChanged() => Dispatcher.BeginInvoke(new Action(() =>
    {
        if (_friendsSubscribed && !_friendsLoading)
        {
            RenderFriends();
        }
    }));

    private void OnFriendshipConnectionLost() => Dispatcher.BeginInvoke(new Action(() =>
    {
        if (_friendsSubscribed)
        {
            ShowFriendsUnavailable("Friends_PresenceUnavailable");
        }
    }));

    private void ShowFriendsUnavailable(string key)
    {
        friendsItemsControl.ItemsSource = null;
        friendsEmptyTextBlock.Visibility = Visibility.Collapsed;
        friendsStatusTextBlock.Text = LocalizationManager.Instance[key];
    }

    private void RenderFriends()
    {
        if (!ClientRuntime.FriendshipService.IsConnected)
        {
            ShowFriendsUnavailable("Friends_PresenceUnavailable");
            return;
        }
        List<FriendItem> items = new List<FriendItem>();
        foreach (Proxy.FriendSummary friend in ClientRuntime.FriendshipService.GetSnapshot())
        {
            items.Add(new FriendItem(friend));
        }
        friendsItemsControl.ItemsSource = items;
        friendsEmptyTextBlock.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        friendsStatusTextBlock.Text = string.Empty;
    }

    private async void OnInviteFriendClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button
            || button.Tag is not long friendAccountId
            || button.DataContext is not FriendItem friend)
        {
            return;
        }

        string initialText = TurnTimePolicy.DefaultSeconds.ToString();
        int turnDurationSeconds;
        while (true)
        {
            TextInputDialogOptions options = new TextInputDialogOptions(
                "InviteMatch_DialogTitle",
                "InviteMatch_DialogPrompt",
                "MatchSettings_TurnTimeLabel")
            {
                ConfirmButtonKey = "InviteMatch_Button",
                RequireText = true,
                InitialText = initialText,
                MessageArguments = new object[] { friend.Username }
            };

            if (!_dialogService.TryGetText(this, options, out string enteredText))
            {
                return;
            }

            initialText = enteredText;
            if (!TurnTimePolicy.TryParse(enteredText, out turnDurationSeconds))
            {
                _dialogService.ShowWarning(
                    this,
                    "MatchCreation_InvalidTurnTimeTitle",
                    "MatchCreation_InvalidTurnTimeMessage");
                continue;
            }

            break;
        }

        IsEnabled = false;
        try
        {
            RemoteMatchClient client = ClientRuntime.GetMatchClient(guest: false);
            Proxy.LobbySnapshot lobby = await client.InviteAsync(friendAccountId, turnDurationSeconds);
            NavigationService?.Navigate(new WaitingForOpponentPage(client, lobby, guestName: null));
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            _dialogService.ShowError(this, "InviteMatch_ErrorTitle",
                RemoteMatchMessages.Key(exception, guest: false));
        }
        finally
        {
            IsEnabled = true;
        }
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

        ClientRuntime.FriendshipService.Close();
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
            if (_guestName == null)
            {
                RenderFriends();
            }
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

    private sealed class LeaderboardItem
    {
        public string Position { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string WinsText { get; set; } = string.Empty;
    }
}
