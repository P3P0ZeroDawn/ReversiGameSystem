using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>Waits for a confirmed second participant on the remote lobby channel.</summary>
public partial class WaitingForOpponentPage : Page
{
    private readonly RemoteMatchClient _client;
    private readonly LobbySnapshot _lobby;
    private readonly string? _guestName;
    private readonly DialogService _dialogs =
        new DialogService(LocalizationManager.Instance);
    private readonly DispatcherTimer _keepalive = new DispatcherTimer
    {
        Interval = TimeSpan.FromSeconds(15)
    };
    private bool _navigated;
    private bool _refreshing;

    internal WaitingForOpponentPage(RemoteMatchClient client, LobbySnapshot lobby, string? guestName)
    {
        InitializeComponent();
        _client = client;
        _lobby = lobby;
        _guestName = guestName;
        matchCodeTextBlock.Text = lobby.MatchCode;
        UpdateTurnTimeText();
        modifySettingsButton.IsEnabled = false;
        playAgainstSystemButton.IsEnabled = false;
        modifySettingsButton.ToolTip = LocalizationManager.Instance["RemoteMatch_NotAvailable"];
        playAgainstSystemButton.ToolTip = LocalizationManager.Instance["RemoteMatch_NotAvailable"];
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        _keepalive.Tick += OnKeepalive;
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        _client.StateChanged += OnState;
        _client.LobbyClosed += OnClosed;
        _keepalive.Start();
        if (_client.HasMatchState(_lobby.MatchId))
        {
            OnState(_client.ReadMatchState(_lobby.MatchId));
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        _keepalive.Stop();
        _client.StateChanged -= OnState;
        _client.LobbyClosed -= OnClosed;
    }

    private void OnState(MatchStateSnapshot state)
    {
        if (state.MatchId != _lobby.MatchId || state.State != ConfirmedMatchState.InProgress)
        {
            return;
        }
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_navigated || !IsLoaded)
            {
                return;
            }
            _navigated = true;
            NavigationService?.Navigate(new GamePage(_client, _lobby.MatchId, _guestName));
        }));
    }

    private void OnClosed(long id)
    {
        if (id == _lobby.MatchId)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_navigated && IsLoaded)
                {
                    ReturnToMenu();
                }
            }));
        }
    }

    private async void OnKeepalive(object? sender, EventArgs args)
    {
        if (_refreshing)
        {
            return;
        }
        _refreshing = true;
        MatchResult<RemoteAvailableMatch[]> availableResult =
            await _client.LoadAvailableMatches();
        MatchResult<RemoteMatchInformation> informationResult =
            await _client.LoadRemoteMatchInformation(_lobby.MatchId);
        MatchResult<MatchStateSnapshot> stateResult =
            await _client.LoadMatchState(_lobby.MatchId);
        if (stateResult.Status == MatchStatus.Success && stateResult.Value != null)
        {
            OnState(stateResult.Value);
        }
        else if (stateResult.Status == MatchStatus.MatchNotInProgress)
        {
            // A confirmed waiting lobby remains on this screen until the server starts it.
        }
        else if (stateResult.Status == MatchStatus.MatchNotWaiting
            || informationResult.Status == MatchStatus.MatchNotWaiting)
        {
            ReturnToMenu();
        }
        else if (availableResult.Status != MatchStatus.Success
            || informationResult.Status != MatchStatus.Success
            || stateResult.Status != MatchStatus.Success)
        {
            _keepalive.Stop();
            MatchStatus failure = stateResult.Status != MatchStatus.Success
                ? stateResult.Status
                : informationResult.Status != MatchStatus.Success
                    ? informationResult.Status
                    : availableResult.Status;
            _dialogs.ShowError(this, "AvailableMatches_ServerConnectionFailedTitle",
                RemoteMatchMessages.Key(failure, _guestName != null));
        }
        _refreshing = false;
    }

    private void OnRefreshClick(object sender, RoutedEventArgs args)
    {
        _keepalive.Start();
        OnKeepalive(sender, args);
    }

    private async void OnCancelClick(object sender, RoutedEventArgs args)
    {
        IsEnabled = false;
        MatchStatus status = await _client.CancelMatch(_lobby.MatchId);
        if (status == MatchStatus.Success)
        {
            ReturnToMenu();
        }
        else
        {
            _dialogs.ShowError(this, "JoinMatch_PersistenceErrorTitle",
                RemoteMatchMessages.Key(status, _guestName != null));
        }
        IsEnabled = true;
    }

    private void ReturnToMenu()
    {
        if (_navigated)
        {
            return;
        }
        _navigated = true;
        NavigationService?.Navigate(_guestName == null ? new MainMenuPage() : new MainMenuPage(_guestName));
    }
    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
        if (_lobby != null)
        {
            UpdateTurnTimeText();
        }
    }
    private void UpdateTurnTimeText()
    {
        turnTimeTextBlock.Text = string.Format(
            LocalizationManager.Instance["WaitingForOpponent_TurnTimeFormat"],
            _lobby.TurnDurationSeconds);
    }
}
