using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays a new match code while waiting for a second participant.
/// </summary>
public partial class WaitingForOpponentPage : Page
{
    private readonly MatchSession _session;
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);

    internal WaitingForOpponentPage(MatchSession session)
    {
        InitializeComponent();
        _session = session;
        matchCodeTextBlock.Text = session.Code;
        UpdateTurnTimeText();
    }

    private void OnModifySettingsClick(object sender, RoutedEventArgs e)
    {
        if (!_dialogService.TryModifyMatchSettings(this, _session, _session.CreatorName))
        {
            return;
        }

        UpdateTurnTimeText();
        _dialogService.ShowInformation(
            this,
            "MatchSettings_SuccessTitle",
            "MatchSettings_SuccessMessage");
    }

    private void OnPlayAgainstSystemClick(object sender, RoutedEventArgs e)
    {
        _session.StartAgainstSystem(LocalizationManager.Instance["Game_SystemPlayerName"]);
        NavigationService?.Navigate(new GamePage(_session));
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        _session.Cancel();
        NavigationService?.Navigate(_session.GuestName == null
            ? new MainMenuPage()
            : new MainMenuPage(_session.GuestName));
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string previousCulture = LocalizationManager.Instance.CultureName;
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
        if (LocalizationManager.Instance.CultureName != previousCulture)
        {
            UpdateTurnTimeText();
        }
    }

    private void UpdateTurnTimeText()
    {
        turnTimeTextBlock.Text = string.Format(
            LocalizationManager.Instance["WaitingForOpponent_TurnTimeFormat"],
            _session.TurnTimeSeconds);
    }
}
