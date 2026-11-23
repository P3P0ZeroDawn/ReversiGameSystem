using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

public partial class MatchCreationPage : Page
{
    private readonly string? _guestName;
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);

    public MatchCreationPage() : this(null)
    {
    }

    public MatchCreationPage(string? guestName)
    {
        InitializeComponent();
        _guestName = guestName;
        turnTimeTextBox.Text = TurnTimePolicy.DefaultSeconds.ToString();
        turnTimeTextBox.Focus();
        turnTimeTextBox.SelectAll();
    }

    private void OnTurnTimePreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text.Any(character => character < '0' || character > '9');
    }

    private async void OnCreateMatchClick(object sender, RoutedEventArgs e)
    {
        if (!TurnTimePolicy.TryParse(turnTimeTextBox.Text, out int turnTimeSeconds))
        {
            _dialogService.ShowWarning(
                this,
                "MatchCreation_InvalidTurnTimeTitle",
                "MatchCreation_InvalidTurnTimeMessage");
            turnTimeTextBox.Focus();
            turnTimeTextBox.SelectAll();
            return;
        }

        IsEnabled = false;
        MatchLobbyClient client = ClientRuntime.GetMatchLobbyClient(_guestName != null);
        MatchStatus status = await client.CreateMatch(turnTimeSeconds);
        if (status == MatchStatus.Success)
        {
            LobbySnapshot lobby = client.ReadCurrentLobby();
            NavigationService?.Navigate(new WaitingForOpponentPage(client, lobby, _guestName));
        }
        else
        {
            _dialogService.ShowError(this, "MatchCreation_PersistenceErrorTitle",
                RemoteMatchMessages.Key(status, _guestName != null));
        }
        IsEnabled = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        NavigateToMenu();
    }

    private async void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LanguageChangeService.ConfirmSelection((ComboBox)sender);
    }

    private void NavigateToMenu()
    {
        NavigationService?.Navigate(_guestName == null
            ? new MainMenuPage()
            : new MainMenuPage(_guestName));
    }
}
