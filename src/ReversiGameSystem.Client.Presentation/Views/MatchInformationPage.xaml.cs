using System;
using System.Collections.Generic;
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

/// <summary>
/// Displays the current or historical state of a selected match.
/// </summary>
public partial class MatchInformationPage : Page
{
    private readonly MatchInformation _match;
    private readonly MatchInformationMode _mode;
    private readonly MatchInformationReturnTarget _returnTarget;
    private readonly string? _guestName;
    private readonly DialogService _dialogService =
        new DialogService(LocalizationManager.Instance);
    private readonly MovementHistoryService _movementHistoryService;
    private readonly DispatcherTimer _replayTimer;
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

    private void ShowLocalizedInformation()
    {
        LocalizationManager localization = LocalizationManager.Instance;
        modeTextBlock.Text = localization[_mode == MatchInformationMode.Replay
            ? "MatchInformation_ReplayMode"
            : "MatchInformation_SpectatorMode"];
        blackPlayerTextBlock.Text = string.Format(
            localization["MatchInformation_BlackPlayerFormat"],
            _match.BlackPlayerName);
        whitePlayerTextBlock.Text = string.Format(
            localization["MatchInformation_WhitePlayerFormat"],
            _match.WhitePlayerName);
        stateTextBlock.Text = string.Format(
            localization["MatchInformation_StateFormat"],
            localization[_match.State == MatchRecordState.Finished
                ? "MatchInformation_FinishedState"
                : "MatchInformation_InProgressState"]);
        turnTimeTextBlock.Text = string.Format(
            localization["MatchInformation_TurnTimeFormat"],
            _match.TurnTimeSeconds);
        resultTextBlock.Text = _match.State != MatchRecordState.Finished
            ? localization["MatchInformation_NoFinalResult"]
            : _match.WinnerName == null
                ? localization["MatchInformation_DrawResult"]
                : string.Format(
                    localization["MatchInformation_WinnerFormat"],
                    _match.WinnerName);
        boardCaptionTextBlock.Text = localization[_mode == MatchInformationMode.Replay
            ? "MatchInformation_ReplayBoardTitle"
            : "MatchInformation_CurrentBoardTitle"];
        RefreshMoveList();
        UpdatePlayButton();
    }

    private void ShowFrame()
    {
        ReversiBoard board = _match.BoardFrames[_frameIndex];
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
                _match.Moves.Count)
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
        foreach (RecordedMove move in _match.Moves)
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

        if (_frameIndex >= _match.BoardFrames.Count - 1)
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
        if (_frameIndex >= _match.BoardFrames.Count - 1)
        {
            StopReplay();
            return;
        }

        _frameIndex++;
        ShowFrame();
        if (_frameIndex >= _match.BoardFrames.Count - 1)
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
        MovementHistoryRetrievalResult result =
            _movementHistoryService.TryGetMovements(
                _match.Id,
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

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string previousCulture = LocalizationManager.Instance.CultureName;
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
        if (LocalizationManager.Instance.CultureName != previousCulture)
        {
            ShowLocalizedInformation();
            ShowFrame();
        }
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _replayTimer.Stop();
    }
}
