using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Features.Matches;

internal sealed class MatchHistoryViewModel : INotifyPropertyChanged
{
    private readonly GameplayClient? _gameplay;
    private readonly ClientSession _session;
    private readonly string? _sessionToken;
    private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
    private readonly CancellationToken _cancellation;
    private MatchRecord[] _history = Array.Empty<MatchRecord>();
    private bool _closed;

    internal MatchHistoryViewModel(ClientSession session, GameplayClient? gameplay)
    {
        _session = session;
        _sessionToken = session.SessionToken;
        _gameplay = gameplay;
        _cancellation = _lifetime.Token;
        _session.SessionChanged += OnSessionChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public MatchHistoryItem[] Matches { get; private set; } = Array.Empty<MatchHistoryItem>();
    public bool IsBusy { get; private set; }

    internal async Task<MatchStatus> LoadHistory()
    {
        if (_session.IsGuest || !_session.AccountId.HasValue || _gameplay == null)
        {
            return MatchStatus.Unauthorized;
        }
        if (!IsCurrentSession())
        {
            return MatchStatus.InvalidSession;
        }
        IsBusy = true;
        NotifyChanged();
        try
        {
            MatchStatus status = await _gameplay.LoadMatchHistory(_cancellation);
            if (!IsCurrentSession())
            {
                return MatchStatus.InvalidSession;
            }
            _history = status == MatchStatus.Success
                ? _gameplay.ReadMatchHistory() : Array.Empty<MatchRecord>();
            RefreshLanguage();
            return status;
        }
        finally
        {
            IsBusy = false;
            NotifyChanged();
        }
    }

    internal async Task<MatchStatus> LoadRecording(long matchId)
    {
        if (!IsCurrentSession() || _gameplay == null)
        {
            return MatchStatus.InvalidSession;
        }
        if (!_history.Any(match => match.MatchId == matchId))
        {
            return MatchStatus.Unauthorized;
        }
        IsBusy = true;
        NotifyChanged();
        try
        {
            MatchStatus status = await _gameplay.LoadMatchDetails(matchId, _cancellation);
            return IsCurrentSession() ? status : MatchStatus.InvalidSession;
        }
        finally
        {
            IsBusy = false;
            NotifyChanged();
        }
    }

    internal MatchRecording ReadRecording(long matchId)
    {
        if (!IsCurrentSession() || _gameplay == null)
        {
            throw new InvalidOperationException("The history session is no longer active.");
        }
        return _gameplay.ReadMatchDetails(matchId);
    }

    internal void RefreshLanguage()
    {
        Matches = _history.Select(match => new MatchHistoryItem(match)).ToArray();
        NotifyChanged();
    }

    internal bool IsCurrentSession()
    {
        return !_cancellation.IsCancellationRequested && _session.SessionToken == _sessionToken;
    }

    private void OnSessionChanged()
    {
        Deactivate();
    }

    internal void Deactivate()
    {
        if (_closed)
        {
            return;
        }
        _closed = true;
        _session.SessionChanged -= OnSessionChanged;
        _lifetime.Cancel();
        _lifetime.Dispose();
        _history = Array.Empty<MatchRecord>();
        Matches = Array.Empty<MatchHistoryItem>();
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
