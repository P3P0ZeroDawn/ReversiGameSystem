using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReversiGameSystem.Features.Gameplay;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using MatchInformation = ReversiGameSystem.Matches.MatchInformation;
using MatchMove = ReversiGameSystem.Matches.MatchMove;

namespace ReversiGameSystem.Features.Matches;

internal sealed class MatchInformationViewModel : INotifyPropertyChanged
{
    private readonly GameplayClient _gameplay;
    private readonly ClientSession _session;
    private readonly string? _sessionToken;
    private readonly long _matchId;
    private readonly SynchronizationContext _context;
    private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
    private readonly CancellationToken _cancellationToken;
    private bool _closed;
    private MatchInformation? _information;
    private MatchStateSnapshot? _state;
    private bool _recovering;
    private bool _active;
    private bool _recoveryFailed;
    private string _connectionMessageResourceKey = string.Empty;
    private MatchMove[] _moves = Array.Empty<MatchMove>();

    internal MatchInformationViewModel(GameplayClient gameplay, ClientSession session, long matchId)
    {
        _gameplay = gameplay;
        _session = session;
        _cancellationToken = _lifetime.Token;
        _sessionToken = session.SessionToken;
        _matchId = matchId;
        _context = SynchronizationContext.Current ?? new SynchronizationContext();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public MatchInformation Information
    {
        get
        {
            return _information ?? throw new InvalidOperationException(
                "Match information has not been loaded.");
        }
        private set { _information = value; }
    }
    public MatchStateSnapshot State
    {
        get
        {
            return _state ?? throw new InvalidOperationException(
                "Match state has not been loaded.");
        }
        private set { _state = value; }
    }
    public bool IsReady { get; private set; }
    public bool NeedsRecovery { get; private set; }
    public string ConnectionMessage
    {
        get
        {
            return string.IsNullOrEmpty(_connectionMessageResourceKey) ? string.Empty
                : LocalizationManager.Instance[_connectionMessageResourceKey];
        }
    }
    internal string[] ReadMoveDescriptions()
    {
        return _moves.Select(move => string.Format(
            LocalizationManager.Instance["MatchInformation_MoveFormat"], move.MoveNumber,
            GetPlayerName(move.SeatNumber),
            string.Concat((char)('A' + move.Column), move.Row + 1))).ToArray();
    }

    internal async Task<MatchStatus> Activate()
    {
        if (_session.IsGuest || !_session.AccountId.HasValue || !IsCurrentSession())
        {
            return MatchStatus.Unauthorized;
        }
        _active = true;
        _gameplay.StateChanged += OnStateChanged;
        _gameplay.ConnectionLost += OnConnectionLost;
        MatchStatus status = await _gameplay.ObserveMatch(_matchId, _cancellationToken);
        if (!IsCurrentSession())
        {
            return MatchStatus.InvalidSession;
        }
        if (status == MatchStatus.Success)
        {
            Information = _gameplay.ReadMatchInformation(_matchId);
            State = _gameplay.ReadMatchState(_matchId);
            status = await LoadMovements();
            if (IsCurrentSession() && status == MatchStatus.Success)
            {
                State = _gameplay.ReadMatchState(_matchId);
                IsReady = true;
                NotifyChanged();
            }
        }
        return status;
    }

    internal async Task<MatchStatus> LoadMovements()
    {
        MatchStatus status = await _gameplay.LoadMoveHistory(_matchId, _cancellationToken);
        if (!IsCurrentSession())
        {
            return MatchStatus.InvalidSession;
        }
        if (status == MatchStatus.Success)
        {
            _moves = _gameplay.ReadMoveHistory(_matchId);
            NotifyChanged();
        }
        return status;
    }

    internal string GetPlayerName(int seatNumber)
    {
        MatchParticipant? player = Information.Participants
            .FirstOrDefault(participant => participant.SeatNumber == seatNumber);
        if (player == null)
        {
            return string.Empty;
        }
        return player.IsSystem ? LocalizationManager.Instance["Game_SystemPlayerName"]
            : player.DisplayName;
    }

    internal ReversiBoard ReadBoard()
    {
        string[] rows = new string[ReversiBoard.Size];
        for (int row = 0; row < rows.Length; row++)
        {
            char[] cells = new char[ReversiBoard.Size];
            for (int column = 0; column < cells.Length; column++)
            {
                cells[column] = State.Board[row * ReversiBoard.Size + column] switch
                {
                    ConfirmedBoardCell.Black => 'B',
                    ConfirmedBoardCell.White => 'W',
                    _ => '.'
                };
            }
            rows[row] = new string(cells);
        }
        return ReversiBoard.FromRows(rows);
    }

    internal bool IsCurrentSession()
    {
        return !_cancellationToken.IsCancellationRequested && _session.SessionToken == _sessionToken;
    }

    internal void Deactivate()
    {
        if (_closed)
        {
            return;
        }
        _closed = true;
        _active = false;
        _lifetime.Cancel();
        _gameplay.StateChanged -= OnStateChanged;
        _gameplay.ConnectionLost -= OnConnectionLost;
        _gameplay.StopObservingMatch(_matchId);
        _lifetime.Dispose();
    }

    private void OnStateChanged(MatchStateSnapshot state)
    {
        _context.Post(_ => ApplyState(state), null);
    }

    private void ApplyState(MatchStateSnapshot state)
    {
        if (!_active || !IsReady || !IsCurrentSession() || state.MatchId != _matchId
            || state.Version <= State.Version)
        {
            return;
        }
        State = state;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
    }

    internal async Task CheckConnection()
    {
        if (!_active || !IsReady || !IsCurrentSession() || _recovering
            || _recoveryFailed || State.State == ConfirmedMatchState.Finished)
        {
            return;
        }
        using CancellationTokenSource check = CancellationTokenSource
            .CreateLinkedTokenSource(_cancellationToken);
        check.CancelAfter(MatchConnectionPolicy.ConnectionCheckTimeout);
        try
        {
            MatchStatus status = await _gameplay.LoadMatchState(_matchId, check.Token);
            if (IsCurrentSession() && status == MatchStatus.ServiceUnavailable)
            {
                await RecoverConnection();
            }
            else if (IsCurrentSession() && status == MatchStatus.Success)
            {
                ApplyState(_gameplay.ReadMatchState(_matchId));
            }
            else if (IsCurrentSession())
            {
                _recoveryFailed = true;
                _connectionMessageResourceKey =
                    RemoteMatchMessages.GetMessageResourceKey(status, false);
                NotifyChanged();
            }
        }
        catch (OperationCanceledException) when (check.IsCancellationRequested)
        {
            await RecoverConnection();
        }
    }

    private void OnConnectionLost()
    {
        _context.Post(_ => RequestRecovery(), null);
    }

    private void RequestRecovery()
    {
        if (_active && IsReady && IsCurrentSession() && !_recovering && !_recoveryFailed)
        {
            NeedsRecovery = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NeedsRecovery)));
        }
    }

    internal async Task RecoverConnection()
    {
        if (!_active || !IsReady || !IsCurrentSession() || _recovering
            || _recoveryFailed || State.State == ConfirmedMatchState.Finished)
        {
            return;
        }
        _recovering = true;
        NeedsRecovery = false;
        using CancellationTokenSource recovery = CancellationTokenSource
            .CreateLinkedTokenSource(_cancellationToken);
        recovery.CancelAfter(MatchConnectionPolicy.ReconnectionWindow);
        _connectionMessageResourceKey = "Game_Reconnecting";
        NotifyChanged();
        try
        {
            MatchStatus status = MatchStatus.ServiceUnavailable;
            while (status == MatchStatus.ServiceUnavailable)
            {
                using CancellationTokenSource attempt = CancellationTokenSource
                    .CreateLinkedTokenSource(recovery.Token);
                attempt.CancelAfter(MatchConnectionPolicy.ConnectionCheckTimeout);
                try
                {
                    status = await _gameplay.RestoreMatchConnection(_matchId, attempt.Token);
                }
                catch (OperationCanceledException) when (!recovery.IsCancellationRequested)
                {
                    status = MatchStatus.ServiceUnavailable;
                }
                if (!IsCurrentSession())
                {
                    return;
                }
                if (status == MatchStatus.ServiceUnavailable)
                {
                    await Task.Delay(MatchConnectionPolicy.RecoveryRetryDelay, recovery.Token);
                }
            }
            _recoveryFailed = status != MatchStatus.Success;
            _connectionMessageResourceKey = status == MatchStatus.Success ? string.Empty
                : RemoteMatchMessages.GetMessageResourceKey(status, false);
            if (status == MatchStatus.Success)
            {
                State = _gameplay.ReadMatchState(_matchId);
                MatchStatus movementStatus = await LoadMovements();
                if (IsCurrentSession() && movementStatus != MatchStatus.Success)
                {
                    _connectionMessageResourceKey =
                        RemoteMatchMessages.GetMessageResourceKey(movementStatus, false);
                }
            }
        }
        catch (OperationCanceledException) when (recovery.IsCancellationRequested)
        {
            _recoveryFailed = true;
            _connectionMessageResourceKey = "Game_ReconnectionFailed";
        }
        finally
        {
            _recovering = false;
            if (IsCurrentSession())
            {
                NotifyChanged();
            }
        }
    }

    private void NotifyChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
