using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays the main navigation and placeholders for player lists.
/// </summary>
public partial class MainMenuPage : Page
{
    private readonly string? _guestName;

    /// <summary>
    /// Initializes the main menu page.
    /// </summary>
    public MainMenuPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the menu for a guest with a temporary display name.
    /// </summary>
    /// <param name="guestName">The name shown while the guest is in the application.</param>
    public MainMenuPage(string guestName) : this()
    {
        _guestName = guestName;
        guestNameTextBlock.Text = guestName;
        guestNameTextBlock.ToolTip = guestName;
        guestIdentityPanel.Visibility = Visibility.Visible;
        friendsCardBorder.Visibility = Visibility.Collapsed;
        addFriendButton.IsEnabled = false;
    }

    private void OnJoinGameClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(_guestName == null
            ? new AvailableMatchesPage()
            : new AvailableMatchesPage(_guestName));
    }

    private void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new LoginScreenPage());
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
    }
}
