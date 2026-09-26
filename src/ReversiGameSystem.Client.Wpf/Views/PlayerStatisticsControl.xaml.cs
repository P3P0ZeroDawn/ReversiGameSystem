using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;

namespace ReversiGameSystem.Views;

/// <summary>
/// Shows statistics within a player's profile.
/// </summary>
public partial class PlayerStatisticsControl : UserControl
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private readonly IPlayerStatisticsService _statisticsService = new PlayerStatisticsService();
    private PlayerStatistics? _statistics;
    private string? _playerId;

    /// <summary>
    /// Initializes the statistics section.
    /// </summary>
    public PlayerStatisticsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>
    /// Gets or sets the selected player's identifier; null selects the signed-in player.
    /// </summary>
    public string? PlayerId
    {
        get { return _playerId; }
        set
        {
            if (_playerId == value)
            {
                return;
            }

            _playerId = value;
            ClearStatistics();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        LocalizationManager.Instance.PropertyChanged -= OnLocalizationChanged;
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationManager.CultureName))
        {
            ShowStatistics();
        }
    }

    private void OnShowStatisticsClick(object sender, RoutedEventArgs e)
    {
        ClearStatistics();
        PlayerStatisticsQueryResult result = _statisticsService.GetCompletedMatches(PlayerId);
        if (result.Status == PlayerStatisticsQueryStatus.ServerConnectionFailed)
        {
            _dialogService.ShowError(
                this,
                "PlayerStatistics_ServerConnectionFailedTitle",
                "PlayerStatistics_ServerConnectionFailedMessage");
            return;
        }

        if (result.Status == PlayerStatisticsQueryStatus.RetrievalError)
        {
            _dialogService.ShowError(
                this,
                "PlayerStatistics_RetrievalErrorTitle",
                "PlayerStatistics_RetrievalErrorMessage");
            return;
        }

        _statistics = PlayerStatistics.FromCompletedMatches(result.CompletedMatches);
        ShowStatistics();
    }

    private void ClearStatistics()
    {
        _statistics = null;
        string unavailable = LocalizationManager.Instance["AccountInformation_UnavailableValue"];
        winsValueTextBlock.Text = unavailable;
        matchesPlayedValueTextBlock.Text = unavailable;
        lossesValueTextBlock.Text = unavailable;
        drawsValueTextBlock.Text = unavailable;
        winRateValueTextBlock.Text = unavailable;
    }

    private void ShowStatistics()
    {
        if (_statistics == null)
        {
            ClearStatistics();
            return;
        }

        CultureInfo culture = CultureInfo.GetCultureInfo(LocalizationManager.Instance.CultureName);
        winsValueTextBlock.Text = _statistics.Wins.ToString("N0", culture);
        matchesPlayedValueTextBlock.Text = _statistics.MatchesPlayed.ToString("N0", culture);
        lossesValueTextBlock.Text = _statistics.Losses.ToString("N0", culture);
        drawsValueTextBlock.Text = _statistics.Draws.ToString("N0", culture);
        winRateValueTextBlock.Text = _statistics.WinRate.ToString("P1", culture);
    }
}
