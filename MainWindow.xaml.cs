using System.Windows.Navigation;

namespace ReversiGameSystem;

/// <summary>
/// Hosts application pages and prevents backward or forward navigation.
/// </summary>
public partial class MainWindow : NavigationWindow
{
    /// <summary>
    /// Initializes the main navigation window.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnNavigating(object sender, NavigatingCancelEventArgs e)
    {
        if (e.NavigationMode == NavigationMode.Back
            || e.NavigationMode == NavigationMode.Forward)
        {
            e.Cancel = true;
        }
    }
}
