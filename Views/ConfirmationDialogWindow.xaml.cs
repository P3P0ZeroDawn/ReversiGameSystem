using System.Windows;
using System.Windows.Input;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Asks the player to choose between a localized confirm and cancel action.
/// </summary>
internal partial class ConfirmationDialogWindow : Window
{
    internal ConfirmationDialogWindow(
        ConfirmationDialogOptions options,
        LocalizationManager localization)
    {
        InitializeComponent();

        Title = localization[options.TitleKey];
        dialogTitleTextBlock.Text = Title;
        dialogMessageTextBlock.Text = string.Format(
            localization[options.MessageKey],
            options.MessageArguments);
        confirmButton.Content = localization[options.ConfirmButtonKey];
        cancelButton.Content = localization[options.CancelButtonKey];
    }

    private void OnTitleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
