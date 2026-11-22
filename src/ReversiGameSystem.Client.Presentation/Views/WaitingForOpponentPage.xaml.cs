using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

public partial class WaitingForOpponentPage : Page
{
    private const string ReconnectingKey = "Game_Reconnecting";
    private const string ServerConnectionFailedTitleKey =
        "AvailableMatches_ServerConnectionFailedTitle";
    private static readonly TimeSpan NormalKeepaliveInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ReconnectingRetryInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ReconnectionWindow = TimeSpan.FromSeconds(30);

    private readonly MatchLobbyClient _client;
    private readonly GameplayClient _gameplayClient;
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

    internal WaitingForOpponentPage(MatchLobbyClient client, LobbySnapshot lobby, string? guestName)
    {
        InitializeComponent();
        _client = client;
        _gameplayClient = ClientRuntime.GetGameplayClient(guestName != null);
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
        _client.MatchStarted += OnState;
        _gameplayClient.StateChanged += OnState;
        _client.LobbyClosed += OnClosed;
        _keepalive.Start();
        OnKeepalive(sender, args);
        if (_gameplayClient.HasMatchState(_lobby.MatchId))
        {
            OnState(_gameplayClient.ReadMatchState(_lobby.MatchId));
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        _keepalive.Stop();
        _client.MatchStarted -= OnState;
        _gameplayClient.StateChanged -= OnState;
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
            NavigationService?.Navigate(new GamePage(_gameplayClient,
                ClientRuntime.GetMatchChatClient(_guestName != null), _lobby.MatchId, _guestName));
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
            MatchStatus status = await _gameplayClient.LoadMatchState(_lobby.MatchId);
            if (status == MatchStatus.Success)
            {
                SetReconnecting(false);
                OnState(_gameplayClient.ReadMatchState(_lobby.MatchId));
            }
            else if (status == MatchStatus.MatchNotInProgress)
            {
                SetReconnecting(false);
            }
            else if (status == MatchStatus.MatchNotWaiting)
            {
                ReturnToMenu();
            }
            else
            {
                HandleKeepaliveFailure(status);
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
        MatchStatus status = await _client.ChangeTurnDuration(
            _lobby.MatchId,
            selectedTurnDuration);
        IsEnabled = true;

        if (_navigated || !IsLoaded)
        {
            return;
        }

        if (status == MatchStatus.Success)
        {
            _lobby = _client.ReadCurrentLobby();
            UpdateTurnTimeText();
            return;
        }

        _dialogs.ShowError(
            this,
            "MatchSettings_PersistenceErrorTitle",
            RemoteMatchMessages.Key(status, _guestName != null));
        if (status == MatchStatus.MatchNotWaiting)
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
    private async void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        await LanguageChangeService.ConfirmSelection((ComboBox)sender);
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
