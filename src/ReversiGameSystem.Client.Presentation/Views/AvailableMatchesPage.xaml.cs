using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using AvailableMatch = ReversiGameSystem.Matches.AvailableMatch;

namespace ReversiGameSystem.Views;

public partial class AvailableMatchesPage : Page
{
    private readonly string? _guestName;
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);

    private bool _loading;

    internal string? SelectedMatchId { get; private set; }

    public AvailableMatchesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadMatches();
    }

    public AvailableMatchesPage(string guestName) : this()
    {
        _guestName = guestName;
        UpdateGuestIdentity();
        guestIdentityPanel.Visibility = Visibility.Visible;
    }

    private void OnBackToMenuClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(_guestName == null
            ? new MainMenuPage()
            : new MainMenuPage(_guestName));
    }

    private async void OnJoinWithCodeClick(object sender, RoutedEventArgs e)
    {
        string initialText = string.Empty;
        while (true)
        {
            TextInputDialogOptions options = new TextInputDialogOptions(
                "AvailableMatches_CodeDialogTitle",
                "AvailableMatches_CodeDialogMessage",
                "AvailableMatches_CodeDialogLabel")
            {
                ConfirmButtonKey = "AvailableMatches_JoinButton",
                RequireText = true,
                MaximumLength = MatchCodePolicy.Length,
                InitialText = initialText
            };

            if (!_dialogService.TryGetText(this, options, out string enteredCode))
            {
                return;
            }

            initialText = enteredCode;
            if (!MatchCodePolicy.TryNormalize(enteredCode, out string code))
            {
                ShowJoinWarning("InvalidMatchCode");
                continue;
            }
            await JoinRemoteMatch(code);
            return;
        }
    }

    private async Task LoadMatches()
    {
        if (_loading)
        {
            return;
        }
        _loading = true;
        IsEnabled = false;
        LocalizationManager localization = LocalizationManager.Instance;
        List<AvailableMatchItem> items = new List<AvailableMatchItem>();
        MatchLobbyClient client = ClientRuntime.GetMatchLobbyClient(_guestName != null);
        MatchStatus status = await client.LoadAvailableMatches();
        _loading = false;
        IsEnabled = true;
        if (status != MatchStatus.Success)
        {
            string messageKey = RemoteMatchMessages.Key(status, _guestName != null);
            availableMatchesItemsControl.ItemsSource = null;
            emptyMatchesTextBlock.SetBinding(TextBlock.TextProperty, new Binding("[" + messageKey + "]")
            { Source = localization });
            _dialogService.ShowError(this, "AvailableMatches_RetrievalErrorTitle",
                messageKey);
            return;
        }
        AvailableMatch[] matches = client.ReadAvailableMatches();

        emptyMatchesTextBlock.SetBinding(TextBlock.TextProperty, new Binding("[AvailableMatches_EmptyMessage]")
        { Source = localization });
        foreach (AvailableMatch match in matches)
        {
            items.Add(new AvailableMatchItem
            {
                MatchId = match.MatchCode,
                HostName = match.CreatorDisplayName,
                CreatedAgoText = string.Format(
                    localization["AvailableMatches_TurnTimeFormat"],
                    match.TurnDurationSeconds),
                StateText = localization["AvailableMatches_WaitingState"]
            });
        }

        availableMatchesItemsControl.ItemsSource = items;
        spectatableMatchesItemsControl.ItemsSource = Array.Empty<AvailableMatchItem>();
    }

    private void OnRefreshMatchesClick(object sender, RoutedEventArgs e)
    {
        _ = LoadMatches();
    }

    private async void OnJoinMatchClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button
            || button.Tag is not string matchId
            || button.DataContext is not AvailableMatchItem item)
        {
            return;
        }

        SelectedMatchId = matchId;
        ConfirmationDialogOptions options = new ConfirmationDialogOptions(
            "JoinMatch_ConfirmationTitle",
            "JoinMatch_ConfirmationMessage",
            "AvailableMatches_JoinButton",
            "Common_CancelButton")
        {
            MessageArguments = new object[] { item.HostName }
        };
        if (!_dialogService.Confirm(this, options))
        {
            return;
        }

        await JoinRemoteMatch(matchId);
    }

    private async Task JoinRemoteMatch(string code)
    {
        IsEnabled = false;
        MatchLobbyClient client = ClientRuntime.GetMatchLobbyClient(_guestName != null);
        MatchStatus status = await client.JoinMatch(code);
        if (status == MatchStatus.Success)
        {
            LobbySnapshot lobby = client.ReadCurrentLobby();
            NavigationService?.Navigate(new GamePage(
                ClientRuntime.GetGameplayClient(_guestName != null),
                ClientRuntime.GetMatchChatClient(_guestName != null), lobby.MatchId, _guestName));
        }
        else
        {
            _dialogService.ShowError(this, "JoinMatch_PersistenceErrorTitle",
                RemoteMatchMessages.Key(status, _guestName != null));
            await LoadMatches();
        }
        IsEnabled = true;
    }

    private void ShowJoinWarning(string resourceName)
    {
        _dialogService.ShowWarning(
            this,
            $"JoinMatch_{resourceName}Title",
            $"JoinMatch_{resourceName}Message");
    }

    private void ShowJoinError(string resourceName)
    {
        _dialogService.ShowError(
            this,
            $"JoinMatch_{resourceName}Title",
            $"JoinMatch_{resourceName}Message");
    }

    private void OnSpectateMatchClick(object sender, RoutedEventArgs e)
    {
        _dialogService.ShowWarning(this, "AvailableMatches_RetrievalErrorTitle", "RemoteMatch_NotAvailable");
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string previousCulture = LocalizationManager.Instance.CultureName;
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
        if (LocalizationManager.Instance.CultureName != previousCulture)
        {
            UpdateGuestIdentity();
            _ = LoadMatches();
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

    private sealed class AvailableMatchItem
    {
        public string MatchId { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public string CreatedAgoText { get; set; } = string.Empty;
        public string StateText { get; set; } = string.Empty;
        public Visibility SpectateVisibility { get; set; }
    }
}
