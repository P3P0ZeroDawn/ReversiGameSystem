using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Services;

/// <summary>
/// Selects, validates, and confirms a proposed board background.
/// </summary>
internal sealed class BoardBackgroundSelectionService
{
    private readonly LocalizationManager _localization;
    private readonly DialogService _dialogService;

    internal BoardBackgroundSelectionService(
        LocalizationManager localization,
        DialogService dialogService)
    {
        _localization = localization;
        _dialogService = dialogService;
    }

    internal bool TrySelect(
        DependencyObject source,
        out string imagePath,
        out BitmapSource? preview)
    {
        imagePath = string.Empty;
        preview = null;
        OpenFileDialog fileDialog = new OpenFileDialog
        {
            Title = _localization["BoardBackground_SelectImageTitle"],
            Filter = _localization["BoardBackground_ImageFilterLabel"]
                + "|*.png;*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };

        Window? owner = Window.GetWindow(source);
        bool? selected = owner == null ? fileDialog.ShowDialog() : fileDialog.ShowDialog(owner);
        if (selected != true)
        {
            return false;
        }

        if (!BoardBackgroundPolicy.TryCreatePreview(fileDialog.FileName, out preview)
            || preview == null)
        {
            _dialogService.ShowWarning(
                source,
                "BoardBackground_ImageNotSupportedTitle",
                "BoardBackground_ImageNotSupportedMessage");
            return false;
        }

        if (!_dialogService.Confirm(
            source,
            "BoardBackground_ConfirmationTitle",
            "BoardBackground_ConfirmationMessage"))
        {
            preview = null;
            return false;
        }

        imagePath = fileDialog.FileName;
        return true;
    }
}
