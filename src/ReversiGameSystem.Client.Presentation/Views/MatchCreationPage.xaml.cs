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
    private readonly MatchCreationService _matchCreationService = new MatchCreationService();

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

    private void OnCreateMatchClick(object sender, RoutedEventArgs e)
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

        string creatorName = _guestName
            ?? AccountPreviewState.Username
            ?? LocalizationManager.Instance["Game_DefaultPlayerName"];
        MatchCreationResult result = _matchCreationService.TryCreate(
            turnTimeSeconds,
            creatorName,
            _guestName,
            out MatchSession? session);

        if (result == MatchCreationResult.ServerConnectionFailed)
        {
            _dialogService.ShowError(
                this,
                "MatchCreation_ServerConnectionFailedTitle",
                "MatchCreation_ServerConnectionFailedMessage");
            return;
        }

        if (result == MatchCreationResult.PersistenceError || session == null)
        {
            _dialogService.ShowError(
                this,
                "MatchCreation_PersistenceErrorTitle",
                "MatchCreation_PersistenceErrorMessage");
            return;
        }

        NavigationService?.Navigate(new WaitingForOpponentPage(session));
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
