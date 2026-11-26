using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ReversiGameSystem.Chat;
using ReversiGameSystem.Features.Gameplay;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using ChatMessage = ReversiGameSystem.Chat.ChatMessage;

namespace ReversiGameSystem.Views;

public partial class GamePage : Page
{
    private const string RecoveryOperation = "GameplayConnectionRecovery";
    private const int ClockRefreshSeconds = 1;
    private const char BlackCell = 'B';
    private const char WhiteCell = 'W';
    private const char EmptyCell = '.';
    private const string AbandonMatchServerConnectionFailedTitleKey =
        "AbandonMatch_ServerConnectionFailedTitle";
    private const string ChatMessageFormatKey = "Chat_MessageFormat";
    private const string ChatNoMessagesMessageKey = "Chat_NoMessagesMessage";
    private const string ChatPlayerMutedMessageKey = "Chat_PlayerMutedMessage";
    private const string ChatPlayerMutedTitleKey = "Chat_PlayerMutedTitle";
    private const string GameChatTitleKey = "Game_ChatTitle";
    private const string GameContinueMatchButtonKey = "Game_ContinueMatchButton";
    private const string GameCurrentTurnFormatKey = "Game_CurrentTurnFormat";
    private const string GameDarkPiecesNameKey = "Game_DarkPiecesName";
    private const string GameDrawResultMessageKey = "Game_DrawResultMessage";
    private const string GameInactivityResultMessageKey = "Game_InactivityResultMessage";
    private const string GameLeaveConfirmationMessageKey = "Game_LeaveConfirmationMessage";
    private const string GameLeaveConfirmationTitleKey = "Game_LeaveConfirmationTitle";
    private const string GameLeaveMatchButtonKey = "Game_LeaveMatchButton";
    private const string GameLightPiecesNameKey = "Game_LightPiecesName";
    private const string GameMatchFinishedStatusKey = "Game_MatchFinishedStatus";
    private const string GameMatchResultTitleKey = "Game_MatchResultTitle";
    private const string GamePresenceConnectedKey = "Game_PresenceConnected";
    private const string GamePresenceDisconnectedKey = "Game_PresenceDisconnected";
    private const string GamePresenceUnknownKey = "Game_PresenceUnknown";
    private const string GameScoreFormatKey = "Game_ScoreFormat";
    private const string GameTitleKey = "Game_Title";
    private const string GameVictoryMessageKey = "Game_VictoryMessage";
    private const string GameVictoryTitleKey = "Game_VictoryTitle";
    private const string MatchHistoryBackButtonKey = "MatchHistory_BackButton";
    private const string RemoteMatchCloseViewKey = "RemoteMatch_CloseView";
    private const string RemoteMatchMovesUnavailableKey = "RemoteMatch_MovesUnavailable";
    private const string GeneralDateTimeFormat = "g";
    private readonly GameViewModel _viewModel;
    private readonly string? _guestName;
    private readonly DialogService _dialogs = new DialogService(LocalizationManager.Instance);
    private readonly InstructionsService _instructions =
        new InstructionsService(key => LocalizationManager.Instance[key]);
    private readonly DispatcherTimer _clock = new DispatcherTimer
    {
        Interval = TimeSpan.FromSeconds(ClockRefreshSeconds)
    };
    private readonly DispatcherTimer _connectionCheck = new DispatcherTimer
    {
        Interval = MatchConnectionPolicy.ConnectionCheckInterval
    };
    private bool _conclusionShown;

    internal GamePage(
        GameplayClient gameplay, MatchChatClient chat, long matchId, string? guestName)
    {
        _guestName = guestName;
        _viewModel = new GameViewModel(gameplay, chat, matchId, guestName != null,
            new DispatcherSynchronizationContext(Dispatcher));
        _viewModel.UnexpectedRecoveryFailure += OnUnexpectedGameplayFailure;
        InitializeComponent();
        chatMessageTextBox.MaxLength = ChatStore.MaximumMessageLength;
        DataContext = _viewModel;
        chatPanel.Visibility = Visibility.Hidden;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        _clock.Tick += OnClockTick;
        _connectionCheck.Tick += OnConnectionCheckTick;
    }

    private async void OnLoaded(object sender, RoutedEventArgs arguments)
    {
        _viewModel.PropertyChanged += OnViewModelChanged;
        gameBoardControl.SquareSelected += OnSquareSelected;
        await _viewModel.ActivateMatch();
        if (IsLoaded)
        {
            RenderMatch();
            RenderMessages();
            _clock.Start();
            _connectionCheck.Start();
        }
    }

    private async void OnUnloaded(object sender, RoutedEventArgs arguments)
    {
        _clock.Stop();
        _connectionCheck.Stop();
        _viewModel.PropertyChanged -= OnViewModelChanged;
        gameBoardControl.SquareSelected -= OnSquareSelected;
        await _viewModel.DeactivateMatch();
    }

    private static void OnUnexpectedGameplayFailure(Exception exception)
    {
        if (Application.Current is App application)
        {
            application.ReportBackgroundFailure(exception, RecoveryOperation);
        }
        else
        {
            throw new InvalidOperationException("The client failure boundary is unavailable.",
                exception);
        }
    }

    private void OnClockTick(object? sender, EventArgs arguments)
    {
        _viewModel.UpdateClock(DateTime.UtcNow);
    }

    private async void OnConnectionCheckTick(object? sender, EventArgs arguments)
    {
        await _viewModel.CheckMatchConnection();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs arguments)
    {
        if (!IsLoaded)
        {
            return;
        }
        if (string.IsNullOrEmpty(arguments.PropertyName))
        {
            RenderMatch();
            RenderMessages();
        }
        else if (arguments.PropertyName == nameof(GameViewModel.MessageHistory))
        {
            RenderMessages();
        }
    }

    private void RenderMatch()
    {
        chatPanel.Visibility = _viewModel.IsChatVisible ? Visibility.Visible : Visibility.Hidden;
        if (!_viewModel.IsReady)
        {
            return;
        }
        MatchStateSnapshot state = _viewModel.State;
        LocalizationManager localization = LocalizationManager.Instance;
        creatorNameTextBlock.Text = _viewModel.GetPlayerDisplayName(GameViewModel.CreatorSeat);
        opponentNameTextBlock.Text = _viewModel.GetPlayerDisplayName(GameViewModel.OpponentSeat);
        creatorScoreTextBlock.Text = string.Format(
            localization[GameScoreFormatKey], state.BlackPieceCount);
        opponentScoreTextBlock.Text = string.Format(
            localization[GameScoreFormatKey], state.WhitePieceCount);
        turnTextBlock.Text = state.CurrentTurnSeatNumber.HasValue
            ? string.Format(localization[GameCurrentTurnFormatKey],
                _viewModel.GetPlayerDisplayName(state.CurrentTurnSeatNumber.Value))
            : localization[GameMatchFinishedStatusKey];
        string[] rows = new string[ReversiBoard.Size];
        for (int row = 0; row < rows.Length; row++)
        {
            char[] cells = new char[ReversiBoard.Size];
            for (int column = 0; column < cells.Length; column++)
            {
                cells[column] = state.Board[row * ReversiBoard.Size + column] switch
                {
                    ConfirmedBoardCell.Black => BlackCell,
                    ConfirmedBoardCell.White => WhiteCell,
                    _ => EmptyCell
                };
            }
            rows[row] = new string(cells);
        }
        ReversiBoard board = ReversiBoard.FromRows(rows);
        DiscColor localColor =
            _viewModel.Information.AssignedSeatNumber == GameViewModel.OpponentSeat
            ? DiscColor.White : DiscColor.Black;
        gameBoardControl.SystemPlayerColor = _viewModel.SystemPlayerColor;
        if (_viewModel.CanPlacePiece)
        {
            gameBoardControl.ShowInteractiveBoard(
                board, localColor, board.GetValidMoves(localColor));
        }
        else
        {
            gameBoardControl.ShowBoard(board, localColor);
        }
        RenderParticipantConnection(creatorConnectionTextBlock, GameViewModel.CreatorSeat);
        RenderParticipantConnection(opponentConnectionTextBlock, GameViewModel.OpponentSeat);
        string leaveKey = _viewModel.IsFinished
            ? MatchHistoryBackButtonKey : RemoteMatchCloseViewKey;
        if (!_viewModel.IsFinished && _viewModel.IsParticipant)
        {
            leaveKey = GameLeaveMatchButtonKey;
        }
        leaveMatchButton.Content = localization[leaveKey];
        ShowConclusion();
    }

    private void RenderParticipantConnection(TextBlock textBlock, int seatNumber)
    {
        bool isSystem = _viewModel.Information.Participants.Any(
            player => player.SeatNumber == seatNumber && player.IsSystem);
        textBlock.Visibility = isSystem ? Visibility.Hidden : Visibility.Visible;
        ParticipantConnectionState state = _viewModel.ReadParticipantConnection(seatNumber);
        string resourceKey = GamePresenceUnknownKey;
        Brush foreground = Brushes.Gray;
        if (state == ParticipantConnectionState.Connected)
        {
            resourceKey = GamePresenceConnectedKey;
            foreground = Brushes.ForestGreen;
        }
        else if (state == ParticipantConnectionState.Disconnected)
        {
            resourceKey = GamePresenceDisconnectedKey;
            foreground = Brushes.IndianRed;
        }
        textBlock.Text = LocalizationManager.Instance[resourceKey];
        textBlock.Foreground = foreground;
    }

    private async void OnSquareSelected(object? sender, BoardSquareSelectedEventArgs arguments)
    {
        MatchStatus status = await _viewModel.PlacePiece(arguments.Position);
        if (IsLoaded && status != MatchStatus.Success && status != MatchStatus.ServiceUnavailable
            && status != MatchStatus.StaleMatchStateVersion && status != MatchStatus.NotPlayersTurn)
        {
            _dialogs.ShowWarning(this, GameTitleKey,
                RemoteMatchMessages.GetMessageResourceKey(status));
        }
    }

    private void RenderMessages()
    {
        if (!_viewModel.IsChatVisible)
        {
            return;
        }
        chatMessagesListBox.Items.Clear();
        foreach (ChatMessage message in _viewModel.MessageHistory)
        {
            chatMessagesListBox.Items.Add(string.Format(
                LocalizationManager.Instance[ChatMessageFormatKey],
                _viewModel.GetPlayerDisplayName(message.SenderSeatNumber), message.Content));
        }
        if (chatMessagesListBox.Items.Count == 0)
        {
            chatMessagesListBox.Items.Add(LocalizationManager.Instance[ChatNoMessagesMessageKey]);
        }
        chatMessagesListBox.ScrollIntoView(
            chatMessagesListBox.Items[chatMessagesListBox.Items.Count - 1]);
    }

    private void OnChatScrollChanged(object sender, ScrollChangedEventArgs arguments)
    {
        if (arguments.ExtentHeightChange != 0 && arguments.OriginalSource is ScrollViewer scroll)
        {
            scroll.ScrollToEnd();
        }
    }

    private async void OnSendMessageClick(object sender, RoutedEventArgs arguments)
    {
        await SendChatMessage();
    }

    private async void OnChatMessageKeyDown(object sender, KeyEventArgs arguments)
    {
        if (arguments.Key == Key.Enter)
        {
            arguments.Handled = true;
            await SendChatMessage();
        }
    }

    private async Task SendChatMessage()
    {
        if (!_viewModel.CanSendMessage)
        {
            return;
        }
        if (sendMessageButton.IsKeyboardFocused)
        {
            chatMessageTextBox.Focus();
        }
        ChatStatus status = await _viewModel.SendChatMessage();
        if (!IsLoaded || status == ChatStatus.Success || status == ChatStatus.ServiceUnavailable)
        {
            return;
        }
        if (status == ChatStatus.PlayerMuted && _viewModel.MuteEndsAtUtc.HasValue)
        {
            string endTime = _viewModel.MuteEndsAtUtc.Value.ToLocalTime().ToString(
                GeneralDateTimeFormat,
                CultureInfo.GetCultureInfo(LocalizationManager.Instance.CultureName));
            _dialogs.ShowWarning(this, ChatPlayerMutedTitleKey, ChatPlayerMutedMessageKey, endTime);
        }
        else
        {
            _dialogs.ShowError(this, GameChatTitleKey,
                RemoteMatchMessages.GetMessageResourceKey(status));
        }
    }

    private void OnInformationClick(object sender, RoutedEventArgs arguments)
    {
        if (!_viewModel.IsReady)
        {
            _dialogs.ShowInformation(this, GameTitleKey, RemoteMatchMovesUnavailableKey);
            return;
        }
        string colorKey = _viewModel.Information.AssignedSeatNumber == GameViewModel.OpponentSeat
            ? GameLightPiecesNameKey : GameDarkPiecesNameKey;
        try
        {
            InstructionsContent content = _instructions.LoadForMatch(
                LocalizationManager.Instance[colorKey], _viewModel.Information.TurnDurationSeconds);
            _dialogs.ShowInformationText(this, content.Title, content.Message);
        }
        catch (Exception exception) when (exception is MissingManifestResourceException
            or MissingSatelliteAssemblyException or FormatException)
        {
            ClientRuntime.Errors.ReportOperationFailure(exception, nameof(InstructionsService));
        }
    }

    private async void OnLanguageSelectionChanged(
        object sender, SelectionChangedEventArgs arguments)
    {
        await LanguageChangeService.ConfirmSelection((ComboBox)sender);
    }

    private void ShowConclusion()
    {
        if (!_viewModel.IsFinished || _conclusionShown || _viewModel.IsLeaving || !IsLoaded)
        {
            return;
        }
        RemoteMatchConclusion? conclusion = _viewModel.State.Conclusion;
        if (conclusion == null)
        {
            return;
        }
        _conclusionShown = true;
        string message = GameDrawResultMessageKey;
        string title = GameMatchResultTitleKey;
        object[] arguments = Array.Empty<object>();
        if (conclusion.WinnerSeatNumber.HasValue)
        {
            message = conclusion.Reason == RemoteMatchEndReason.Inactivity
                ? GameInactivityResultMessageKey : GameVictoryMessageKey;
            title = conclusion.Reason == RemoteMatchEndReason.Inactivity
                ? GameMatchResultTitleKey : GameVictoryTitleKey;
            arguments = new object[]
                {
                    _viewModel.GetPlayerDisplayName(conclusion.WinnerSeatNumber.Value)
                };
        }
        _dialogs.ShowInformation(this, title, message, arguments);
        NavigateToMenu();
    }

    private async void OnLeaveMatchClick(object sender, RoutedEventArgs arguments)
    {
        if (_viewModel.IsLeaving)
        {
            return;
        }
        if (_viewModel.IsRecovering || _viewModel.RecoveryFailed || _viewModel.IsFinished)
        {
            NavigateToMenu();
            return;
        }
        string messageKey = RemoteMatchCloseViewKey;
        string confirmKey = RemoteMatchCloseViewKey;
        object[] messageArguments = Array.Empty<object>();
        if (_viewModel.CanForfeit)
        {
            messageKey = GameLeaveConfirmationMessageKey;
            confirmKey = GameLeaveMatchButtonKey;
            int opponent = _viewModel.Information.AssignedSeatNumber == GameViewModel.CreatorSeat
                ? GameViewModel.OpponentSeat : GameViewModel.CreatorSeat;
            messageArguments = new object[] { _viewModel.GetPlayerDisplayName(opponent) };
        }
        ConfirmationDialogOptions options = new ConfirmationDialogOptions(
            GameLeaveConfirmationTitleKey, messageKey, confirmKey, GameContinueMatchButtonKey)
        {
            MessageArguments = messageArguments
        };
        if (!_dialogs.Confirm(this, options))
        {
            return;
        }
        if (!_viewModel.CanForfeit)
        {
            NavigateToMenu();
            return;
        }
        MatchStatus status = await _viewModel.ForfeitMatch();
        if (IsLoaded && status == MatchStatus.Success)
        {
            _conclusionShown = true;
            NavigateToMenu();
        }
        else if (IsLoaded && status != MatchStatus.ServiceUnavailable
            && status != MatchStatus.MatchNotInProgress)
        {
            _dialogs.ShowError(this, AbandonMatchServerConnectionFailedTitleKey,
                RemoteMatchMessages.GetMessageResourceKey(status));
        }
    }

    private void NavigateToMenu()
    {
        NavigationService?.Navigate(_guestName == null
            ? new MainMenuPage() : new MainMenuPage(_guestName));
    }
}
