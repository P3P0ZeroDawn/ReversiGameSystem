using System.Linq;
using System.Windows;
using System.Windows.Input;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Collects a new turn duration for a match that has not started.
/// </summary>
internal partial class MatchSettingsDialogWindow : Window
{
    private readonly DialogService _dialogService;
    private readonly MatchSession _session;
    private readonly string _requestingPlayerName;
    private readonly MatchSettingsService _matchSettingsService;

    internal MatchSettingsDialogWindow(
        LocalizationManager localization,
        DialogService dialogService,
        MatchSession session,
        string requestingPlayerName,
        MatchSettingsService matchSettingsService)
    {
        InitializeComponent();
        _dialogService = dialogService;
        _session = session;
        _requestingPlayerName = requestingPlayerName;
        _matchSettingsService = matchSettingsService;

        Title = localization["MatchSettings_Title"];
        dialogTitleTextBlock.Text = Title;
        dialogMessageTextBlock.Text = localization["MatchSettings_Description"];
        turnTimeLabel.Content = localization["MatchSettings_TurnTimeLabel"];
        secondsSuffixTextBlock.Text = localization["MatchCreation_SecondsSuffix"];
        turnTimeHintTextBlock.Text = localization["MatchSettings_TurnTimeHint"];
        cancelButton.Content = localization["Common_CancelButton"];
        saveButton.Content = localization["MatchSettings_SaveButton"];
        turnTimeTextBox.Text = session.TurnTimeSeconds.ToString();
        Loaded += OnDialogLoaded;
    }

    private void OnDialogLoaded(object sender, RoutedEventArgs e)
    {
        turnTimeTextBox.Focus();
        turnTimeTextBox.SelectAll();
    }

    private void OnTitleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnTurnTimePreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text.Any(character => character < '0' || character > '9');
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (!TurnTimePolicy.TryParse(turnTimeTextBox.Text, out int turnTimeSeconds))
        {
            ShowWarning("InvalidTurnTime");
            FocusTurnTime();
            return;
        }

        MatchSettingsModificationResult result = _matchSettingsService.ChangeTurnTime(
            _session,
            _requestingPlayerName,
            turnTimeSeconds);

        switch (result)
        {
            case MatchSettingsModificationResult.Success:
                DialogResult = true;
                break;
            case MatchSettingsModificationResult.AlreadyStarted:
                ShowWarning("AlreadyStarted");
                DialogResult = false;
                break;
            case MatchSettingsModificationResult.Unauthorized:
                ShowWarning("Unauthorized");
                DialogResult = false;
                break;
            case MatchSettingsModificationResult.InvalidTurnTime:
                ShowWarning("InvalidTurnTime");
                FocusTurnTime();
                break;
            case MatchSettingsModificationResult.PersistenceError:
                ShowError("PersistenceError");
                DialogResult = false;
                break;
            default:
                ShowError("ServerConnectionFailed");
                DialogResult = false;
                break;
        }
    }

    private void FocusTurnTime()
    {
        turnTimeTextBox.Focus();
        turnTimeTextBox.SelectAll();
    }

    private void ShowWarning(string resourceName)
    {
        _dialogService.ShowWarning(
            this,
            $"MatchSettings_{resourceName}Title",
            $"MatchSettings_{resourceName}Message");
    }

    private void ShowError(string resourceName)
    {
        _dialogService.ShowError(
            this,
            $"MatchSettings_{resourceName}Title",
            $"MatchSettings_{resourceName}Message");
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
