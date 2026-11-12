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
    private readonly RemoteMatchClient _remoteClient;
    private readonly long _remoteMatchId;
    private readonly string? _remoteGuestName;
    private readonly DispatcherTimer _remoteRefreshTimer = new DispatcherTimer
    {
        Interval = TimeSpan.FromSeconds(15)
    };
    private RemoteMatchInformation? _remoteInformation;
    private bool _remoteLoading;
    private bool _remoteSending;
    private bool _remoteChatReady;
    private DateTime? _remoteMutedUntil;
    private string? _remoteStatusKey;

    internal GamePage(RemoteMatchClient client, long matchId, string? guestName)
    {
        _remoteClient = client ?? throw new ArgumentNullException(nameof(client));
        _remoteMatchId = matchId;
        _remoteGuestName = guestName;
        InitializeComponent();
        // This path never constructs a local match, subscribes board input, or starts turn timers.
        sendMessageButton.IsEnabled = false;
        refreshRemoteMatchButton.Visibility = Visibility.Visible;
        creatorConnectionTextBlock.Visibility = Visibility.Visible;
        opponentConnectionTextBlock.Visibility = Visibility.Visible;
        leaveMatchButton.SetBinding(ContentControl.ContentProperty,
            new Binding("[RemoteMatch_CloseView]") { Source = LocalizationManager.Instance });
        turnTimeTextBlock.SetBinding(TextBlock.TextProperty,
            new Binding("[RemoteMatch_MovesUnavailable]") { Source = LocalizationManager.Instance });
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
        MatchResult<RemoteMatchInformation> informationResult =
            await _remoteClient.LoadRemoteMatchInformation(_remoteMatchId);
        MatchResult<MatchStateSnapshot> stateResult =
            await _remoteClient.LoadMatchState(_remoteMatchId);
        if (informationResult.Status == MatchStatus.Success
            && stateResult.Status == MatchStatus.Success)
        {
            _remoteInformation = informationResult.Value
                ?? throw new InvalidOperationException(
                    "A successful match result must contain its value.");
            if (!IsLoaded)
            {
                _remoteLoading = false;
                refreshRemoteMatchButton.IsEnabled = true;
                return;
            }
            RenderRemoteMatch();
            if (_remoteGuestName == null)
            {
                MatchStatus openStatus = await _remoteClient.OpenChatChannel(_remoteMatchId);
                MatchStatus refreshStatus = openStatus == MatchStatus.Success
                    ? await _remoteClient.RefreshChat(_remoteMatchId)
                    : openStatus;
                _remoteChatReady = IsLoaded && refreshStatus == MatchStatus.Success;
                if (_remoteChatReady)
                {
                    RenderRemoteMessages();
                }
                else
                {
                    _remoteStatusKey = RemoteMatchMessages.Key(
                        refreshStatus, _remoteGuestName != null);
                }
            }
            if (_remoteGuestName != null || _remoteChatReady)
            {
                _remoteStatusKey = null;
            }
        }
        else
        {
            _remoteChatReady = false;
            MatchStatus failure = informationResult.Status != MatchStatus.Success
                ? informationResult.Status
                : stateResult.Status;
            _remoteStatusKey = RemoteMatchMessages.Key(failure, _remoteGuestName != null);
        }
        _remoteLoading = false;
        refreshRemoteMatchButton.IsEnabled = true;
        sendMessageButton.IsEnabled = IsLoaded && _remoteChatReady && !_remoteSending;
        UpdateRemoteStatus();
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
        gameBoardControl.ShowBoard(ReversiBoard.FromRows(rows),
            _remoteInformation.AssignedSeatNumber == 2 ? DiscColor.White : DiscColor.Black);
        RenderParticipantConnections();
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
        turnStatusTextBlock.Text = _remoteMutedUntil > DateTime.UtcNow
            ? string.Format(LocalizationManager.Instance["Chat_PlayerMutedMessage"],
                _remoteMutedUntil.Value.ToLocalTime().ToString("g",
                    CultureInfo.GetCultureInfo(LocalizationManager.Instance.CultureName)))
            : _remoteStatusKey == null ? string.Empty : LocalizationManager.Instance[_remoteStatusKey];
    }

    private async Task SendRemoteChatMessage()
    {
        if (_remoteSending || !_remoteChatReady || _remoteGuestName != null)
        {
            return;
        }
        string content = chatMessageTextBox.Text.Trim();
        if (content.Length == 0 || content.Length > ChatStore.MaximumMessageLength)
        {
            HandleChatSendFailure(content.Length == 0 ? ChatSendResult.EmptyMessage : ChatSendResult.MessageTooLong, null);
            return;
        }
        if (_remoteMutedUntil > DateTime.UtcNow)
        {
            HandleChatSendFailure(ChatSendResult.PlayerMuted, _remoteMutedUntil);
            return;
        }
        _remoteSending = true;
        sendMessageButton.IsEnabled = false;
        chatMessageTextBox.IsEnabled = false;
        ChatSubmission submission =
            await _remoteClient.SendChatMessage(_remoteMatchId, content);
        if (submission.Status == MatchStatus.Success)
        {
            if (IsLoaded)
            {
                chatMessageTextBox.Clear();
                _remoteStatusKey = null;
                RenderRemoteMessages();
            }
        }
        else if (submission.Status == MatchStatus.PlayerMuted)
        {
            _remoteMutedUntil = submission.MuteEndsAtUtc;
            if (IsLoaded)
            {
                HandleChatSendFailure(ChatSendResult.PlayerMuted, _remoteMutedUntil);
            }
        }
        else
        {
            _remoteStatusKey = RemoteMatchMessages.Key(
                submission.Status, _remoteGuestName != null);
            if (IsLoaded)
            {
                _dialogService.ShowError(this, "Game_ChatTitle", _remoteStatusKey);
            }
        }
        _remoteSending = false;
        chatMessageTextBox.IsEnabled = true;
        sendMessageButton.IsEnabled = IsLoaded && _remoteChatReady;
        UpdateRemoteStatus();
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

    private void CloseRemoteView()
    {
        ConfirmationDialogOptions options = new ConfirmationDialogOptions(
            "Game_LeaveConfirmationTitle", "RemoteMatch_CloseView",
            "RemoteMatch_CloseView", "Game_ContinueMatchButton");
        if (_dialogService.Confirm(this, options))
        {
            NavigationService?.Navigate(_remoteGuestName == null
                ? new MainMenuPage() : new MainMenuPage(_remoteGuestName));
        }
    }
}
