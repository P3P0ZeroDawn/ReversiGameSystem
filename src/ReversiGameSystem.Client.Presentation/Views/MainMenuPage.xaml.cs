using System;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows.Data;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Features.Friendships;
using System.Resources;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

public partial class MainMenuPage : Page
{
    private const int LeaderboardEntryLimit = 10;

    private readonly string? _guestName;
    private readonly DialogService _dialogService =
        new DialogService(LocalizationManager.Instance);
    private readonly InstructionsService _instructionsService =
        new InstructionsService(key => LocalizationManager.Instance[key]);
    private readonly FriendshipViewModel _friendsViewModel = new FriendshipViewModel(
        ClientRuntime.FriendshipService, ClientRuntime.CurrentSession);
    private bool _friendsSubscribed;
    private bool _leaderboardLoaded;

    public MainMenuPage()
    {
        InitializeComponent();
        friendsItemsControl.SetBinding(ItemsControl.ItemsSourceProperty,
            new Binding(nameof(FriendshipViewModel.Friends)) { Source = _friendsViewModel });
    }

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
            _friendsViewModel.IsActive = true;
            SubscribeFriends();
            await LoadFriends();
        }

        if (!_leaderboardLoaded)
        {
            await LoadLeaderboard();
        }
    }

    private async System.Threading.Tasks.Task LoadLeaderboard()
    {
        LeaderboardResult result = await ClientRuntime.LeaderboardService.GetLeaderboard(
            LeaderboardEntryLimit);
        if (result.Status != LeaderboardStatus.Success)
        {
            _leaderboardLoaded = false;
            ShowLeaderboardError();
            return;
        }
        LeaderboardEntry[] entries = result.Entries;

        CultureInfo culture = CultureInfo.GetCultureInfo(
            LocalizationManager.Instance.CultureName);
        List<LeaderboardItem> items = new List<LeaderboardItem>();
        foreach (LeaderboardEntry entry in entries)
        {
            items.Add(CreateLeaderboardItem(entry, culture));
        }

        _leaderboardLoaded = true;
        topWinnersItemsControl.ItemsSource = items;
        topWinnersEmptyTextBlock.Visibility = items.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static LeaderboardItem CreateLeaderboardItem(
        LeaderboardEntry entry,
        CultureInfo culture)
    {
        return new LeaderboardItem
        {
            Position = "#" + entry.Position.ToString("N0", culture),
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
        ClientRuntime.FriendshipService.MembershipChanged += OnMembershipChanged;
        _friendsSubscribed = true;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _friendsViewModel.IsActive = false;
        if (_friendsSubscribed)
        {
            ClientRuntime.FriendshipService.FriendsChanged -= OnFriendsChanged;
            ClientRuntime.FriendshipService.ConnectionLost -= OnFriendshipConnectionLost;
            ClientRuntime.FriendshipService.MembershipChanged -= OnMembershipChanged;
            _friendsSubscribed = false;
        }
    }

    private async Task LoadFriends()
    {
        refreshFriendsButton.IsEnabled = false;
        friendsStatusTextBlock.Text = LocalizationManager.Instance["Friends_Loading"];
        try
        {
            FriendshipStatus status = await _friendsViewModel.LoadFriends();
            if (!IsLoaded)
            {
                return;
            }
            string unavailableKey = status switch
            {
                FriendshipStatus.Success => string.Empty,
                FriendshipStatus.InvalidSession or FriendshipStatus.MissingSessionToken
                    or FriendshipStatus.InactiveAccount => "Friends_SessionUnavailable",
                FriendshipStatus.ServiceUnavailable => "Friends_PresenceUnavailable",
                FriendshipStatus.SubmissionInProgress => "Friends_Loading",
                _ => throw new ArgumentOutOfRangeException(nameof(status))
            };
            if (status == FriendshipStatus.Success)
            {
                RenderFriends();
            }
            else
            {
                ShowFriendsUnavailable(unavailableKey);
            }
        }
        catch (FaultException<ServerProxy.ServiceFault> exception)
        {
            if (IsLoaded)
            {
                ClientRuntime.Errors.ReportOperationFailure(exception, nameof(MainMenuPage));
            }
            else
            {
                ClientRuntime.Errors.RecordFailure(exception, nameof(MainMenuPage), LogLevel.Error);
            }
        }
        finally
        {
            refreshFriendsButton.IsEnabled = !_friendsViewModel.IsLoadingFriends;
        }
    }

    private void OnMembershipChanged()
    {
        Dispatcher.BeginInvoke(new Action(OnMembershipInvalidated));
    }

    private async void OnMembershipInvalidated()
    {
        if (IsLoaded && _friendsSubscribed)
        {
            await LoadFriends();
        }
    }

    private async void OnRefreshFriendsClick(object sender, RoutedEventArgs e)
    {
        await LoadFriends();
    }

    private void OnFriendsChanged()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_friendsSubscribed)
            {
                RenderFriends();
            }
        }));
    }

    private void OnFriendshipConnectionLost()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_friendsSubscribed)
            {
                ShowFriendsUnavailable("Friends_PresenceUnavailable");
            }
        }));
    }

    private void ShowFriendsUnavailable(string key)
    {
        friendsListPanel.Visibility = Visibility.Collapsed;
        friendsEmptyTextBlock.Visibility = Visibility.Collapsed;
        friendsStatusTextBlock.Text = LocalizationManager.Instance[key];
    }

    private void RenderFriends()
    {
        if (!_friendsViewModel.IsConnected)
        {
            ShowFriendsUnavailable("Friends_PresenceUnavailable");
            return;
        }
        _friendsViewModel.RefreshConfirmedFriends();
        friendsListPanel.Visibility = Visibility.Visible;
        friendsEmptyTextBlock.Visibility = _friendsViewModel.Friends.Length == 0
            ? Visibility.Visible : Visibility.Collapsed;
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
            MatchLobbyClient client = ClientRuntime.GetMatchLobbyClient(guest: false);
            MatchStatus status =
                await client.InviteToMatch(friendAccountId, turnDurationSeconds);
            if (status == MatchStatus.Success)
            {
                LobbySnapshot lobby = client.ReadCurrentLobby();
                NavigationService?.Navigate(
                    new WaitingForOpponentPage(client, lobby, guestName: null));
            }
            else
            {
                _dialogService.ShowError(this, "InviteMatch_ErrorTitle",
                    RemoteMatchMessages.GetMessageResourceKey(status, guest: false));
            }
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void OnAddFriendsClick(object sender, RoutedEventArgs e)
    {
        AddFriendsWindow dialog = new AddFriendsWindow { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
        {
            PlayerAccount account = dialog.SelectedPlayer
                ?? throw new InvalidOperationException(
                    "A selected profile must contain its account.");
            NavigationService?.Navigate(new AccountInformationPage(
                account, AccountInformationReturnTarget.AddFriends));
        }
    }

    private void OnFriendRequestsClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new FriendRequestsPage());
    }

    private void OnFriendProfileClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is FriendItem friend)
        {
            PlayerAccount account = new PlayerAccount(
                friend.AccountId, friend.Username, friend.HasProfileImage);
            NavigationService?.Navigate(new AccountInformationPage(
                account, AccountInformationReturnTarget.Friends));
        }
    }

    private async void OnRemoveFriendClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not FriendItem friend
            || _friendsViewModel.IsSubmitting)
        {
            return;
        }
        ConfirmationDialogOptions options = new ConfirmationDialogOptions(
            "FriendRemoval_ConfirmationTitle", "FriendRemoval_ConfirmationMessage",
            "FriendRemoval_ConfirmButton", "Common_CancelButton")
        {
            MessageArguments = new object[] { friend.Username }
        };
        if (!_dialogService.Confirm(this, options))
        {
            return;
        }
        IsEnabled = false;
        try
        {
            FriendshipStatus status = await _friendsViewModel.RemoveFriend(friend.AccountId);
            if (!IsLoaded)
            {
                return;
            }
            if (status == FriendshipStatus.Success)
            {
                _dialogService.ShowInformation(this, "FriendRemoval_SuccessTitle",
                    "FriendRemoval_SuccessMessage", friend.Username);
                RenderFriends();
            }
            else if (status != FriendshipStatus.SubmissionInProgress)
            {
                _dialogService.ShowFriendshipFailure(this, status);
                if (status == FriendshipStatus.FriendshipNotFound)
                {
                    await LoadFriends();
                }
            }
        }
        catch (FaultException<ServerProxy.ServiceFault> exception)
        {
            if (IsLoaded)
            {
                ClientRuntime.Errors.ReportOperationFailure(
                    exception, nameof(MainMenuPage));
            }
            else
            {
                ClientRuntime.Errors.RecordFailure(
                    exception, nameof(MainMenuPage), LogLevel.Error);
            }
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
            string? sessionToken = ClientRuntime.CurrentSession.SessionToken;
            SessionStatus status = SessionStatus.Success;
            if (sessionToken != null && !string.IsNullOrWhiteSpace(sessionToken))
            {
                status = await ClientRuntime.SessionService.CloseSession(sessionToken);
            }
            if (status == SessionStatus.ServiceUnavailable)
            {
                _dialogService.ShowError(this,
                    "Session_CloseFailedTitle", "Session_CloseFailedMessage");
                return;
            }
            ClientRuntime.CurrentSession.Clear();
            NavigationService?.Navigate(new LoginScreenPage());
        }
        finally
        {
            IsEnabled = true;
        }
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
        try
        {
            InstructionsContent content = _instructionsService.LoadGeneral();
            _dialogService.ShowInformationText(this, content.Title, content.Message);
        }
        catch (Exception exception) when (exception is MissingManifestResourceException
            or MissingSatelliteAssemblyException)
        {
            ClientRuntime.Errors.ReportOperationFailure(exception, nameof(InstructionsService));
        }
    }

    private async void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string previousCulture = LocalizationManager.Instance.CultureName;
        await LanguageChangeService.ConfirmSelection((ComboBox)sender);
        if (LocalizationManager.Instance.CultureName != previousCulture)
        {
            UpdateGuestIdentity();
            if (_guestName == null)
            {
                RenderFriends();
            }
            await LoadLeaderboard();
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
