using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays matches that can be joined.
/// </summary>
public partial class AvailableMatchesPage : Page
{
    private readonly string? _guestName;
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);

    /// <summary>
    /// Initializes the available matches page.
    /// </summary>
    public AvailableMatchesPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the available matches page for a guest.
    /// </summary>
    /// <param name="guestName">The name shown while the guest is in the application.</param>
    public AvailableMatchesPage(string guestName) : this()
    {
        _guestName = guestName;
        guestNameTextBlock.Text = guestName;
        guestNameTextBlock.ToolTip = guestName;
        guestIdentityPanel.Visibility = Visibility.Visible;
    }

    private void OnBackToMenuClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(_guestName == null
            ? new MainMenuPage()
            : new MainMenuPage(_guestName));
    }

    private void OnJoinWithCodeClick(object sender, RoutedEventArgs e)
    {
        TextInputDialogOptions options = new TextInputDialogOptions(
            "AvailableMatches_CodeDialogTitle",
            "AvailableMatches_CodeDialogMessage",
            "AvailableMatches_CodeDialogLabel")
        {
            ConfirmButtonKey = "AvailableMatches_JoinButton",
            RequireText = true,
            MaximumLength = 64
        };

        // TODO: Look up the entered code on the server and join the matching game.
        _dialogService.TryGetText(this, options, out _);
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
    }
}
