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
    private const string ReconnectingKey = "Game_Reconnecting";
    private const string ServerConnectionFailedTitleKey =
        "AvailableMatches_ServerConnectionFailedTitle";
    private static readonly TimeSpan NormalKeepaliveInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ReconnectingRetryInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ReconnectionWindow = TimeSpan.FromSeconds(30);

    private readonly RemoteMatchClient _client;
    private LobbySnapshot _lobby;
    private readonly string? _guestName;
    private readonly DialogService _dialogs =
        new DialogService(LocalizationManager.Instance);
    private readonly DispatcherTimer _keepalive = new DispatcherTimer
    {
        Interval = NormalKeepaliveInterval
    };
    private bool _navigated;
    private bool _refreshing;
    private DateTime? _reconnectingStartedUtc;

    internal WaitingForOpponentPage(RemoteMatchClient client, LobbySnapshot lobby, string? guestName)
    {
        InitializeComponent();
        _client = client;
        _lobby = lobby;
        _guestName = guestName;
        matchCodeTextBlock.Text = lobby.MatchCode;
        UpdateTurnTimeText();
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
        try
        {
            MatchResult<MatchStateSnapshot> stateResult =
                await _client.LoadMatchState(_lobby.MatchId);
            if (stateResult.Status == MatchStatus.Success && stateResult.Value != null)
            {
                SetReconnecting(false);
                OnState(stateResult.Value);
            }
            else if (stateResult.Status == MatchStatus.MatchNotInProgress)
            {
                SetReconnecting(false);
            }
            else if (stateResult.Status == MatchStatus.MatchNotWaiting)
            {
                ReturnToMenu();
            }
            else
            {
                HandleKeepaliveFailure(stateResult.Status);
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void HandleKeepaliveFailure(MatchStatus failure)
    {
        if (!_reconnectingStartedUtc.HasValue)
        {
            _reconnectingStartedUtc = DateTime.UtcNow;
            _keepalive.Interval = ReconnectingRetryInterval;
        }

        SetReconnecting(true);
        if (DateTime.UtcNow - _reconnectingStartedUtc.Value < ReconnectionWindow)
        {
            return;
        }

        _keepalive.Stop();
        _dialogs.ShowError(
            this,
            ServerConnectionFailedTitleKey,
            RemoteMatchMessages.Key(failure, _guestName != null));
        ReturnToMenu();
    }

    private void SetReconnecting(bool isReconnecting)
    {
        if (!isReconnecting && !_reconnectingStartedUtc.HasValue)
        {
            return;
        }

        if (!isReconnecting)
        {
            _reconnectingStartedUtc = null;
            _keepalive.Interval = NormalKeepaliveInterval;
        }

        modifySettingsButton.IsEnabled = !isReconnecting;
        connectionStatusTextBlock.Text = isReconnecting
            ? LocalizationManager.Instance[ReconnectingKey]
            : string.Empty;
        connectionStatusTextBlock.Visibility = isReconnecting
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private async void OnPlayAgainstSystemClick(object sender, RoutedEventArgs args)
    {
        IsEnabled = false;
        try
        {
            MatchStatus status = await _client.StartSystemMatch(_lobby.MatchId);
            if (status != MatchStatus.Success && IsLoaded)
            {
                _dialogs.ShowError(this, ServerConnectionFailedTitleKey,
                    RemoteMatchMessages.Key(status, _guestName != null));
            }
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private async void OnModifySettingsClick(object sender, RoutedEventArgs args)
    {
        int selectedTurnDuration = _dialogs.SelectTurnDuration(this, _lobby.TurnDurationSeconds);
        if (selectedTurnDuration == _lobby.TurnDurationSeconds)
        {
            return;
        }

        IsEnabled = false;
        MatchResult<LobbySnapshot> result = await _client.ChangeTurnDuration(
            _lobby.MatchId,
            selectedTurnDuration);
        IsEnabled = true;

        if (_navigated || !IsLoaded)
        {
            return;
        }

        if (result.Status == MatchStatus.Success && result.Value != null)
        {
            _lobby = result.Value;
            UpdateTurnTimeText();
            return;
        }

        _dialogs.ShowError(
            this,
            "MatchSettings_PersistenceErrorTitle",
            RemoteMatchMessages.Key(result.Status, _guestName != null));
        if (result.Status == MatchStatus.MatchNotWaiting)
        {
            ReturnToMenu();
        }
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
        SetReconnecting(_reconnectingStartedUtc.HasValue);
    }
    private void UpdateTurnTimeText()
    {
        turnTimeTextBlock.Text = string.Format(
            LocalizationManager.Instance["WaitingForOpponent_TurnTimeFormat"],
            _lobby.TurnDurationSeconds);
    }
}
