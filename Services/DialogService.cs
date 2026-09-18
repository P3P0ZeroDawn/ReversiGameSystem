using System.Windows;
using ReversiGameSystem.Localization;

namespace ReversiGameSystem.Services;

/// <summary>
/// Opens localized alerts owned by the current application window.
/// </summary>
internal sealed class DialogService
{
    private readonly LocalizationManager _localization;

    internal DialogService(LocalizationManager localization)
    {
        _localization = localization;
    }

    internal void ShowInformation(DependencyObject source, string titleKey, string messageKey)
    {
        Show(source, new LocalizedAlert(titleKey, messageKey), MessageBoxImage.Information);
    }

    internal void ShowWarning(DependencyObject source, string titleKey, string messageKey)
    {
        Show(source, new LocalizedAlert(titleKey, messageKey), MessageBoxImage.Warning);
    }

    internal void ShowError(DependencyObject source, string titleKey, string messageKey)
    {
        Show(source, new LocalizedAlert(titleKey, messageKey), MessageBoxImage.Error);
    }

    private void Show(DependencyObject source, LocalizedAlert alert, MessageBoxImage icon)
    {
        Window? owner = Window.GetWindow(source);
        if (owner != null)
        {
            MessageBox.Show(
                owner,
                _localization[alert.MessageKey],
                _localization[alert.TitleKey],
                MessageBoxButton.OK,
                icon);
            return;
        }

        MessageBox.Show(
            _localization[alert.MessageKey],
            _localization[alert.TitleKey],
            MessageBoxButton.OK,
            icon);
    }

    private readonly struct LocalizedAlert
    {
        internal LocalizedAlert(string titleKey, string messageKey)
        {
            TitleKey = titleKey;
            MessageKey = messageKey;
        }

        internal string TitleKey { get; }

        internal string MessageKey { get; }
    }
}
