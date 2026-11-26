using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReversiGameSystem.Chat;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using MatchInformation = ReversiGameSystem.Matches.MatchInformation;
using ChatMessage = ReversiGameSystem.Chat.ChatMessage;

namespace ReversiGameSystem.Features.Gameplay;

internal sealed class GameViewModel : INotifyPropertyChanged
{
    internal const int CreatorSeat = 1;
    internal const int OpponentSeat = 2;
    private const string ReconnectingMessageKey = "Game_Reconnecting";
    private const string ReconnectionFailedMessageKey = "Game_ReconnectionFailed";
    private const string PlayerMutedMessageKey = "Chat_PlayerMutedMessage";
    private const string AbandonmentReasonKey = "MatchHistory_AbandonmentReason";
    private const string UnavailablePlayerNameKey = "AccountInformation_UnavailableValue";
    private const string SystemPlayerNameKey = "Game_SystemPlayerName";
    private const string GeneralDateTimeFormat = "g";
    private static readonly TimeSpan ConnectionCheckTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan RecoveryRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ReconnectionWindow = TimeSpan.FromSeconds(30);
    private readonly GameplayClient _gameplay;
    private readonly MatchChatClient _chat;
    private readonly SynchronizationContext _presentationContext;
    private readonly long _matchId;
    private readonly bool _isGuest;
    private CancellationTokenSource? _recovery;
    private CancellationTokenSource? _connectionCheck;
    private Task _recoveryTask = Task.CompletedTask;
    private bool _active;
    private bool _loading;
    private bool _placing;
    private bool _sending;
    private bool _leaving;
    private bool _chatReady;
    private bool _recoveryFailed;
    private bool _muted;
    private string _statusMessageResourceKey = string.Empty;

    internal GameViewModel(GameplayClient gameplay, MatchChatClient chat, long matchId,
        bool isGuest, SynchronizationContext presentationContext)
    {
        _gameplay = gameplay ?? throw new ArgumentNullException(nameof(gameplay));
        _chat = chat ?? throw new ArgumentNullException(nameof(chat));
        _matchId = matchId;
        _isGuest = isGuest;
        _presentationContext = presentationContext
            ?? throw new ArgumentNullException(nameof(presentationContext));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    internal event Action<Exception>? UnexpectedRecoveryFailure;
    public TurnCountdownViewModel Countdown { get; } = new TurnCountdownViewModel();
    public string MessageText { get; set; } = string.Empty;
    public bool IsReady { get; private set; }
    public bool IsRecovering
    {
        get
        {
            return _recovery != null;
        }
    }
    public bool RecoveryFailed
    {
        get
        {
            return _recoveryFailed;
        }
    }
    public bool IsLeaving
    {
        get
        {
            return _leaving;
        }
    }
    public bool IsMuted
    {
        get
        {
            return _muted;
        }
    }
    public DateTime? MuteEndsAtUtc
    {
        get
        {
            return _chat.MuteEndsAtUtc;
        }
    }
    public MatchInformation Information
    {
        get
        {
            return _gameplay.ReadMatchInformation(_matchId);
        }
    }
    public MatchStateSnapshot State
    {
        get
        {
            return _gameplay.ReadMatchState(_matchId);
        }
    }
    public bool IsFinished
    {
        get
        {
            return _gameplay.HasMatchState(_matchId) && State.State == ConfirmedMatchState.Finished;
        }
    }
    public bool IsParticipant
    {
        get
        {
            return IsReady && !_isGuest && Information.AssignedSeatNumber.HasValue;
        }
    }
    public bool CanForfeit
    {
        get
        {
            return IsParticipant && !IsFinished && State.State == ConfirmedMatchState.InProgress;
        }
    }
    public DiscColor SystemPlayerColor
    {
        get
        {
            DiscColor color = DiscColor.Empty;
            if (IsReady)
            {
                foreach (MatchParticipant participant in Information.Participants)
                {
                    if (participant.IsSystem)
                    {
                        color = participant.SeatNumber == CreatorSeat
                            ? DiscColor.Black : DiscColor.White;
                        break;
                    }
                }
            }
            return color;
        }
    }

    public bool IsChatVisible
    {
        get
        {
            return IsReady && !_isGuest && !Information.Participants.Any(player => player.IsSystem);
        }
    }
    public bool CanWriteMessage
    {
        get
        {
            return _active && IsChatVisible && _chatReady && !_muted
                && !IsRecovering && !_recoveryFailed && !IsFinished;
        }
    }
    public bool CanSendMessage
    {
        get
        {
            return CanWriteMessage && !_sending;
        }
    }
    public bool IsMessageReadOnly
    {
        get
        {
            return _sending;
        }
    }
    public bool CanLeave
    {
        get
        {
            return !_leaving;
        }
    }
    public bool CanPlacePiece
    {
        get
        {
            return _active && IsParticipant && !_placing && !IsRecovering && !_recoveryFailed
                && State.State == ConfirmedMatchState.InProgress
                && State.CurrentTurnSeatNumber == Information.AssignedSeatNumber;
        }
    }
    public string StatusText
    {
        get
        {
            string text = string.Empty;
            LocalizationManager localization = LocalizationManager.Instance;
            if (IsRecovering || _recoveryFailed)
            {
                string key = IsRecovering ? ReconnectingMessageKey : ReconnectionFailedMessageKey;
                text = localization[key];
            }
            else if (_muted && MuteEndsAtUtc.HasValue)
            {
                string endTime = MuteEndsAtUtc.Value.ToLocalTime().ToString(GeneralDateTimeFormat,
                    CultureInfo.GetCultureInfo(localization.CultureName));
                text = string.Format(localization[PlayerMutedMessageKey], endTime);
            }
            else if (_statusMessageResourceKey.Length > 0)
            {
                text = localization[_statusMessageResourceKey];
            }
            else if (IsFinished && State.Conclusion?.Reason == RemoteMatchEndReason.Abandonment)
            {
                text = localization[AbandonmentReasonKey];
            }
            return text;
        }
    }
    public ChatMessage[] MessageHistory
    {
        get
        {
            return _chat.ReadMessages(_matchId);
        }
    }

    internal async Task ActivateMatch()
    {
        if (_active)
        {
            return;
        }
        _active = true;
        _recoveryFailed = false;
        _statusMessageResourceKey = string.Empty;
        _gameplay.StateChanged += OnMatchStateChanged;
        _gameplay.ParticipantConnectionChanged += OnParticipantConnectionChanged;
        _gameplay.ConnectionLost += OnConnectionLost;
        _chat.MessagesChanged += OnMessagesChanged;
        _chat.MuteChanged += OnMuteChanged;
        LocalizationManager.Instance.PropertyChanged += OnCultureChanged;
        await LoadMatch();
    }

    internal async Task DeactivateMatch()
    {
        _active = false;
        _gameplay.StateChanged -= OnMatchStateChanged;
        _gameplay.ParticipantConnectionChanged -= OnParticipantConnectionChanged;
        _gameplay.ConnectionLost -= OnConnectionLost;
        _chat.MessagesChanged -= OnMessagesChanged;
        _chat.MuteChanged -= OnMuteChanged;
        LocalizationManager.Instance.PropertyChanged -= OnCultureChanged;
        _connectionCheck?.Cancel();
        _recovery?.Cancel();
        _chatReady = false;
        NotifyPropertyChanged();
        await _recoveryTask;
    }

    private async Task LoadMatch()
    {
        if (_loading || IsRecovering || _recoveryFailed)
        {
            return;
        }
        _loading = true;
        try
        {
            MatchStatus status = await _gameplay.LoadMatchInformation(
                _matchId, CancellationToken.None);
            if (status == MatchStatus.Success)
            {
                status = await _gameplay.LoadMatchState(_matchId, CancellationToken.None);
            }
            IsReady = status == MatchStatus.Success;
            if (IsReady && _active)
            {
                await LoadChat(CancellationToken.None);
            }
            else
            {
                _statusMessageResourceKey =
                    RemoteMatchMessages.GetMessageResourceKey(status, _isGuest);
            }
        }
        finally
        {
            _loading = false;
            UpdateClock(DateTime.UtcNow);
            NotifyPropertyChanged();
        }
    }

    private async Task LoadChat(CancellationToken cancellation = default)
    {
        if (!IsChatVisible)
        {
            return;
        }
        ChatStatus status = await _chat.OpenChatChannel(_matchId, cancellation);
        if (status == ChatStatus.Success)
        {
            status = await _chat.LoadChatHistory(_matchId, cancellation);
        }
        _chatReady = status == ChatStatus.Success;
        _statusMessageResourceKey =
                    RemoteMatchMessages.GetMessageResourceKey(status, _isGuest);
        NotifyPropertyChanged(nameof(MessageHistory));
    }

    internal void UpdateClock(DateTime nowUtc)
    {
        if (IsReady)
        {
            Countdown.Update(State, Information.TurnDurationSeconds, nowUtc);
        }
        bool muted = MuteEndsAtUtc > nowUtc;
        if (_muted != muted)
        {
            _muted = muted;
            NotifyPropertyChanged(nameof(IsMuted));
            NotifyPropertyChanged(nameof(CanWriteMessage));
            NotifyPropertyChanged(nameof(CanSendMessage));
            NotifyPropertyChanged(nameof(StatusText));
        }
    }

    internal async Task CheckMatchConnection()
    {
        if (!_active || !IsReady || _loading || _placing || _sending || _leaving
            || _connectionCheck != null || IsRecovering || _recoveryFailed || IsFinished)
        {
            return;
        }
        using CancellationTokenSource check = new CancellationTokenSource(ConnectionCheckTimeout);
        _connectionCheck = check;
        try
        {
            MatchStatus status = await _gameplay.LoadMatchState(_matchId, check.Token);
            if (status == MatchStatus.ServiceUnavailable)
            {
                StartMatchRecovery(null);
                await _recoveryTask;
            }
        }
        catch (OperationCanceledException) when (check.IsCancellationRequested)
        {
            StartMatchRecovery(null);
            await _recoveryTask;
        }
        finally
        {
            _connectionCheck = null;
        }
    }

    private void StartMatchRecovery(object? state)
    {
        if (_active && _recoveryTask.IsCompleted && IsReady && !IsFinished && !_recoveryFailed)
        {
            _recoveryTask = RestoreMatchConnection();
        }
    }

    private async Task RestoreMatchConnection()
    {
        try
        {
            await RecoverMatchConnection();
        }
        catch (Exception exception)
        {
            _active = false;
            _chatReady = false;
            _recoveryFailed = false;
            _statusMessageResourceKey = string.Empty;
            Action<Exception>? reportFailure = UnexpectedRecoveryFailure;
            if (reportFailure == null)
            {
                throw;
            }
            reportFailure(exception);
        }
    }

    private async Task RecoverMatchConnection()
    {
        using CancellationTokenSource recovery = new CancellationTokenSource(ReconnectionWindow);
        _recovery = recovery;
        _chatReady = false;
        bool restored = false;
        bool completed = false;
        try
        {
            NotifyPropertyChanged();
            MatchStatus status = MatchStatus.ServiceUnavailable;
            while (status is MatchStatus.ServiceUnavailable or MatchStatus.MatchNotInProgress)
            {
                using CancellationTokenSource attempt = CancellationTokenSource
                    .CreateLinkedTokenSource(recovery.Token);
                attempt.CancelAfter(ConnectionCheckTimeout);
                try
                {
                    status = await _gameplay.RestoreMatchConnection(_matchId, attempt.Token);
                }
                catch (OperationCanceledException) when (attempt.IsCancellationRequested
                    && !recovery.IsCancellationRequested)
                {
                    status = MatchStatus.ServiceUnavailable;
                }
                if (status is MatchStatus.ServiceUnavailable or MatchStatus.MatchNotInProgress)
                {
                    await Task.Delay(RecoveryRetryDelay, recovery.Token);
                }
            }
            restored = status == MatchStatus.Success;
            if (restored && _active && !IsFinished)
            {
                await LoadChat(recovery.Token);
            }
            completed = true;
        }
        catch (OperationCanceledException) when (recovery.IsCancellationRequested)
        {
            restored = false;
            completed = true;
        }
        finally
        {
            _recovery = null;
            _recoveryFailed = completed && !restored;
            if (completed)
            {
                UpdateClock(DateTime.UtcNow);
                NotifyPropertyChanged();
            }
        }
    }

    internal async Task<MatchStatus> PlacePiece(BoardPosition position)
    {
        if (!CanPlacePiece)
        {
            return MatchStatus.NotPlayersTurn;
        }
        _placing = true;
        NotifyPropertyChanged();
        MatchStatus status;
        try
        {
            MatchPlacement placement = new MatchPlacement
            {
                MatchId = _matchId,
                ExpectedVersion = State.Version,
                Row = position.Row,
                Column = position.Column
            };
            status = await _gameplay.PlacePiece(placement, CancellationToken.None);
            if (status is MatchStatus.StaleMatchStateVersion or MatchStatus.NotPlayersTurn)
            {
                await LoadMatch();
                _statusMessageResourceKey = string.Empty;
            }
            else
            {
                _statusMessageResourceKey =
                    RemoteMatchMessages.GetMessageResourceKey(status, _isGuest);
            }
        }
        finally
        {
            _placing = false;
            NotifyPropertyChanged();
        }
        return status;
    }

    internal async Task<ChatStatus> SendChatMessage()
    {
        UpdateClock(DateTime.UtcNow);
        ChatStatus status = ChatStatus.Unauthorized;
        if (_muted)
        {
            return ChatStatus.PlayerMuted;
        }
        if (!CanSendMessage)
        {
            return status;
        }
        string message = MessageText.Trim();
        if (message.Length == 0)
        {
            return ChatStatus.EmptyMessage;
        }
        if (message.Length > ChatStore.MaximumMessageLength)
        {
            return ChatStatus.MessageTooLong;
        }
        _sending = true;
        NotifyPropertyChanged();
        try
        {
            status = await _chat.SendChatMessage(_matchId, message, CancellationToken.None);
            _statusMessageResourceKey = status == ChatStatus.PlayerMuted
                ? string.Empty : RemoteMatchMessages.GetMessageResourceKey(status, _isGuest);
            if (status == ChatStatus.Success)
            {
                MessageText = string.Empty;
            }
            else if (status == ChatStatus.ServiceUnavailable)
            {
                _chatReady = false;
            }
        }
        finally
        {
            _sending = false;
            UpdateClock(DateTime.UtcNow);
            NotifyPropertyChanged();
        }
        return status;
    }

    internal async Task<MatchStatus> ForfeitMatch()
    {
        if (!CanForfeit)
        {
            return MatchStatus.MatchNotInProgress;
        }
        _leaving = true;
        NotifyPropertyChanged(nameof(CanLeave));
        MatchStatus status;
        try
        {
            status = await _gameplay.ForfeitMatch(_matchId, CancellationToken.None);
            if (status == MatchStatus.MatchNotInProgress)
            {
                await LoadMatch();
            }
            _statusMessageResourceKey =
                    RemoteMatchMessages.GetMessageResourceKey(status, _isGuest);
        }
        finally
        {
            _leaving = false;
            NotifyPropertyChanged(nameof(CanLeave));
            NotifyPropertyChanged(nameof(StatusText));
        }
        return status;
    }

    internal string GetPlayerDisplayName(int seatNumber)
    {
        string name = LocalizationManager.Instance[UnavailablePlayerNameKey];
        if (IsReady)
        {
            MatchParticipant? player = Information.Participants
                .FirstOrDefault(participant => participant.SeatNumber == seatNumber);
            if (player != null)
            {
                name = player.IsSystem
                    ? LocalizationManager.Instance[SystemPlayerNameKey]
                    : player.DisplayName;
            }
        }
        return name;
    }

    internal ParticipantConnectionState ReadParticipantConnection(int seatNumber)
    {
        return _gameplay.ReadParticipantConnection(_matchId, seatNumber);
    }

    private void OnMatchStateChanged(MatchStateSnapshot state)
    {
        if (state.MatchId == _matchId)
        {
            _presentationContext.Post(RefreshMatch, null);
        }
    }

    private void OnParticipantConnectionChanged(long matchId)
    {
        if (matchId == _matchId)
        {
            _presentationContext.Post(RefreshMatch, null);
        }
    }

    private void OnMessagesChanged(long matchId)
    {
        if (matchId == _matchId)
        {
            _presentationContext.Post(RefreshMessages, null);
        }
    }

    private void OnMuteChanged()
    {
        _presentationContext.Post(RefreshMatch, null);
    }

    private void OnConnectionLost()
    {
        _presentationContext.Post(StartMatchRecovery, null);
    }

    private void OnCultureChanged(object? sender, PropertyChangedEventArgs arguments)
    {
        if (arguments.PropertyName == nameof(LocalizationManager.CultureName))
        {
            RefreshMatch(null);
            RefreshMessages(null);
        }
    }

    private void RefreshMatch(object? state)
    {
        if (_active)
        {
            UpdateClock(DateTime.UtcNow);
            NotifyPropertyChanged();
        }
    }

    private void RefreshMessages(object? state)
    {
        if (_active)
        {
            NotifyPropertyChanged(nameof(MessageHistory));
        }
    }

    private void NotifyPropertyChanged(string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
