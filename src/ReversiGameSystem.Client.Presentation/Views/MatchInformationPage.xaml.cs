using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ServiceModel;
using System.Threading.Tasks;
using ReversiGameSystem.Features.Gameplay;
using ReversiGameSystem.Features.Matches;
using ReversiGameSystem.Matches;
using MatchInformation = ReversiGameSystem.Game.MatchInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

internal enum MatchInformationMode
{
    Spectator,
    Replay
}

internal enum MatchInformationReturnTarget
{
    AvailableMatches,
    History
}

public partial class MatchInformationPage : Page
{
    private const int BlackSeatNumber = 1;
    private const int WhiteSeatNumber = 2;
    private readonly MatchInformation? _match;
    private readonly MatchInformationMode _mode;
    private readonly MatchInformationReturnTarget _returnTarget;
    private readonly string? _guestName;
    private readonly DialogService _dialogService =
        new DialogService(LocalizationManager.Instance);
    private readonly MovementHistoryService? _movementHistoryService;
    private readonly DispatcherTimer _replayTimer;
    private readonly MatchInformationViewModel? _spectator;
    private readonly DispatcherTimer? _connectionTimer;
    private bool _checkingConnection;
    private MatchInformation LegacyMatch
    {
        get
        {
            return _match ?? throw new InvalidOperationException("Replay information is unavailable in spectator mode.");
        }
    }

    private int _frameIndex;
    private bool _isPlaying;

    internal MatchInformationPage(
        MatchInformation match,
        MatchInformationMode mode,
        MatchInformationReturnTarget returnTarget,
        string? guestName = null)
    {
        InitializeComponent();
        _match = match;
        _mode = mode;
        _returnTarget = returnTarget;
        _guestName = guestName;
        _movementHistoryService = new MovementHistoryService(new[] { match });
        _frameIndex = mode == MatchInformationMode.Replay
            ? 0
            : match.BoardFrames.Count - 1;
        _replayTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(900)
        };
        _replayTimer.Tick += OnReplayTimerTick;
        Unloaded += OnPageUnloaded;
        replayControlsPanel.Visibility = mode == MatchInformationMode.Replay
            ? Visibility.Visible
            : Visibility.Collapsed;
        ShowLocalizedInformation();
        ShowFrame();
    }

    internal MatchInformationPage(GameplayClient gameplay, long matchId)
    {
        InitializeComponent();
        _mode = MatchInformationMode.Spectator;
        _returnTarget = MatchInformationReturnTarget.AvailableMatches;
        _replayTimer = new DispatcherTimer();
        _spectator = new MatchInformationViewModel(gameplay, ClientRuntime.CurrentSession, matchId);
        _connectionTimer = new DispatcherTimer { Interval = MatchConnectionPolicy.ConnectionCheckInterval };
        _connectionTimer.Tick += OnConnectionCheck;
        replayControlsPanel.Visibility = Visibility.Collapsed;
        movementDetailsButton.Visibility = Visibility.Collapsed;
        _spectator.PropertyChanged += OnSpectatorChanged;
        Loaded += OnSpectatorLoaded;
        Unloaded += OnPageUnloaded;
    }

    private async void OnSpectatorLoaded(object sender, RoutedEventArgs e)
    {
        if (_spectator == null)
        {
            return;
        }
        try
        {
            MatchStatus status = await _spectator.Activate();
            if (!_spectator.IsCurrentSession())
            {
                return;
            }
            if (status != MatchStatus.Success)
            {
                _dialogService.ShowError(this, "AvailableMatches_RetrievalErrorTitle",
                    RemoteMatchMessages.GetMessageResourceKey(status, false));
                NavigationService?.Navigate(new AvailableMatchesPage());
                return;
            }
            ShowSpectator();
            _connectionTimer?.Start();
        }
        catch (OperationCanceledException) when (!_spectator.IsCurrentSession())
        {
            _spectator.Deactivate();
        }
        catch (FaultException<ReversiGameSystem.ServerProxy.ServiceFault> exception)
        {
            OnSpectatorFailure(exception);
            NavigationService?.Navigate(new AvailableMatchesPage());
        }
    }

    private async void OnConnectionCheck(object? sender, EventArgs e)
    {
        if (_checkingConnection || _spectator == null)
        {
            return;
        }
        _checkingConnection = true;
        try
        {
            await _spectator.CheckConnection();
        }
        catch (FaultException<ReversiGameSystem.ServerProxy.ServiceFault> exception)
        {
            OnSpectatorFailure(exception);
        }
        finally
        {
            _checkingConnection = false;
        }
    }

    private void OnSpectatorFailure(Exception exception)
    {
        if (_spectator?.IsCurrentSession() == true)
        {
            _connectionTimer?.Stop();
            _spectator.Deactivate();
            ClientRuntime.Errors.ReportOperationFailure(exception, nameof(MatchInformationPage));
        }
    }

    private async void OnSpectatorChanged(object? sender, PropertyChangedEventArgs e)
    {
        ShowSpectator();
        if (_spectator == null || !_spectator.IsCurrentSession())
        {
            return;
        }
        try
        {
            if (e.PropertyName == nameof(MatchInformationViewModel.State))
            {
                MatchStatus status = await _spectator.LoadMovements();
                if (_spectator.IsCurrentSession() && status != MatchStatus.Success)
                {
                    _dialogService.ShowError(this, "AvailableMatches_RetrievalErrorTitle",
                        RemoteMatchMessages.GetMessageResourceKey(status, false));
                }
            }
            else if (e.PropertyName == nameof(MatchInformationViewModel.NeedsRecovery))
            {
                await _spectator.RecoverConnection();
            }
        }
        catch (OperationCanceledException) when (!_spectator.IsCurrentSession())
        {
            _spectator.Deactivate();
        }
        catch (FaultException<ReversiGameSystem.ServerProxy.ServiceFault> exception)
        {
            OnSpectatorFailure(exception);
        }
    }

    private void ShowSpectator()
    {
        if (_spectator?.IsReady != true || !_spectator.IsCurrentSession())
        {
            return;
        }
        LocalizationManager localization = LocalizationManager.Instance;
        MatchStateSnapshot state = _spectator.State;
        modeTextBlock.Text = localization["MatchInformation_SpectatorMode"];
        blackPlayerTextBlock.Text = string.Format(localization["MatchInformation_BlackPlayerFormat"],
            _spectator.GetPlayerName(BlackSeatNumber));
        whitePlayerTextBlock.Text = string.Format(localization["MatchInformation_WhitePlayerFormat"],
            _spectator.GetPlayerName(WhiteSeatNumber));
        stateTextBlock.Text = string.Format(localization["MatchInformation_StateFormat"],
            localization[state.State == ConfirmedMatchState.Finished
                ? "MatchInformation_FinishedState" : "MatchInformation_InProgressState"]);
        turnTimeTextBlock.Text = string.Format(localization["MatchInformation_TurnTimeFormat"],
            _spectator.Information.TurnDurationSeconds);
        scoreTextBlock.Text = string.Format(localization["MatchInformation_ScoreFormat"],
            state.BlackPieceCount, state.WhitePieceCount);
        string resultText;
        if (state.Conclusion == null)
        {
            resultText = localization["MatchInformation_NoFinalResult"];
        }
        else if (state.Conclusion.WinnerSeatNumber.HasValue)
        {
            resultText = string.Format(localization["MatchInformation_WinnerFormat"],
                _spectator.GetPlayerName(state.Conclusion.WinnerSeatNumber.Value));
        }
        else
        {
            resultText = localization["MatchInformation_DrawResult"];
        }
        resultTextBlock.Text = resultText;
        boardCaptionTextBlock.Text = localization["MatchInformation_CurrentBoardTitle"];
        framePositionTextBlock.Text = _spectator.ConnectionMessage;
        movesListBox.ItemsSource = _spectator.ReadMoveDescriptions();
        matchBoardControl.SystemPlayerColor = DiscColor.Empty;
        foreach (MatchParticipant participant in _spectator.Information.Participants)
        {
            if (participant.IsSystem)
            {
                matchBoardControl.SystemPlayerColor = participant.SeatNumber == BlackSeatNumber
                    ? DiscColor.Black : DiscColor.White;
            }
        }
        matchBoardControl.ShowBoard(_spectator.ReadBoard(), DiscColor.Empty);
    }

    private void ShowLocalizedInformation()
    {
        LocalizationManager localization = LocalizationManager.Instance;
        modeTextBlock.Text = localization[_mode == MatchInformationMode.Replay
            ? "MatchInformation_ReplayMode"
            : "MatchInformation_SpectatorMode"];
        blackPlayerTextBlock.Text = string.Format(
            localization["MatchInformation_BlackPlayerFormat"],
            LegacyMatch.BlackPlayerName);
        whitePlayerTextBlock.Text = string.Format(
            localization["MatchInformation_WhitePlayerFormat"],
            LegacyMatch.WhitePlayerName);
        stateTextBlock.Text = string.Format(
            localization["MatchInformation_StateFormat"],
            localization[LegacyMatch.State == MatchRecordState.Finished
                ? "MatchInformation_FinishedState"
                : "MatchInformation_InProgressState"]);
        turnTimeTextBlock.Text = string.Format(
            localization["MatchInformation_TurnTimeFormat"],
            LegacyMatch.TurnTimeSeconds);
        string resultText = localization["MatchInformation_NoFinalResult"];
        if (LegacyMatch.State == MatchRecordState.Finished)
        {
            resultText = LegacyMatch.WinnerName == null
                ? localization["MatchInformation_DrawResult"]
                : string.Format(localization["MatchInformation_WinnerFormat"],
                    LegacyMatch.WinnerName);
        }
        resultTextBlock.Text = resultText;
        boardCaptionTextBlock.Text = localization[_mode == MatchInformationMode.Replay
            ? "MatchInformation_ReplayBoardTitle"
            : "MatchInformation_CurrentBoardTitle"];
        RefreshMoveList();
        UpdatePlayButton();
    }

    private void ShowFrame()
    {
        ReversiBoard board = LegacyMatch.BoardFrames[_frameIndex];
        matchBoardControl.ShowBoard(board);
        LocalizationManager localization = LocalizationManager.Instance;
        scoreTextBlock.Text = string.Format(
            localization["MatchInformation_ScoreFormat"],
            board.Count(DiscColor.Black),
            board.Count(DiscColor.White));
        framePositionTextBlock.Text = _mode == MatchInformationMode.Replay
            ? string.Format(
                localization["MatchInformation_FrameFormat"],
                _frameIndex,
                LegacyMatch.Moves.Count)
            : string.Empty;
        movesListBox.SelectedIndex = _frameIndex - 1;
        if (movesListBox.SelectedItem != null)
        {
            movesListBox.ScrollIntoView(movesListBox.SelectedItem);
        }
    }

    private void RefreshMoveList()
    {
        LocalizationManager localization = LocalizationManager.Instance;
        List<string> moveDescriptions = new List<string>();
        foreach (RecordedMove move in LegacyMatch.Moves)
        {
            moveDescriptions.Add(string.Format(
                localization["MatchInformation_MoveFormat"],
                move.Number,
                move.PlayerName,
                move.Coordinate));
        }

        movesListBox.ItemsSource = moveDescriptions;
    }

    private void OnPreviousClick(object sender, RoutedEventArgs e)
    {
        StopReplay();
        if (_frameIndex > 0)
        {
            _frameIndex--;
            ShowFrame();
        }
    }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        StopReplay();
        AdvanceReplay();
    }

    private void OnPlayPauseClick(object sender, RoutedEventArgs e)
    {
        if (_isPlaying)
        {
            StopReplay();
            return;
        }

        if (_frameIndex >= LegacyMatch.BoardFrames.Count - 1)
        {
            _frameIndex = 0;
            ShowFrame();
        }

        _isPlaying = true;
        _replayTimer.Start();
        UpdatePlayButton();
    }

    private void OnReplayTimerTick(object? sender, EventArgs e)
    {
        AdvanceReplay();
    }

    private void AdvanceReplay()
    {
        if (_frameIndex >= LegacyMatch.BoardFrames.Count - 1)
        {
            StopReplay();
            return;
        }

        _frameIndex++;
        ShowFrame();
        if (_frameIndex >= LegacyMatch.BoardFrames.Count - 1)
        {
            StopReplay();
        }
    }

    private void StopReplay()
    {
        _replayTimer.Stop();
        _isPlaying = false;
        UpdatePlayButton();
    }

    private void UpdatePlayButton()
    {
        playPauseButton.Content = LocalizationManager.Instance[_isPlaying
            ? "MatchInformation_PauseButton"
            : "MatchInformation_PlayButton"];
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        StopReplay();
        if (_returnTarget == MatchInformationReturnTarget.History)
        {
            NavigationService?.Navigate(new MatchHistoryPage());
            return;
        }

        NavigationService?.Navigate(_guestName == null
            ? new AvailableMatchesPage()
            : new AvailableMatchesPage(_guestName));
    }

    private void OnViewMovementHistoryClick(object sender, RoutedEventArgs e)
    {
        MovementHistoryService service = _movementHistoryService
            ?? throw new InvalidOperationException("Legacy movements are unavailable in spectator mode.");
        MovementHistoryRetrievalResult result =
            service.TryGetMovements(
                LegacyMatch.Id,
                out IReadOnlyList<RecordedMove> movements);
        if (result != MovementHistoryRetrievalResult.Success)
        {
            string prefix = result
                == MovementHistoryRetrievalResult.ServerConnectionFailed
                    ? "MovementHistory_ServerConnectionFailed"
                    : "MovementHistory_RetrievalError";
            _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
            return;
        }

        MovementHistoryDialogWindow dialog = new MovementHistoryDialogWindow(
            LocalizationManager.Instance,
            movements);
        Window? owner = Window.GetWindow(this);
        if (owner != null)
        {
            dialog.Owner = owner;
        }

        dialog.ShowDialog();
    }

    private async void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string previousCulture = LocalizationManager.Instance.CultureName;
        await LanguageChangeService.ConfirmSelection((ComboBox)sender);
        if (LocalizationManager.Instance.CultureName != previousCulture)
        {
            if (_spectator != null)
            {
                ShowSpectator();
            }
            else
            {
                ShowLocalizedInformation();
                ShowFrame();
            }
        }
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _replayTimer.Stop();
        _connectionTimer?.Stop();
        if (_spectator != null)
        {
            _spectator.PropertyChanged -= OnSpectatorChanged;
            _spectator.Deactivate();
        }
    }
}
