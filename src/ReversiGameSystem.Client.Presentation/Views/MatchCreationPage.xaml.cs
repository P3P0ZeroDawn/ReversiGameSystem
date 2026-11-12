using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Collects the turn duration before creating a match.
/// </summary>
public partial class MatchCreationPage : Page
{
    private readonly string? _guestName;
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);

    /// <summary>
    /// Initializes match creation for an authenticated player.
    /// </summary>
    public MatchCreationPage() : this(null)
    {
    }

    /// <summary>
    /// Initializes match creation for the current player identity.
    /// </summary>
    /// <param name="guestName">The temporary name when the player is a guest.</param>
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
        RemoteMatchClient client = ClientRuntime.GetMatchClient(_guestName != null);
        MatchResult<LobbySnapshot> result = await client.CreateMatch(turnTimeSeconds);
        if (result.Status == MatchStatus.Success)
        {
            LobbySnapshot lobby = result.Value
                ?? throw new InvalidOperationException(
                    "A successful match result must contain its value.");
            NavigationService?.Navigate(new WaitingForOpponentPage(client, lobby, _guestName));
        }
        else
        {
            _dialogService.ShowError(this, "MatchCreation_PersistenceErrorTitle",
                RemoteMatchMessages.Key(result.Status, _guestName != null));
        }
        IsEnabled = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        NavigateToMenu();
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
    }

    private void NavigateToMenu()
    {
        NavigationService?.Navigate(_guestName == null
            ? new MainMenuPage()
            : new MainMenuPage(_guestName));
    }
}
