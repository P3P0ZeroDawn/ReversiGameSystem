using System;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.ServiceModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

public partial class PlayerStatisticsControl : UserControl
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private ProfileStatistics? _statistics;
    private long? _accountId;

    public PlayerStatisticsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }
    public long? AccountId
    {
        get { return _accountId; }
        set
        {
            if (_accountId == value)
            {
                return;
            }

            _accountId = value;
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

    internal void SetStatistics(ProfileStatistics statistics)
    {
        _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        ShowStatistics();
    }

    private async void OnShowStatisticsClick(object sender, RoutedEventArgs e)
    {
        string? sessionToken = ClientRuntime.CurrentSession.SessionToken;
        if (ClientRuntime.CurrentSession.IsGuest || string.IsNullOrWhiteSpace(sessionToken))
        {
            _dialogService.ShowError(this, "PlayerStatistics_RetrievalErrorTitle",
                "PlayerStatistics_RetrievalErrorMessage");
            return;
        }
        long? accountId = AccountId;
        IsEnabled = false;
        try
        {
            ProfileResult<PlayerProfile> result =
                await ClientRuntime.ProfileService.GetPlayerProfile(sessionToken!, accountId);
            if (!IsLoaded || ClientRuntime.CurrentSession.SessionToken != sessionToken
                || AccountId != accountId)
            {
                return;
            }
            if (result.Status == ProfileStatus.Success)
            {
                PlayerProfile profile = result.Value
                    ?? throw new InvalidOperationException(
                        "A successful profile must contain data.");
                SetStatistics(profile.Statistics);
            }
            else
            {
                string prefix = result.Status == ProfileStatus.ServiceUnavailable
                    ? "PlayerStatistics_ServerConnectionFailed" : "PlayerStatistics_RetrievalError";
                _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
            }
        }
        catch (FaultException<ServerProxy.ServiceFault> exception)
        {
            if (IsLoaded)
            {
                ClientRuntime.Errors.ReportOperationFailure(
                    exception, nameof(PlayerStatisticsControl));
            }
            else
            {
                ClientRuntime.Errors.RecordFailure(
                    exception, nameof(PlayerStatisticsControl), LogLevel.Error);
            }
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
