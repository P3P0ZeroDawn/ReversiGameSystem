using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using ReversiGameSystem.Features.Preferences;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Views;

public partial class SettingsPage : Page
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private readonly SettingsViewModel _viewModel;

    public SettingsPage()
    {
        _viewModel = new SettingsViewModel(ClientRuntime.PreferencesService,
            ClientRuntime.CurrentSession);
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.IsLoaded)
        {
            ShowFailure(await _viewModel.LoadPreferences());
        }
    }

    private async void OnProfilePicturesPreferenceChanged(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowFailure(await _viewModel.ChangePieceStyle(
                profilePicturesCheckBox.IsChecked == true));
        }
        finally
        {
            BindingOperations.GetBindingExpression(profilePicturesCheckBox,
                CheckBox.IsCheckedProperty)
                ?.UpdateTarget();
        }
    }

    private async void OnCustomBackgroundPreferenceChanged(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowFailure(await _viewModel.ChangeCustomBackground(
                customBackgroundCheckBox.IsChecked == true));
        }
        finally
        {
            BindingOperations.GetBindingExpression(customBackgroundCheckBox,
                CheckBox.IsCheckedProperty)
                ?.UpdateTarget();
        }
    }

    private async void OnChangeBackgroundClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog picker = new OpenFileDialog
        {
            Title = LocalizationManager.Instance["BoardBackground_SelectImageTitle"],
            Filter = LocalizationManager.Instance["BoardBackground_ImageFilterLabel"]
                + "|*.png;*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };
        Window? owner = Window.GetWindow(this);
        bool? selected = owner == null ? picker.ShowDialog() : picker.ShowDialog(owner);
        if (selected != true)
        {
            return;
        }
        if (BoardBackgroundPolicy.CreatePreview(picker.FileName) == null)
        {
            ShowInvalidImage();
            return;
        }
        if (!_dialogService.Confirm(this,
            "BoardBackground_ConfirmationTitle", "BoardBackground_ConfirmationMessage"))
        {
            return;
        }
        PlayerImageContent image;
        try
        {
            image = await PlayerImageService.ReadImageFile(picker.FileName);
        }
        catch (IOException)
        {
            ShowInvalidImage();
            return;
        }
        catch (UnauthorizedAccessException)
        {
            ShowInvalidImage();
            return;
        }
        PreferencesStatus status = await _viewModel.ChangeBoardBackground(image);
        ShowFailure(status);
        if (status == PreferencesStatus.Success)
        {
            _dialogService.ShowInformation(this,
                "BoardBackground_SuccessTitle", "BoardBackground_SuccessMessage");
        }
    }

    private void ShowInvalidImage()
    {
        _dialogService.ShowWarning(this,
            "BoardBackground_ImageNotSupportedTitle", "BoardBackground_ImageNotSupportedMessage");
    }

    private void ShowFailure(PreferencesStatus status)
    {
        if (status != PreferencesStatus.Success && status != PreferencesStatus.OperationInProgress)
        {
            string prefix = status == PreferencesStatus.ServiceUnavailable
                ? "Settings_ServerConnectionFailed" : "Settings_PersistenceError";
            _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
        }
    }

    private void OnViewModelChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        bool hasPreview = _viewModel.BackgroundPreview != null;
        backgroundPreviewImage.Visibility = hasPreview ? Visibility.Visible : Visibility.Collapsed;
        backgroundMaskImage.Visibility = hasPreview ? Visibility.Visible : Visibility.Collapsed;
        noBackgroundTextBlock.Visibility = hasPreview ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnBackToMenuClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new MainMenuPage());
    }

    private async void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext == _viewModel && _viewModel.CanEdit)
        {
            await LanguageChangeService.ConfirmSelection((ComboBox)sender,
                _viewModel.ChangeLanguage);
        }
    }
}
