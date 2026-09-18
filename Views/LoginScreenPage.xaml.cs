using System.Windows;
using System.Windows.Controls;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays the sign-in form.
/// </summary>
public partial class LoginScreenPage : Page
{
    /// <summary>
    /// Initializes the sign-in page.
    /// </summary>
    public LoginScreenPage()
    {
        InitializeComponent();
    }

    private void OnCreateAccountClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new AccountCreationPage());
    }
}
