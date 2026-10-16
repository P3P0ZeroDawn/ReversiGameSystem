using System.Windows;
using ReversiGameSystem.Services;

namespace ReversiGameSystem;

/// <summary>
/// Starts the WPF application.
/// </summary>
public partial class App : Application
{
    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        ClientRuntime.Dispose();
        base.OnExit(e);
    }
}
