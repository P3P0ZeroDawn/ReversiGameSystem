using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Services;

/// <summary>
/// Selects, validates, and confirms a profile image before it is applied.
/// </summary>
internal sealed class ProfileImageSelectionService
{
    private readonly LocalizationManager _localization;
    private readonly DialogService _dialogService;

    internal ProfileImageSelectionService(
        LocalizationManager localization,
        DialogService dialogService)
    {
        _localization = localization;
        _dialogService = dialogService;
    }

    internal ProfileImageSelection? Select(
        DependencyObject source,
        string resourcePrefix)
    {
        ProfileImageSelection? selection = null;
        OpenFileDialog fileDialog = new OpenFileDialog
        {
            Title = _localization[resourcePrefix + "_SelectProfileImageTitle"],
            Filter = _localization[resourcePrefix + "_ProfileImageFilterLabel"]
                + "|*.png;*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };

        Window? owner = Window.GetWindow(source);
        bool? selected = owner == null ? fileDialog.ShowDialog() : fileDialog.ShowDialog(owner);
        if (selected != true)
        {
            return selection;
        }

        BitmapSource? preview = ProfilePicturePolicy.CreatePreview(fileDialog.FileName);
        if (preview == null)
        {
            _dialogService.ShowWarning(
                source,
                resourcePrefix + "_ImageNotSupportedTitle",
                resourcePrefix + "_ImageNotSupportedMessage");
            return selection;
        }

        if (!_dialogService.Confirm(
            source,
            "ProfileImageModification_ConfirmationTitle",
            "ProfileImageModification_ConfirmationMessage"))
        {
            return selection;
        }

        selection = new ProfileImageSelection(fileDialog.FileName, preview);
        return selection;
    }
}

internal sealed class ProfileImageSelection
{
    internal ProfileImageSelection(string filePath, BitmapSource preview)
    {
        FilePath = filePath;
        Preview = preview;
    }

    internal string FilePath { get; }

    internal BitmapSource Preview { get; }
}
