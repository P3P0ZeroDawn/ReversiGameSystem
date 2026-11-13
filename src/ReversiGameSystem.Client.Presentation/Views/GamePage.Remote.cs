using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

public partial class GamePage
{
    private const string RemoteMatchCloseViewKey = "RemoteMatch_CloseView";
    private const string GameLeaveConfirmationTitleKey = "Game_LeaveConfirmationTitle";
    private const string GameLeaveMatchButtonKey = "Game_LeaveMatchButton";
    private const string GameContinueMatchButtonKey = "Game_ContinueMatchButton";
    private const string AbandonMatchFailedTitleKey = "AbandonMatch_ServerConnectionFailedTitle";
    private const string GameMatchResultTitleKey = "Game_MatchResultTitle";

    private readonly RemoteMatchClient _remoteClient;
    private readonly long _remoteMatchId;
    private readonly string? _remoteGuestName;
    private readonly DispatcherTimer _remoteRefreshTimer = new DispatcherTimer
    {
        Interval = TimeSpan.FromSeconds(15)
    };
    private RemoteMatchInformation? _remoteInformation;
    private bool _remoteLoading;
    private bool _remotePlacing;
    private bool _remoteSending;
    private bool _remoteLeaving;
    private bool _remoteChatReady;
    private bool _remoteConclusionShown;
    private DateTime? _remoteMutedUntil;
    private string? _remoteStatusKey;
    private string? _leaveButtonResourceKey;

    internal GamePage(RemoteMatchClient client, long matchId, string? guestName)
    {
        _remoteClient = client ?? throw new ArgumentNullException(nameof(client));
        _remoteMatchId = matchId;
        _remoteGuestName = guestName;
        InitializeComponent();
        // The server owns match state. Local rules only identify selectable positions.
        sendMessageButton.IsEnabled = false;
        refreshRemoteMatchButton.Visibility = Visibility.Visible;
        creatorConnectionTextBlock.Visibility = Visibility.Visible;
        opponentConnectionTextBlock.Visibility = Visibility.Visible;
        UpdateLeaveButtonContent(null);
        turnTimeTextBlock.TextWrapping = TextWrapping.Wrap;
        if (guestName != null)
        {
            chatPanel.Visibility = Visibility.Collapsed;
            chatSpacingColumn.Width = new GridLength(0);
            chatColumn.Width = new GridLength(0);
        }
        Loaded += OnRemotePageLoaded;
        Unloaded += OnRemotePageUnloaded;
        _remoteRefreshTimer.Tick += OnRemoteRefreshTick;
    }

    private async void OnRemotePageLoaded(object sender, RoutedEventArgs e)
    {
        LocalizationManager.Instance.PropertyChanged += OnRemoteCultureChanged;
        _remoteClient.StateChanged += OnRemoteStateChanged;
        _remoteClient.MessagesChanged += OnRemoteMessagesChanged;
        _remoteClient.ParticipantConnectionChanged += OnParticipantConnectionChanged;
        _remoteClient.Muted += OnRemoteMuted;
        gameBoardControl.SquareSelected += OnRemoteSquareSelected;
        await LoadRemoteMatch();
        if (IsLoaded)
        {
            _remoteRefreshTimer.Start();
        }
    }

    private void OnRemotePageUnloaded(object sender, RoutedEventArgs e)
    {
        LocalizationManager.Instance.PropertyChanged -= OnRemoteCultureChanged;
        _remoteRefreshTimer.Stop();
        _remoteClient.StateChanged -= OnRemoteStateChanged;
        _remoteClient.MessagesChanged -= OnRemoteMessagesChanged;
        _remoteClient.ParticipantConnectionChanged -= OnParticipantConnectionChanged;
        _remoteClient.Muted -= OnRemoteMuted;
        gameBoardControl.SquareSelected -= OnRemoteSquareSelected;
        _remoteChatReady = false;
        sendMessageButton.IsEnabled = false;
    }

    private void OnRemoteCultureChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationManager.CultureName))
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsLoaded)
                {
                    RenderRemoteMatch();
                    RenderRemoteMessages();
                    RenderParticipantConnections();
                    UpdateRemoteStatus();
                }
            }));
        }
    }

    private async void OnRemoteRefreshTick(object? sender, EventArgs e)
    {
        await LoadRemoteMatch();
    }

    private async void OnRefreshRemoteMatchClick(object sender, RoutedEventArgs e)
    {
        await LoadRemoteMatch();
    }

    private async Task LoadRemoteMatch()
    {
        if (_remoteLoading)
        {
            return;
        }

        _remoteLoading = true;
        refreshRemoteMatchButton.IsEnabled = false;
        try
        {
            if (await LoadMatchSnapshotAsync() && IsLoaded)
            {
                await InitializeRemoteChatAsync();
                if (_remoteGuestName != null || _remoteChatReady)
                {
                    _remoteStatusKey = null;
                }
            }
        }
        finally
        {
            _remoteLoading = false;
            refreshRemoteMatchButton.IsEnabled = true;
            sendMessageButton.IsEnabled = IsLoaded && _remoteChatReady && !_remoteSending;
            UpdateRemoteStatus();
        }
    }

    private async Task<bool> LoadMatchSnapshotAsync()
    {
        MatchResult<RemoteMatchInformation> informationResult =
            await _remoteClient.LoadRemoteMatchInformation(_remoteMatchId);
        MatchResult<MatchStateSnapshot> stateResult =
            await _remoteClient.LoadMatchState(_remoteMatchId);
        if (informationResult.Status != MatchStatus.Success
            || stateResult.Status != MatchStatus.Success)
        {
            _remoteChatReady = false;
            MatchStatus failure = informationResult.Status != MatchStatus.Success
                ? informationResult.Status
                : stateResult.Status;
            _remoteStatusKey = RemoteMatchMessages.Key(failure, _remoteGuestName != null);
            return false;
        }

        _remoteInformation = informationResult.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
        if (IsLoaded)
        {
            RenderRemoteMatch();
        }

        return true;
    }

    private async Task InitializeRemoteChatAsync()
    {
        if (_remoteGuestName != null)
        {
            return;
        }

        MatchStatus openStatus = await _remoteClient.OpenChatChannel(_remoteMatchId);
        MatchStatus refreshStatus = openStatus == MatchStatus.Success
            ? await _remoteClient.RefreshChat(_remoteMatchId)
            : openStatus;
        _remoteChatReady = IsLoaded && refreshStatus == MatchStatus.Success;
        if (_remoteChatReady)
        {
            RenderRemoteMessages();
            return;
        }

        _remoteStatusKey = RemoteMatchMessages.Key(refreshStatus, _remoteGuestName != null);
    }

    private void OnRemoteStateChanged(MatchStateSnapshot state)
    {
        if (state.MatchId == _remoteMatchId)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsLoaded)
                {
                    RenderRemoteMatch();
                }
            }));
        }
    }

    private void RenderRemoteMatch()
    {
        if (_remoteInformation == null || !_remoteClient.HasMatchState(_remoteMatchId))
        {
            return;
        }
        MatchStateSnapshot state = _remoteClient.ReadMatchState(_remoteMatchId);
        LocalizationManager localization = LocalizationManager.Instance;
        creatorNameTextBlock.Text = RemotePlayerName(1);
        opponentNameTextBlock.Text = RemotePlayerName(2);
        creatorScoreTextBlock.Text = string.Format(localization["Game_ScoreFormat"], state.BlackPieceCount);
        opponentScoreTextBlock.Text = string.Format(localization["Game_ScoreFormat"], state.WhitePieceCount);
        turnTextBlock.Text = state.CurrentTurnSeatNumber.HasValue
            ? string.Format(localization["Game_CurrentTurnFormat"], RemotePlayerName(state.CurrentTurnSeatNumber.Value))
            : localization["Game_MatchFinishedStatus"];
        // Map the confirmed board into the existing renderer, without applying moves or rules.
        string[] rows = Enumerable.Range(0, ReversiBoard.Size).Select(row => new string(
            state.Board.Skip(row * ReversiBoard.Size).Take(ReversiBoard.Size).Select(cell => cell switch
            {
                ConfirmedBoardCell.Black => 'B',
                ConfirmedBoardCell.White => 'W',
                _ => '.'
            }).ToArray())).ToArray();
        ReversiBoard board = ReversiBoard.FromRows(rows);
        DiscColor localColor = _remoteInformation.AssignedSeatNumber == 2
            ? DiscColor.White
            : DiscColor.Black;
        bool canPlace = !_remotePlacing
            && state.State == ConfirmedMatchState.InProgress
            && state.CurrentTurnSeatNumber == _remoteInformation.AssignedSeatNumber;
        if (canPlace)
        {
            gameBoardControl.ShowInteractiveBoard(
                board,
                localColor,
                board.GetValidMoves(localColor));
        }
        else
        {
            gameBoardControl.ShowBoard(board, localColor);
        }
        turnTimeTextBlock.Text = string.Format(
            localization["MatchInformation_TurnTimeFormat"],
            _remoteInformation.TurnDurationSeconds);
        RenderParticipantConnections();
        UpdateLeaveButtonContent(state.State);
        UpdateRemoteStatus();
        NotifyConclusionIfFinished(state);
    }

    private async void OnRemoteSquareSelected(
        object? sender,
        BoardSquareSelectedEventArgs e)
    {
        if (_remotePlacing
            || _remoteInformation?.AssignedSeatNumber == null
            || !_remoteClient.HasMatchState(_remoteMatchId))
        {
            return;
        }

        MatchStateSnapshot confirmedState = _remoteClient.ReadMatchState(_remoteMatchId);
        if (confirmedState.CurrentTurnSeatNumber != _remoteInformation.AssignedSeatNumber)
        {
            return;
        }

        _remotePlacing = true;
        RenderRemoteMatch();
        MatchResult<MatchStateSnapshot> placement = await _remoteClient.PlacePiece(
            confirmedState,
            e.Position.Row,
            e.Position.Column);
        if (placement.Status == MatchStatus.Success)
        {
            _remoteStatusKey = null;
        }
        else if (placement.Status == MatchStatus.StaleMatchStateVersion
            || placement.Status == MatchStatus.NotPlayersTurn)
        {
            await LoadRemoteMatch();
        }
        else
        {
            _remoteStatusKey = RemoteMatchMessages.Key(placement.Status);
            if (IsLoaded)
            {
                _dialogService.ShowWarning(this, "Game_Title", _remoteStatusKey);
            }
        }
        _remotePlacing = false;
        if (IsLoaded)
        {
            RenderRemoteMatch();
            UpdateRemoteStatus();
        }
    }

    private string RemotePlayerName(int seat)
    {
        return _remoteInformation?.Participants
            .FirstOrDefault(player => player.SeatNumber == seat)?.DisplayName
            ?? LocalizationManager.Instance["AccountInformation_UnavailableValue"];
    }

    private void OnRemoteMessagesChanged(long matchId)
    {
        if (matchId == _remoteMatchId)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsLoaded)
                {
                    RenderRemoteMessages();
                }
            }));
        }
    }

    private void OnParticipantConnectionChanged(long matchId)
    {
        if (matchId == _remoteMatchId)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsLoaded)
                {
                    RenderParticipantConnections();
                }
            }));
        }
    }

    private void RenderParticipantConnections()
    {
        RenderParticipantConnection(creatorConnectionTextBlock, 1);
        RenderParticipantConnection(opponentConnectionTextBlock, 2);
    }

    private void RenderParticipantConnection(TextBlock textBlock, int seatNumber)
    {
        ParticipantConnectionState state = _remoteClient.ReadParticipantConnection(
            _remoteMatchId,
            seatNumber);
        string resourceKey = "Game_PresenceUnknown";
        Brush foreground = Brushes.Gray;
        if (state == ParticipantConnectionState.Connected)
        {
            resourceKey = "Game_PresenceConnected";
            foreground = Brushes.ForestGreen;
        }
        else if (state == ParticipantConnectionState.Disconnected)
        {
            resourceKey = "Game_PresenceDisconnected";
            foreground = Brushes.IndianRed;
        }

        textBlock.Text = LocalizationManager.Instance[resourceKey];
        textBlock.Foreground = foreground;
    }

    private void RenderRemoteMessages()
    {
        if (_remoteInformation == null || _remoteGuestName != null)
        {
            return;
        }
        chatMessagesListBox.Items.Clear();
        foreach (MatchChatMessage message in _remoteClient.ReadMessages(_remoteMatchId))
        {
            chatMessagesListBox.Items.Add(string.Format(LocalizationManager.Instance["Chat_MessageFormat"],
                RemotePlayerName(message.SenderSeatNumber), message.Content));
        }
        if (chatMessagesListBox.Items.Count == 0)
        {
            chatMessagesListBox.Items.Add(LocalizationManager.Instance["Chat_NoMessagesMessage"]);
        }
        chatMessagesListBox.ScrollIntoView(chatMessagesListBox.Items[chatMessagesListBox.Items.Count - 1]);
    }

    private void OnRemoteMuted(RemoteChatMute mute)
    {
        if (mute.MatchId == _remoteMatchId)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsLoaded)
                {
                    _remoteMutedUntil = mute.EndsAtUtc;
                    UpdateRemoteStatus();
                }
            }));
        }
    }

    private void UpdateRemoteStatus()
    {
        if (_remoteMutedUntil > DateTime.UtcNow)
        {
            turnStatusTextBlock.Text = string.Format(
                LocalizationManager.Instance["Chat_PlayerMutedMessage"],
                _remoteMutedUntil.Value.ToLocalTime().ToString(
                    "g",
                    CultureInfo.GetCultureInfo(LocalizationManager.Instance.CultureName)));
            return;
        }

        string? statusKey = ResolveRemoteStatusKey();
        turnStatusTextBlock.Text = statusKey == null
            ? string.Empty
            : LocalizationManager.Instance[statusKey];
    }

    private string? ResolveRemoteStatusKey()
    {
        if (_remoteStatusKey != null)
        {
            return _remoteStatusKey;
        }
        if (!_remoteClient.HasMatchState(_remoteMatchId))
        {
            return null;
        }

        MatchStateSnapshot state = _remoteClient.ReadMatchState(_remoteMatchId);
        if (state.State == ConfirmedMatchState.Finished
            && state.Conclusion?.Reason == RemoteMatchEndReason.Abandonment)
        {
            return "MatchHistory_AbandonmentReason";
        }

        return null;
    }

    private async Task SendRemoteChatMessage()
    {
        string? content = ValidateRemoteChatInput();
        if (content == null)
        {
            return;
        }

        _remoteSending = true;
        sendMessageButton.IsEnabled = false;
        chatMessageTextBox.IsEnabled = false;
        try
        {
            ChatSubmission submission =
                await _remoteClient.SendChatMessage(_remoteMatchId, content);
            ApplyChatSubmissionResult(submission);
        }
        finally
        {
            _remoteSending = false;
            chatMessageTextBox.IsEnabled = true;
            sendMessageButton.IsEnabled = IsLoaded && _remoteChatReady;
            UpdateRemoteStatus();
        }
    }

    private string? ValidateRemoteChatInput()
    {
        if (_remoteSending || !_remoteChatReady || _remoteGuestName != null)
        {
            return null;
        }

        string content = chatMessageTextBox.Text.Trim();
        if (content.Length == 0 || content.Length > ChatStore.MaximumMessageLength)
        {
            ChatSendResult lengthFailure = content.Length == 0
                ? ChatSendResult.EmptyMessage
                : ChatSendResult.MessageTooLong;
            HandleChatSendFailure(lengthFailure, null);
            return null;
        }
        if (_remoteMutedUntil > DateTime.UtcNow)
        {
            HandleChatSendFailure(ChatSendResult.PlayerMuted, _remoteMutedUntil);
            return null;
        }

        return content;
    }

    private void ApplyChatSubmissionResult(ChatSubmission submission)
    {
        if (submission.Status == MatchStatus.Success)
        {
            if (IsLoaded)
            {
                chatMessageTextBox.Clear();
                _remoteStatusKey = null;
                RenderRemoteMessages();
            }
            return;
        }

        if (submission.Status == MatchStatus.PlayerMuted)
        {
            _remoteMutedUntil = submission.MuteEndsAtUtc;
            if (IsLoaded)
            {
                HandleChatSendFailure(ChatSendResult.PlayerMuted, _remoteMutedUntil);
            }
            return;
        }

        _remoteStatusKey = RemoteMatchMessages.Key(
            submission.Status, _remoteGuestName != null);
        if (IsLoaded)
        {
            _dialogService.ShowError(this, "Game_ChatTitle", _remoteStatusKey);
        }
    }

    private void ShowRemoteInstructions()
    {
        if (_remoteInformation == null)
        {
            _dialogService.ShowInformation(this, "Game_Title", "RemoteMatch_MovesUnavailable");
            return;
        }
        InstructionsLoadResult result = _instructionsService.TryLoadForMatch(
            LocalizationManager.Instance[_remoteInformation.AssignedSeatNumber == 2
                ? "Game_LightPiecesName" : "Game_DarkPiecesName"],
            _remoteInformation.TurnDurationSeconds, out InstructionsContent? content);
        if (result == InstructionsLoadResult.Success && content != null)
        {
            _dialogService.ShowInformationText(this, content.Title, content.Message);
        }
        else
        {
            _dialogService.ShowError(this, "Instructions_LoadingErrorTitle", "Instructions_LoadingErrorMessage");
        }
    }

    private void UpdateLeaveButtonContent(ConfirmedMatchState? matchState)
    {
        string resourceKey = ResolveLeaveButtonResourceKey(matchState);
        if (string.Equals(_leaveButtonResourceKey, resourceKey, StringComparison.Ordinal))
        {
            return;
        }

        _leaveButtonResourceKey = resourceKey;
        leaveMatchButton.SetBinding(
            ContentControl.ContentProperty,
            new Binding("[" + resourceKey + "]") { Source = LocalizationManager.Instance });
    }

    private string ResolveLeaveButtonResourceKey(ConfirmedMatchState? matchState)
    {
        if (matchState == ConfirmedMatchState.Finished)
        {
            return "MatchHistory_BackButton";
        }
        if (IsActiveRemoteParticipant())
        {
            return GameLeaveMatchButtonKey;
        }

        return RemoteMatchCloseViewKey;
    }

    private bool IsActiveRemoteParticipant()
    {
        return _remoteGuestName == null
            && _remoteInformation?.AssignedSeatNumber != null;
    }

    private void NotifyConclusionIfFinished(MatchStateSnapshot state)
    {
        if (state.State != ConfirmedMatchState.Finished
            || _remoteConclusionShown
            || _remoteLeaving
            || !IsLoaded)
        {
            return;
        }

        _remoteConclusionShown = true;
        _remoteRefreshTimer.Stop();
        if (state.Conclusion == null)
        {
            return;
        }

        ShowRemoteConclusion(state.Conclusion);
        if (IsLoaded)
        {
            NavigateToMainMenu();
        }
    }

    private void ShowRemoteConclusion(RemoteMatchConclusion conclusion)
    {
        if (conclusion.WinnerSeatNumber == null)
        {
            _dialogService.ShowInformation(
                this,
                GameMatchResultTitleKey,
                "Game_DrawResultMessage");
            return;
        }

        string winnerName = RemotePlayerName(conclusion.WinnerSeatNumber.Value);
        if (conclusion.Reason == RemoteMatchEndReason.Inactivity)
        {
            _dialogService.ShowInformation(
                this,
                GameMatchResultTitleKey,
                "Game_InactivityResultMessage",
                winnerName);
            return;
        }

        _dialogService.ShowInformation(
            this,
            "Game_VictoryTitle",
            "Game_VictoryMessage",
            winnerName);
    }

    private async Task LeaveRemoteMatchAsync()
    {
        if (_remoteLeaving)
        {
            return;
        }
        if (IsRemoteMatchFinished())
        {
            NavigateToMainMenu();
            return;
        }
        if (!CanForfeitRemoteMatch())
        {
            CloseRemoteView();
            return;
        }
        if (!ConfirmRemoteAbandonment())
        {
            return;
        }

        await ExecuteRemoteForfeitAsync();
    }

    private bool IsRemoteMatchFinished()
    {
        return _remoteClient.HasMatchState(_remoteMatchId)
            && _remoteClient.ReadMatchState(_remoteMatchId).State == ConfirmedMatchState.Finished;
    }

    private bool CanForfeitRemoteMatch()
    {
        return IsActiveRemoteParticipant()
            && _remoteClient.HasMatchState(_remoteMatchId)
            && _remoteClient.ReadMatchState(_remoteMatchId).State
                == ConfirmedMatchState.InProgress;
    }

    private bool ConfirmRemoteAbandonment()
    {
        int opponentSeat = _remoteInformation?.AssignedSeatNumber == 1 ? 2 : 1;
        string opponentName = RemotePlayerName(opponentSeat);
        ConfirmationDialogOptions options = new ConfirmationDialogOptions(
            GameLeaveConfirmationTitleKey,
            "Game_LeaveConfirmationMessage",
            GameLeaveMatchButtonKey,
            GameContinueMatchButtonKey)
        {
            MessageArguments = new object[] { opponentName }
        };
        return _dialogService.Confirm(this, options);
    }

    private async Task ExecuteRemoteForfeitAsync()
    {
        _remoteLeaving = true;
        leaveMatchButton.IsEnabled = false;
        try
        {
            MatchStatus status = await _remoteClient.ForfeitMatch(_remoteMatchId);
            if (!IsLoaded)
            {
                return;
            }

            await HandleForfeitResultAsync(status);
        }
        finally
        {
            _remoteLeaving = false;
            leaveMatchButton.IsEnabled = IsLoaded;
        }
    }

    private async Task HandleForfeitResultAsync(MatchStatus status)
    {
        if (status == MatchStatus.Success)
        {
            _remoteConclusionShown = true;
            _remoteRefreshTimer.Stop();
            NavigateToMainMenu();
            return;
        }
        if (status == MatchStatus.ServiceUnavailable)
        {
            _dialogService.ShowError(
                this,
                AbandonMatchFailedTitleKey,
                "AbandonMatch_ServerConnectionFailedMessage");
            return;
        }
        if (status == MatchStatus.MatchNotInProgress)
        {
            await LoadRemoteMatch();
            return;
        }

        string messageKey = RemoteMatchMessages.Key(status, _remoteGuestName != null);
        _remoteStatusKey = messageKey;
        UpdateRemoteStatus();
        _dialogService.ShowError(
            this,
            AbandonMatchFailedTitleKey,
            messageKey);
    }

    private void CloseRemoteView()
    {
        ConfirmationDialogOptions options = new ConfirmationDialogOptions(
            GameLeaveConfirmationTitleKey,
            RemoteMatchCloseViewKey,
            RemoteMatchCloseViewKey,
            GameContinueMatchButtonKey);
        if (_dialogService.Confirm(this, options))
        {
            NavigateToMainMenu();
        }
    }

    private void NavigateToMainMenu()
    {
        NavigationService?.Navigate(_remoteGuestName == null
            ? new MainMenuPage()
            : new MainMenuPage(_remoteGuestName));
    }
}
