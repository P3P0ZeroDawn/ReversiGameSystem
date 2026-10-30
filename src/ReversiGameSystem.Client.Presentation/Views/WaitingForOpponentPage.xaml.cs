using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Views;

/// <summary>Waits for a confirmed second participant on the remote lobby channel.</summary>
public partial class WaitingForOpponentPage : Page
{
    private readonly RemoteMatchClient _client;
    private readonly Proxy.LobbySnapshot _lobby;
    private readonly string? _guestName;
    private readonly DialogService _dialogs = new(LocalizationManager.Instance);
    private readonly DispatcherTimer _keepalive = new() { Interval = TimeSpan.FromSeconds(15) };
    private bool _navigated;
    private bool _refreshing;

    internal WaitingForOpponentPage(RemoteMatchClient client, Proxy.LobbySnapshot lobby, string? guestName)
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
        if (_client.TryReadState(_lobby.MatchId, out Proxy.MatchStateSnapshot? cached))
        {
            OnState(cached!);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        _keepalive.Stop();
        _client.StateChanged -= OnState;
        _client.LobbyClosed -= OnClosed;
    }

    private void OnState(Proxy.MatchStateSnapshot state)
    {
        if (state.MatchId != _lobby.MatchId || state.State != Proxy.MatchState.InProgress)
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
            await _client.ListAsync();
            await _client.LoadInformationAsync(_lobby.MatchId);
            Proxy.MatchStateSnapshot state = await _client.LoadStateAsync(_lobby.MatchId);
            OnState(state);
        }
        catch (FaultException<Proxy.MatchFault> fault)
            when (fault.Detail.Reason == Proxy.MatchFailure.MatchNotInProgress)
        {
            // A confirmed waiting lobby remains on this screen until the server starts it.
        }
        catch (FaultException<Proxy.MatchFault> fault)
            when (fault.Detail.Reason == Proxy.MatchFailure.MatchNotWaiting)
        {
            ReturnToMenu();
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            _keepalive.Stop();
            _dialogs.ShowError(this, "AvailableMatches_ServerConnectionFailedTitle", RemoteMatchMessages.Key(exception, _guestName != null));
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void OnRefreshClick(object sender, RoutedEventArgs args)
    {
        _keepalive.Start();
        OnKeepalive(sender, args);
    }

    private async void OnCancelClick(object sender, RoutedEventArgs args)
    {
        IsEnabled = false;
        try
        {
            await _client.CancelAsync(_lobby.MatchId);
            ReturnToMenu();
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            _dialogs.ShowError(this, "JoinMatch_PersistenceErrorTitle", RemoteMatchMessages.Key(exception, _guestName != null));
        }
        finally
        {
            IsEnabled = true;
        }
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
    private void UpdateTurnTimeText() => turnTimeTextBlock.Text = string.Format(
        LocalizationManager.Instance["WaitingForOpponent_TurnTimeFormat"], _lobby.Configuration.TurnDurationSeconds);
}
