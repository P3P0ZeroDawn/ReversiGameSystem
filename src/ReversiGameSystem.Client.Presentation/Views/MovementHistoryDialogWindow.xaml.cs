using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays the detailed movement history for one accessible match.
/// </summary>
internal partial class MovementHistoryDialogWindow : Window
{
    internal MovementHistoryDialogWindow(
        LocalizationManager localization,
        IReadOnlyList<RecordedMove> movements)
    {
        InitializeComponent();
        Title = localization["MovementHistory_Title"];
        dialogTitleTextBlock.Text = Title;
        dialogMessageTextBlock.Text = localization["MovementHistory_Description"];
        emptyMovementsTextBlock.Text = localization["MovementHistory_EmptyMessage"];
        closeButton.Content = localization["MovementHistory_CloseButton"];

        CultureInfo culture = CultureInfo.GetCultureInfo(localization.CultureName);
        List<MovementHistoryItem> items = new List<MovementHistoryItem>();
        foreach (RecordedMove movement in movements)
        {
            items.Add(new MovementHistoryItem
            {
                SequenceText = string.Format(
                    localization["MovementHistory_SequenceFormat"],
                    movement.Number),
                DateText = string.Format(
                    localization["MovementHistory_DateFormat"],
                    movement.RecordedAtUtc.ToLocalTime().ToString("g", culture)),
                PlayerText = string.Format(
                    localization["MovementHistory_PlayerFormat"],
                    movement.PlayerName),
                PositionText = string.Format(
                    localization["MovementHistory_PositionFormat"],
                    movement.Coordinate),
                AffectedText = string.Format(
                    localization["MovementHistory_AffectedPiecesFormat"],
                    movement.AffectedPieces),
                OriginText = string.Format(
                    localization["MovementHistory_OriginFormat"],
                    localization[movement.Origin == MatchMoveOrigin.Manual
                        ? "MovementHistory_ManualOrigin"
                        : "MovementHistory_AutomaticOrigin"])
            });
        }

        movementsItemsControl.ItemsSource = items;
        emptyMovementsTextBlock.Visibility = items.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        movementsScrollViewer.Visibility = items.Count == 0
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

    private sealed class MovementHistoryItem
    {
        public string SequenceText { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
        public string PlayerText { get; set; } = string.Empty;
        public string PositionText { get; set; } = string.Empty;
        public string AffectedText { get; set; } = string.Empty;
        public string OriginText { get; set; } = string.Empty;
    }
}
