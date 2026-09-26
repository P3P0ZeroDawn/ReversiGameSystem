using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using ServiceChatMessage = ReversiGameSystem.Services.ChatMessage;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays an active Reversi match using the current player's visual preferences.
/// </summary>
public partial class GamePage : Page
{
    private readonly MatchSession _session;
    private readonly DialogService _dialogService =
        new DialogService(LocalizationManager.Instance);
    private readonly GameMoveService _gameMoveService = new GameMoveService();
    private readonly InstructionsService _instructionsService =
        new InstructionsService(key => LocalizationManager.Instance[key]);
    private readonly AbandonMatchService _abandonMatchService =
        new AbandonMatchService();
    private readonly ChatService _chatService;
    private readonly DispatcherTimer _turnTimer;
    private readonly DispatcherTimer _systemMoveTimer;
    private readonly List<ServiceChatMessage> _visibleChatMessages =
        new List<ServiceChatMessage>();
    private int _remainingSeconds;
    private string? _statusResourceKey;
    private object[] _statusArguments = Array.Empty<object>();
    private bool _resultHandled;
    private bool _movesSuspended;

    internal GamePage(MatchSession session)
    {
        InitializeComponent();
        _session = session;
        _chatService = new ChatService(session, ChatPreviewState.GetStore());
        _turnTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _turnTimer.Tick += OnTurnTimerTick;
        _systemMoveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(600)
        };
        _systemMoveTimer.Tick += OnSystemMoveTimerTick;
        gameBoardControl.SquareSelected += OnBoardSquareSelected;
        Unloaded += OnPageUnloaded;

        if (_session.GuestName != null)
        {
            chatPanel.Visibility = Visibility.Collapsed;
            chatSpacingColumn.Width = new GridLength(0);
            chatColumn.Width = new GridLength(0);
        }
        else
        {
            _chatService.MessageReceived += OnChatMessageReceived;
            if (!LoadChatMessages())
            {
                _chatService.MessageReceived -= OnChatMessageReceived;
            }
        }

        creatorNameTextBlock.Text = FormatPlayerName(_session.CreatorName);
        BeginTurn();
    }

    private void BeginTurn()
    {
        _turnTimer.Stop();
        _systemMoveTimer.Stop();
        if (_session.State != MatchState.InProgress)
        {
            RefreshGameDisplay();
            ShowMatchResultAndReturn();
            return;
        }

        TurnPreparationResult preparation = _gameMoveService.PrepareCurrentTurn(
            _session,
            out DiscColor? skippedColor);
        if (preparation == TurnPreparationResult.TurnPassed && skippedColor.HasValue)
        {
            SetStatus(
                "Game_TurnSkippedMessage",
                _session.GetPlayerName(skippedColor.Value));
        }

        if (preparation == TurnPreparationResult.MatchFinished)
        {
            RefreshGameDisplay();
            ShowMatchResultAndReturn();
            return;
        }

        _remainingSeconds = _session.TurnTimeSeconds;
        RefreshGameDisplay();
        if (_session.UsesSystemOpponent
            && _session.CurrentTurnColor != _session.LocalPlayerColor)
        {
            _systemMoveTimer.Start();
            return;
        }

        _turnTimer.Start();
    }

    private void RefreshGameDisplay()
    {
        UpdateLocalizedSessionText();
        bool isLocalTurn = _session.State == MatchState.InProgress
            && _session.CurrentTurnColor == _session.LocalPlayerColor
            && !_movesSuspended;
        if (isLocalTurn)
        {
            IReadOnlyList<BoardPosition> validMoves =
                _gameMoveService.GetCurrentValidMoves(_session);
            gameBoardControl.ShowInteractiveBoard(
                _session.Board,
                _session.LocalPlayerColor,
                validMoves);
            return;
        }

        gameBoardControl.ShowBoard(_session.Board, _session.LocalPlayerColor);
    }

    private void UpdateLocalizedSessionText()
    {
        LocalizationManager localization = LocalizationManager.Instance;
        creatorNameTextBlock.Text = FormatPlayerName(_session.CreatorName);
        opponentNameTextBlock.Text = GetOpponentName();
        creatorScoreTextBlock.Text = string.Format(
            localization["Game_ScoreFormat"],
            _session.Board.Count(DiscColor.Black));
        opponentScoreTextBlock.Text = string.Format(
            localization["Game_ScoreFormat"],
            _session.Board.Count(DiscColor.White));
        turnTextBlock.Text = _session.State == MatchState.InProgress
            ? string.Format(
                localization["Game_CurrentTurnFormat"],
                FormatPlayerName(_session.GetPlayerName(_session.CurrentTurnColor)))
            : localization["Game_MatchFinishedStatus"];
        turnTimeTextBlock.Text = string.Format(
            localization["Game_RemainingTimeFormat"],
            Math.Max(0, _remainingSeconds));
        UpdateLocalizedStatus();
        if (_session.GuestName == null)
        {
            RenderChatMessages();
        }
    }

    private void OnBoardSquareSelected(
        object? sender,
        BoardSquareSelectedEventArgs e)
    {
        GameMoveResult result = _gameMoveService.TryMakeMove(
            _session,
            _session.LocalPlayerColor,
            e.Position,
            out MatchMove? move,
            out DiscColor? skippedColor);
        if (result == GameMoveResult.InvalidPosition)
        {
            SetStatus("Game_InvalidPositionMessage");
            UpdateLocalizedStatus();
            return;
        }

        if (result == GameMoveResult.ServerConnectionFailed)
        {
            _turnTimer.Stop();
            _movesSuspended = true;
            _dialogService.ShowError(
                this,
                "Game_ServerConnectionFailedTitle",
                "Game_ServerConnectionFailedMessage");
            RefreshGameDisplay();
            return;
        }

        if (result == GameMoveResult.PersistenceError)
        {
            _turnTimer.Stop();
            _movesSuspended = true;
            _dialogService.ShowError(
                this,
                "Game_MovementPersistenceErrorTitle",
                "Game_MovementPersistenceErrorMessage");
            RefreshGameDisplay();
            return;
        }

        if (result != GameMoveResult.Success || move == null)
        {
            return;
        }

        _turnTimer.Stop();
        if (skippedColor.HasValue)
        {
            SetStatus(
                "Game_TurnSkippedMessage",
                _session.GetPlayerName(skippedColor.Value));
        }
        else
        {
            ClearStatus();
        }

        BeginTurn();
    }

    private void OnTurnTimerTick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        UpdateLocalizedSessionText();
        if (_remainingSeconds > 0)
        {
            return;
        }

        _turnTimer.Stop();
        DiscColor timedOutColor = _session.CurrentTurnColor;
        TurnTimeoutResult result = _gameMoveService.HandleTimeout(
            _session,
            out MatchMove? move,
            out DiscColor? skippedColor);
        if (result == TurnTimeoutResult.ResultPersistenceError)
        {
            _movesSuspended = true;
            _dialogService.ShowError(
                this,
                "Game_MatchResultPersistenceErrorTitle",
                "Game_MatchResultPersistenceErrorMessage");
            RefreshGameDisplay();
            return;
        }

        if (result == TurnTimeoutResult.MatchFinishedByInactivity
            || result == TurnTimeoutResult.MatchFinished)
        {
            RefreshGameDisplay();
            ShowMatchResultAndReturn();
            return;
        }

        if (result == TurnTimeoutResult.TurnPassed && skippedColor.HasValue)
        {
            SetStatus(
                "Game_TurnSkippedMessage",
                _session.GetPlayerName(skippedColor.Value));
        }
        else if (result == TurnTimeoutResult.AutomaticMove && move != null)
        {
            if (skippedColor.HasValue)
            {
                SetStatus(
                    "Game_AutomaticMoveAndTurnSkippedMessage",
                    _session.GetPlayerName(timedOutColor),
                    move.Position.ToNotation(),
                    _session.GetPlayerName(skippedColor.Value));
            }
            else
            {
                SetStatus(
                    "Game_AutomaticMoveMessage",
                    _session.GetPlayerName(timedOutColor),
                    move.Position.ToNotation());
            }
        }

        BeginTurn();
    }

    private void OnSystemMoveTimerTick(object? sender, EventArgs e)
    {
        _systemMoveTimer.Stop();
        string systemName = _session.GetPlayerName(_session.CurrentTurnColor);
        GameMoveResult result = _gameMoveService.TryMakeSystemMove(
            _session,
            out MatchMove? move,
            out DiscColor? skippedColor);
        if (result != GameMoveResult.Success || move == null)
        {
            _movesSuspended = true;
            _dialogService.ShowError(
                this,
                "Game_MovementPersistenceErrorTitle",
                "Game_MovementPersistenceErrorMessage");
            RefreshGameDisplay();
            return;
        }

        if (skippedColor.HasValue)
        {
            SetStatus(
                "Game_SystemMoveAndTurnSkippedMessage",
                systemName,
                move.Position.ToNotation(),
                _session.GetPlayerName(skippedColor.Value));
        }
        else
        {
            SetStatus(
                "Game_SystemMoveMessage",
                systemName,
                move.Position.ToNotation());
        }

        BeginTurn();
    }

    private void SetStatus(string resourceKey, params object[] arguments)
    {
        _statusResourceKey = resourceKey;
        _statusArguments = arguments;
    }

    private void ClearStatus()
    {
        _statusResourceKey = null;
        _statusArguments = Array.Empty<object>();
    }

    private void UpdateLocalizedStatus()
    {
        if (_statusResourceKey == null)
        {
            turnStatusTextBlock.Text = string.Empty;
            return;
        }

        object[] localizedArguments = new object[_statusArguments.Length];
        for (int index = 0; index < _statusArguments.Length; index++)
        {
            localizedArguments[index] = _statusArguments[index] is string text
                ? FormatPlayerName(text)
                : _statusArguments[index];
        }

        turnStatusTextBlock.Text = string.Format(
            LocalizationManager.Instance[_statusResourceKey],
            localizedArguments);
    }

    private void ShowMatchResultAndReturn()
    {
        if (_resultHandled || _session.State != MatchState.Finished)
        {
            return;
        }

        _resultHandled = true;
        _turnTimer.Stop();
        _systemMoveTimer.Stop();
        if (_session.WinnerName == null)
        {
            _dialogService.ShowInformation(
                this,
                "Game_MatchResultTitle",
                "Game_DrawResultMessage");
        }
        else
        {
            string messageKey = _session.EndReason == MatchEndReason.Inactivity
                ? "Game_InactivityResultMessage"
                : "Game_VictoryMessage";
            _dialogService.ShowInformation(
                this,
                "Game_MatchResultTitle",
                messageKey,
                FormatPlayerName(_session.WinnerName));
        }

        NavigateToMenu();
    }

    private void OnSendMessageClick(object sender, RoutedEventArgs e)
    {
        SendChatMessage();
    }

    private void OnChatMessageKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        SendChatMessage();
    }

    private void SendChatMessage()
    {
        ChatSendResult result = _chatService.Send(
            chatMessageTextBox.Text,
            out ServiceChatMessage? message,
            out DateTime? muteEndsAtUtc);
        if (result == ChatSendResult.Success && message != null)
        {
            chatMessageTextBox.Clear();
            return;
        }

        HandleChatSendFailure(result, muteEndsAtUtc);
    }

    private bool LoadChatMessages()
    {
        ChatReadResult result = _chatService.TryGetMessages(
            out IReadOnlyList<ServiceChatMessage> messages);
        _visibleChatMessages.Clear();
        if (result == ChatReadResult.Success)
        {
            _visibleChatMessages.AddRange(messages);
            RenderChatMessages();
            return true;
        }

        chatMessagesListBox.Items.Clear();
        string prefix = result == ChatReadResult.Unauthorized
            ? "ChatRead_Unauthorized"
            : result == ChatReadResult.ServerConnectionFailed
                ? "ChatRead_ServerConnectionFailed"
                : "ChatRead_RetrievalError";
        _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
        return false;
    }

    private void OnChatMessageReceived(ServiceChatMessage message)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() => OnChatMessageReceived(message)));
            return;
        }

        if (!_chatService.CanReadMessage(message)
            || _visibleChatMessages.Exists(item => item.Id == message.Id))
        {
            return;
        }

        _visibleChatMessages.Add(message);
        _visibleChatMessages.Sort((first, second) =>
        {
            int timestampComparison = first.SentAtUtc.CompareTo(second.SentAtUtc);
            return timestampComparison != 0
                ? timestampComparison
                : string.CompareOrdinal(first.Id, second.Id);
        });
        RenderChatMessages();
    }

    private void RenderChatMessages()
    {
        chatMessagesListBox.Items.Clear();
        if (_visibleChatMessages.Count == 0)
        {
            chatMessagesListBox.Items.Add(
                LocalizationManager.Instance["Chat_NoMessagesMessage"]);
            return;
        }

        foreach (ServiceChatMessage message in _visibleChatMessages)
        {
            chatMessagesListBox.Items.Add(string.Format(
                LocalizationManager.Instance["Chat_MessageFormat"],
                message.SenderUsername,
                message.Content));
        }

        chatMessagesListBox.ScrollIntoView(
            chatMessagesListBox.Items[chatMessagesListBox.Items.Count - 1]);
    }

    private void HandleChatSendFailure(
        ChatSendResult result,
        DateTime? muteEndsAtUtc)
    {
        switch (result)
        {
            case ChatSendResult.EmptyMessage:
                _dialogService.ShowWarning(
                    this,
                    "Chat_EmptyMessageTitle",
                    "Chat_EmptyMessageMessage");
                break;
            case ChatSendResult.MessageTooLong:
                _dialogService.ShowWarning(
                    this,
                    "Chat_MessageTooLongTitle",
                    "Chat_MessageTooLongMessage");
                break;
            case ChatSendResult.Unauthorized:
                _dialogService.ShowError(
                    this,
                    "Chat_UnauthorizedTitle",
                    "Chat_UnauthorizedMessage");
                break;
            case ChatSendResult.PlayerMuted:
                string muteEnd = muteEndsAtUtc.HasValue
                    ? muteEndsAtUtc.Value.ToLocalTime().ToString(
                        "g",
                        CultureInfo.GetCultureInfo(
                            LocalizationManager.Instance.CultureName))
                    : LocalizationManager.Instance["AccountInformation_UnavailableValue"];
                _dialogService.ShowWarning(
                    this,
                    "Chat_PlayerMutedTitle",
                    "Chat_PlayerMutedMessage",
                    muteEnd);
                break;
            case ChatSendResult.MessageBlocked:
                _dialogService.ShowWarning(
                    this,
                    "Chat_MessageBlockedTitle",
                    "Chat_MessageBlockedMessage");
                break;
            case ChatSendResult.ServerConnectionFailed:
                _dialogService.ShowError(
                    this,
                    "Chat_ServerConnectionFailedTitle",
                    "Chat_ServerConnectionFailedMessage");
                break;
            case ChatSendResult.MessagePersistenceError:
                _dialogService.ShowError(
                    this,
                    "Chat_MessagePersistenceErrorTitle",
                    "Chat_MessagePersistenceErrorMessage");
                break;
            case ChatSendResult.ModerationPersistenceError:
                _dialogService.ShowError(
                    this,
                    "Chat_ModerationPersistenceErrorTitle",
                    "Chat_ModerationPersistenceErrorMessage");
                break;
            case ChatSendResult.MutePersistenceError:
                _dialogService.ShowError(
                    this,
                    "Chat_MutePersistenceErrorTitle",
                    "Chat_MutePersistenceErrorMessage");
                break;
        }
    }

    private void OnInformationClick(object sender, RoutedEventArgs e)
    {
        InstructionsLoadResult result = _instructionsService.TryLoadForMatch(
            LocalizationManager.Instance[_session.LocalPlayerColor == DiscColor.Black
                ? "Game_DarkPiecesName"
                : "Game_LightPiecesName"],
            _session.TurnTimeSeconds,
            out InstructionsContent? content);
        if (result != InstructionsLoadResult.Success || content == null)
        {
            _dialogService.ShowError(
                this,
                "Instructions_LoadingErrorTitle",
                "Instructions_LoadingErrorMessage");
            return;
        }

        _dialogService.ShowInformationText(this, content.Title, content.Message);
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string previousCulture = LocalizationManager.Instance.CultureName;
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
        if (LocalizationManager.Instance.CultureName != previousCulture)
        {
            UpdateLocalizedSessionText();
        }
    }

    private void OnLeaveMatchClick(object sender, RoutedEventArgs e)
    {
        string opponentName = GetOpponentName();
        ConfirmationDialogOptions options = new ConfirmationDialogOptions(
            "Game_LeaveConfirmationTitle",
            "Game_LeaveConfirmationMessage",
            "Game_LeaveMatchButton",
            "Game_ContinueMatchButton")
        {
            MessageArguments = new object[] { opponentName }
        };
        if (!_dialogService.Confirm(this, options))
        {
            return;
        }

        AbandonMatchResult result = _abandonMatchService.TryAbandon(
            _session,
            _session.LocalPlayerColor);
        if (result == AbandonMatchResult.Success
            || result == AbandonMatchResult.MatchAlreadyFinished)
        {
            RefreshGameDisplay();
            ShowMatchResultAndReturn();
            return;
        }

        if (result == AbandonMatchResult.ServerConnectionFailed)
        {
            _dialogService.ShowError(
                this,
                "AbandonMatch_ServerConnectionFailedTitle",
                "AbandonMatch_ServerConnectionFailedMessage");
            return;
        }

        _dialogService.ShowError(
            this,
            "AbandonMatch_PersistenceErrorTitle",
            "AbandonMatch_PersistenceErrorMessage");
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _turnTimer.Stop();
        _systemMoveTimer.Stop();
        gameBoardControl.SquareSelected -= OnBoardSquareSelected;
        _chatService.MessageReceived -= OnChatMessageReceived;
    }

    private string GetOpponentName()
    {
        if (_session.LocalPlayerColor == DiscColor.White)
        {
            return FormatPlayerName(_session.CreatorName);
        }

        string opponentName = _session.OpponentName
            ?? LocalizationManager.Instance["Game_SystemPlayerName"];
        return FormatPlayerName(opponentName);
    }

    private string FormatPlayerName(string playerName)
    {
        return _session.GuestName != null
            && string.Equals(
                playerName,
                _session.GuestName,
                StringComparison.Ordinal)
            ? GuestNameFormatter.Format(playerName)
            : playerName;
    }

    private void NavigateToMenu()
    {
        NavigationService?.Navigate(_session.GuestName == null
            ? new MainMenuPage()
            : new MainMenuPage(_session.GuestName));
    }
}
