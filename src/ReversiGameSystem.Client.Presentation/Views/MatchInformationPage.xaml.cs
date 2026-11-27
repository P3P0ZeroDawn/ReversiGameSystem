using System;
using System.ComponentModel;
using System.IO;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Features.Gameplay;
using ReversiGameSystem.Features.Matches;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

public partial class MatchInformationPage : Page
{
    private const int BlackSeatNumber = 1;
    private const int WhiteSeatNumber = 2;
    private const int ReplayFrameIntervalMilliseconds = 900;
    private readonly MatchReplayViewModel? _replay;
    private readonly DialogService _dialogService =
        new DialogService(LocalizationManager.Instance);
    private readonly DispatcherTimer _replayTimer;
    private readonly MatchInformationViewModel? _spectator;
    private readonly DispatcherTimer? _connectionTimer;
    private bool _checkingConnection;
    private MatchReplayViewModel Replay
    {
        get
        {
            return _replay ?? throw new InvalidOperationException(
                "Replay information is unavailable in spectator mode.");
        }
    }

    internal MatchInformationPage(MatchRecording recording)
    {
        InitializeComponent();
        _replay = new MatchReplayViewModel(recording);
        _replayTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(ReplayFrameIntervalMilliseconds)
        };
        _replayTimer.Tick += OnReplayTimerTick;
        Loaded += OnReplayLoaded;
        Unloaded += OnPageUnloaded;
        replayControlsPanel.Visibility = Visibility.Visible;
        ShowLocalizedInformation();
        ShowFrame();
    }

    internal MatchInformationPage(GameplayClient gameplay, long matchId)
    {
        InitializeComponent();
        _replayTimer = new DispatcherTimer();
        _spectator = new MatchInformationViewModel(gameplay, ClientRuntime.CurrentSession, matchId);
        _connectionTimer = new DispatcherTimer
        {
            Interval = MatchConnectionPolicy.ConnectionCheckInterval
        };
        _connectionTimer.Tick += OnConnectionCheck;
        replayControlsPanel.Visibility = Visibility.Collapsed;
        movementDetailsButton.Visibility = Visibility.Collapsed;
        _spectator.PropertyChanged += OnSpectatorChanged;
        Loaded += OnSpectatorLoaded;
        Unloaded += OnPageUnloaded;
    }

    private async void OnReplayLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ProfileStatus status = await Replay.PlayerPictures.LoadPlayerPictures(
                ClientRuntime.ProfileService, ClientRuntime.CurrentSession);
            if (!IsLoaded || status == ProfileStatus.InvalidSession)
            {
                return;
            }
            ShowFrame();
            if (status != ProfileStatus.Success)
            {
                _dialogService.ShowWarning(this,
                    "Game_PlayerPicturesUnavailableTitle", "Game_PlayerPicturesUnavailableMessage");
            }
        }
        catch (Exception exception) when (RemoteCall.IsDeclaredFault(exception)
            || exception is IOException or NotSupportedException or FormatException)
        {
            if (IsLoaded)
            {
                ClientRuntime.Errors.ReportOperationFailure(exception, nameof(OnReplayLoaded));
            }
            else
            {
                ClientRuntime.Errors.RecordFailure(
                    exception, nameof(OnReplayLoaded), LogLevel.Error);
            }
        }
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
        modeTextBlock.Text = localization["MatchInformation_ReplayMode"];
        blackPlayerTextBlock.Text = string.Format(
            localization["MatchInformation_BlackPlayerFormat"], Replay.GetPlayerName(BlackSeatNumber));
        whitePlayerTextBlock.Text = string.Format(
            localization["MatchInformation_WhitePlayerFormat"], Replay.GetPlayerName(WhiteSeatNumber));
        stateTextBlock.Text = string.Format(localization["MatchInformation_StateFormat"],
            localization[Replay.IsFinished ? "MatchInformation_FinishedState"
                : "MatchInformation_InProgressState"]);
        turnTimeTextBlock.Text = string.Format(localization["MatchInformation_TurnTimeFormat"],
            Replay.TurnDurationSeconds);
        resultTextBlock.Text = Replay.FinalResult;
        boardCaptionTextBlock.Text = localization["MatchInformation_ReplayBoardTitle"];
        movesListBox.ItemsSource = Replay.ReadMoveDescriptions();
        UpdatePlayButton();
    }

    private void ShowFrame()
    {
        ReversiBoard board = Replay.ReadBoard();
        matchBoardControl.SystemPlayerColor = Replay.PlayerPictures.SystemPlayerColor;
        matchBoardControl.BlackPlayerPicture = Replay.PlayerPictures.BlackPlayerPicture;
        matchBoardControl.WhitePlayerPicture = Replay.PlayerPictures.WhitePlayerPicture;
        matchBoardControl.ShowBoard(board);
        LocalizationManager localization = LocalizationManager.Instance;
        scoreTextBlock.Text = string.Format(localization["MatchInformation_ScoreFormat"],
            board.Count(DiscColor.Black), board.Count(DiscColor.White));
        framePositionTextBlock.Text = string.Format(localization["MatchInformation_FrameFormat"],
            Replay.FrameIndex, Replay.MoveCount);
        movesListBox.SelectedIndex = Replay.FrameIndex - 1;
        if (movesListBox.SelectedItem != null)
        {
            movesListBox.ScrollIntoView(movesListBox.SelectedItem);
        }
    }

    private void OnPreviousClick(object sender, RoutedEventArgs e)
    {
        Replay.PreviousFrame();
        _replayTimer.Stop();
        ShowFrame();
        UpdatePlayButton();
    }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        StopReplay();
        Replay.NextFrame();
        ShowFrame();
    }

    private void OnPlayPauseClick(object sender, RoutedEventArgs e)
    {
        Replay.TogglePlayback();
        ShowFrame();
        if (Replay.IsPlaying)
        {
            _replayTimer.Start();
        }
        else
        {
            _replayTimer.Stop();
        }
        UpdatePlayButton();
    }

    private void OnReplayTimerTick(object? sender, EventArgs e)
    {
        Replay.NextFrame();
        ShowFrame();
        if (!Replay.IsPlaying)
        {
            StopReplay();
        }
    }

    private void StopReplay()
    {
        _replayTimer.Stop();
        _replay?.PausePlayback();
        UpdatePlayButton();
    }

    private void UpdatePlayButton()
    {
        playPauseButton.Content = LocalizationManager.Instance[_replay?.IsPlaying == true
            ? "MatchInformation_PauseButton" : "MatchInformation_PlayButton"];
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        StopReplay();
        if (_replay != null)
        {
            NavigationService?.Navigate(new MatchHistoryPage());
        }
        else
        {
            NavigationService?.Navigate(new AvailableMatchesPage());
        }
    }

    private void OnViewMovementHistoryClick(object sender, RoutedEventArgs e)
    {
        MovementHistoryDialogWindow dialog = new MovementHistoryDialogWindow(
            LocalizationManager.Instance, Replay.ReadMovementDetails());
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
        _replay?.PlayerPictures.Deactivate();
        if (_spectator != null)
        {
            _spectator.PropertyChanged -= OnSpectatorChanged;
            _spectator.Deactivate();
        }
    }
}
