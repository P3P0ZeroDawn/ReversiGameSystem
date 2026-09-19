using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays the signed-in player's appearance preferences.
/// </summary>
public partial class SettingsPage : Page
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private readonly BoardBackgroundSelectionService _selectionService;
    private readonly IBoardBackgroundService _backgroundService = new BoardBackgroundService();

    /// <summary>
    /// Initializes the settings page.
    /// </summary>
    public SettingsPage()
    {
        InitializeComponent();
        _selectionService = new BoardBackgroundSelectionService(
            LocalizationManager.Instance, _dialogService);
        profilePicturesCheckBox.IsChecked = UserPreferencesState.UseProfilePicturesOnPieces;
        customBackgroundCheckBox.IsChecked = UserPreferencesState.UseCustomBoardBackground;
        profilePicturesCheckBox.Checked += OnProfilePicturesPreferenceChanged;
        profilePicturesCheckBox.Unchecked += OnProfilePicturesPreferenceChanged;
        customBackgroundCheckBox.Checked += OnCustomBackgroundPreferenceChanged;
        customBackgroundCheckBox.Unchecked += OnCustomBackgroundPreferenceChanged;
        ShowBackgroundPreview();
    }

    private void OnProfilePicturesPreferenceChanged(object sender, RoutedEventArgs e)
    {
        UserPreferencesState.UseProfilePicturesOnPieces = profilePicturesCheckBox.IsChecked == true;
    }

    private void OnCustomBackgroundPreferenceChanged(object sender, RoutedEventArgs e)
    {
        UserPreferencesState.UseCustomBoardBackground = customBackgroundCheckBox.IsChecked == true;
    }

    private void OnChangeBackgroundClick(object sender, RoutedEventArgs e)
    {
        if (!_selectionService.TrySelect(
            this, out string imagePath, out BitmapSource? preview)
            || preview == null)
        {
            return;
        }

        BoardBackgroundSaveResult result = _backgroundService.SaveForCurrentPlayer(imagePath);
        if (result == BoardBackgroundSaveResult.ServerConnectionFailed)
        {
            _dialogService.ShowError(
                this,
                "BoardBackground_ServerConnectionFailedTitle",
                "BoardBackground_ServerConnectionFailedMessage");
            return;
        }

        if (result == BoardBackgroundSaveResult.PersistenceError)
        {
            _dialogService.ShowError(
                this,
                "BoardBackground_PersistenceErrorTitle",
                "BoardBackground_PersistenceErrorMessage");
            return;
        }

        UserPreferencesState.SetBoardBackground(preview);
        customBackgroundCheckBox.IsChecked = true;
        ShowBackgroundPreview();
        _dialogService.ShowInformation(
            this,
            "BoardBackground_SuccessTitle",
            "BoardBackground_SuccessMessage");
    }

    private void ShowBackgroundPreview()
    {
        BitmapSource? preview = UserPreferencesState.BoardBackgroundPreview;
        backgroundPreviewImage.Source = preview;
        backgroundPreviewImage.Visibility = preview == null
            ? Visibility.Collapsed : Visibility.Visible;
        backgroundMaskImage.Visibility = preview == null
            ? Visibility.Collapsed : Visibility.Visible;
        noBackgroundTextBlock.Visibility = preview == null
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnBackToMenuClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new MainMenuPage());
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
    }
}
