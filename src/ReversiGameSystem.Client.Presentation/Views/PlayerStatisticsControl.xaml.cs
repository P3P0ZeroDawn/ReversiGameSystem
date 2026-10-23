using System;
using System.ComponentModel;
using System.Globalization;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Views;

/// <summary>
/// Shows statistics within a player's profile.
/// </summary>
public partial class PlayerStatisticsControl : UserControl
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private Proxy.PlayerStatistics? _statistics;
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

    internal void SetStatistics(Proxy.PlayerStatistics statistics)
    {
        _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        ShowStatistics();
    }

    private async void OnShowStatisticsClick(object sender, RoutedEventArgs e)
    {
        if (PlayerId != null)
        {
            _dialogService.ShowError(
                this,
                "PlayerStatistics_RetrievalErrorTitle",
                "PlayerStatistics_RetrievalErrorMessage");
            return;
        }

        string? sessionToken = AccountPreviewState.SessionToken;
        if (sessionToken == null || string.IsNullOrWhiteSpace(sessionToken))
        {
            _dialogService.ShowError(
                this,
                "PlayerStatistics_RetrievalErrorTitle",
                "PlayerStatistics_RetrievalErrorMessage");
            return;
        }

        ClearStatistics();
        try
        {
            IsEnabled = false;
            Proxy.PlayerProfile profile = await ClientRuntime.ProfileService.GetPlayerProfileAsync(sessionToken);
            SetStatistics(profile.Statistics);
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            string resourcePrefix = exception is not FaultException && (exception is CommunicationException or TimeoutException)
                ? "PlayerStatistics_ServerConnectionFailed" : "PlayerStatistics_RetrievalError";
            _dialogService.ShowError(this, resourcePrefix + "Title", resourcePrefix + "Message");
        }
        finally
        {
            IsEnabled = true;
        }
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
