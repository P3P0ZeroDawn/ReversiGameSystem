using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using ReversiGameSystem.Features.Matches;
using ReversiGameSystem.Localization;

namespace ReversiGameSystem.Views;

internal partial class MovementHistoryDialogWindow : Window
{
    internal MovementHistoryDialogWindow(
        LocalizationManager localization,
        IReadOnlyList<MovementHistoryItem> movements)
    {
        InitializeComponent();
        Title = localization["MovementHistory_Title"];
        dialogTitleTextBlock.Text = Title;
        dialogMessageTextBlock.Text = localization["MovementHistory_Description"];
        emptyMovementsTextBlock.Text = localization["MovementHistory_EmptyMessage"];
        closeButton.Content = localization["MovementHistory_CloseButton"];

        movementsItemsControl.ItemsSource = movements;
        emptyMovementsTextBlock.Visibility = movements.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        movementsScrollViewer.Visibility = movements.Count == 0
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void OnTitleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

}
