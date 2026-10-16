using System.Windows;
using System.Windows.Input;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Presents two explicit actions and a cancel option.
/// </summary>
internal partial class ActionChoiceDialogWindow : Window
{
    internal ActionChoiceDialogWindow(
        ActionChoiceDialogOptions options,
        LocalizationManager localization)
    {
        InitializeComponent();

        Title = localization[options.TitleKey];
        dialogTitleTextBlock.Text = Title;
        dialogMessageTextBlock.Text = string.Format(
            localization[options.MessageKey],
            options.MessageArguments);
        primaryButton.Content = localization[options.PrimaryButtonKey];
        alternateButton.Content = localization[options.AlternateButtonKey];
        cancelButton.Content = localization[options.CancelButtonKey];
    }

    internal ActionChoiceDialogResult SelectedAction { get; private set; } =
        ActionChoiceDialogResult.Cancel;

    private void OnTitleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnPrimaryClick(object sender, RoutedEventArgs e)
    {
        SelectedAction = ActionChoiceDialogResult.Primary;
        DialogResult = true;
    }

    private void OnAlternateClick(object sender, RoutedEventArgs e)
    {
        SelectedAction = ActionChoiceDialogResult.Alternate;
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
