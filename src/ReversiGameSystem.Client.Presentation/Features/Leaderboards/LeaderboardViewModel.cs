using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Features.Leaderboards;

internal sealed class LeaderboardViewModel : INotifyPropertyChanged
{
    private readonly LeaderboardClient _client;
    private LeaderboardEntry[] _entries = Array.Empty<LeaderboardEntry>();
    private int _generation;
    private bool _active;

    internal LeaderboardViewModel(LeaderboardClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public LeaderboardItem[] Rows { get; private set; } = Array.Empty<LeaderboardItem>();
    public bool IsBusy { get; private set; }
    public bool IsLoaded { get; private set; }

    internal void Activate()
    {
        _active = true;
    }

    internal void Deactivate()
    {
        _active = false;
        _generation++;
        IsBusy = false;
        NotifyChanged();
    }

    internal async Task<LeaderboardStatus> LoadLeaderboard(int maximumEntries)
    {
        if (!_active)
        {
            return LeaderboardStatus.Inactive;
        }
        if (IsBusy)
        {
            return LeaderboardStatus.Busy;
        }
        int generation = _generation;
        IsBusy = true;
        NotifyChanged();
        try
        {
            LeaderboardStatus status = await _client.LoadLeaderboard(maximumEntries);
            if (!_active || generation != _generation)
            {
                return LeaderboardStatus.Inactive;
            }
            if (status == LeaderboardStatus.Success)
            {
                _entries = _client.ReadLeaderboard();
                IsLoaded = true;
                RefreshLanguage();
            }
            return status;
        }
        finally
        {
            if (generation == _generation)
            {
                IsBusy = false;
                NotifyChanged();
            }
        }
    }

    internal void RefreshLanguage()
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(
            LocalizationManager.Instance.CultureName);
        LeaderboardItem[] rows = new LeaderboardItem[_entries.Length];
        for (int index = 0; index < _entries.Length; index++)
        {
            LeaderboardEntry entry = _entries[index];
            rows[index] = new LeaderboardItem(
                "#" + entry.Position.ToString("N0", culture), entry.Username,
                string.Format(culture, LocalizationManager.Instance["MainMenu_TopWinnersWinsFormat"],
                    entry.Wins));
        }
        Rows = rows;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}

internal sealed class LeaderboardItem
{
    internal LeaderboardItem(string position, string username, string winsText)
    {
        Position = position;
        Username = username;
        WinsText = winsText;
    }

    public string Position { get; }
    public string Username { get; }
    public string WinsText { get; }
}
