using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Features.Matches;

internal sealed class AvailableMatchesViewModel : INotifyPropertyChanged
{
    private readonly GameplayClient? _gameplay;
    private readonly ClientSession _session;
    private readonly string? _sessionToken;
    private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
    private readonly CancellationToken _cancellationToken;
    private bool _closed;

    internal AvailableMatchesViewModel(ClientSession session, GameplayClient? gameplay)
    {
        _session = session;
        _cancellationToken = _lifetime.Token;
        _sessionToken = session.SessionToken;
        _gameplay = gameplay;
        CanObserve = !session.IsGuest && session.AccountId.HasValue && gameplay != null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public bool CanObserve { get; }
    public MatchInformation[] ObservableMatches { get; private set; } =
        Array.Empty<MatchInformation>();

    internal async Task<MatchStatus> LoadObservableMatches()
    {
        if (!CanObserve || !IsCurrentSession())
        {
            return MatchStatus.Unauthorized;
        }
        GameplayClient gameplay = _gameplay
            ?? throw new InvalidOperationException(
                "An authenticated observer requires a gameplay client.");
        ObservableMatches = Array.Empty<MatchInformation>();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ObservableMatches)));
        MatchStatus status = await gameplay.LoadObservableMatches(_cancellationToken);
        if (!IsCurrentSession())
        {
            return MatchStatus.InvalidSession;
        }
        ObservableMatches = status == MatchStatus.Success
            ? gameplay.ReadObservableMatches() : Array.Empty<MatchInformation>();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ObservableMatches)));
        return status;
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
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
