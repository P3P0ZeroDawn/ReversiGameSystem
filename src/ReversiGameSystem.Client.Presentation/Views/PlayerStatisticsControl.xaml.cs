using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

public partial class PlayerStatisticsControl : UserControl
{
    private ProfileStatistics? _statistics;

    public PlayerStatisticsControl()
    {
        InitializeComponent();
        ClearStatistics();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
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

    internal void SetStatistics(ProfileStatistics statistics)
    {
        _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        showStatisticsButton.IsEnabled = true;
        ShowStatistics();
    }

    private void OnShowStatisticsClick(object sender, RoutedEventArgs e)
    {
        ShowStatistics();
    }

    internal void ClearStatistics()
    {
        _statistics = null;
        showStatisticsButton.IsEnabled = false;
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
