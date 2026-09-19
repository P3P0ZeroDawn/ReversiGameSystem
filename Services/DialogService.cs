using System.Windows;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Views;

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

    internal void ShowInformation(
        DependencyObject source,
        string titleKey,
        string messageKey,
        params object[] messageArguments)
    {
        Show(
            source,
            new LocalizedAlert(titleKey, messageKey, messageArguments),
            MessageBoxImage.Information);
    }

    internal void ShowInformationText(
        DependencyObject source,
        string title,
        string message)
    {
        Window? owner = Window.GetWindow(source);
        if (owner == null)
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        MessageBox.Show(
            owner,
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    internal void ShowWarning(
        DependencyObject source,
        string titleKey,
        string messageKey,
        params object[] messageArguments)
    {
        Show(
            source,
            new LocalizedAlert(titleKey, messageKey, messageArguments),
            MessageBoxImage.Warning);
    }

    internal void ShowError(
        DependencyObject source,
        string titleKey,
        string messageKey,
        params object[] messageArguments)
    {
        Show(
            source,
            new LocalizedAlert(titleKey, messageKey, messageArguments),
            MessageBoxImage.Error);
    }

    internal bool Confirm(DependencyObject source, string titleKey, string messageKey)
    {
        string title = _localization[titleKey];
        string message = _localization[messageKey];
        Window? owner = Window.GetWindow(source);
        MessageBoxResult result = owner == null
            ? MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
            : MessageBox.Show(
                owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }

    internal bool Confirm(DependencyObject source, ConfirmationDialogOptions options)
    {
        ConfirmationDialogWindow dialog = new ConfirmationDialogWindow(options, _localization);
        Window? owner = Window.GetWindow(source);
        if (owner != null)
        {
            dialog.Owner = owner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        return dialog.ShowDialog() == true;
    }

    internal ActionChoiceDialogResult Choose(
        DependencyObject source,
        ActionChoiceDialogOptions options)
    {
        ActionChoiceDialogWindow dialog = new ActionChoiceDialogWindow(
            options,
            _localization);
        Window? owner = Window.GetWindow(source);
        if (owner != null)
        {
            dialog.Owner = owner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        dialog.ShowDialog();
        return dialog.SelectedAction;
    }

    internal bool TryGetText(
        DependencyObject source,
        TextInputDialogOptions options,
        out string text)
    {
        TextInputDialogWindow dialog = new TextInputDialogWindow(options, _localization);
        Window? owner = Window.GetWindow(source);
        if (owner != null)
        {
            dialog.Owner = owner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        bool accepted = dialog.ShowDialog() == true;
        text = accepted ? dialog.EnteredText : string.Empty;
        return accepted;
    }

    internal bool TryGetNewPassword(DependencyObject source, out string password)
    {
        NewPasswordDialogWindow dialog = new NewPasswordDialogWindow(_localization, this);
        Window? owner = Window.GetWindow(source);
        if (owner != null)
        {
            dialog.Owner = owner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        bool accepted = dialog.ShowDialog() == true;
        password = accepted ? dialog.NewPassword : string.Empty;
        return accepted;
    }

    internal bool TryChangePassword(DependencyObject source)
    {
        PasswordModificationDialogWindow dialog = new PasswordModificationDialogWindow(
            _localization,
            this,
            new PasswordModificationService());
        Window? owner = Window.GetWindow(source);
        if (owner != null)
        {
            dialog.Owner = owner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        return dialog.ShowDialog() == true;
    }

    internal bool TryModifyMatchSettings(
        DependencyObject source,
        MatchSession session,
        string requestingPlayerName)
    {
        MatchSettingsDialogWindow dialog = new MatchSettingsDialogWindow(
            _localization,
            this,
            session,
            requestingPlayerName,
            new MatchSettingsService());
        Window? owner = Window.GetWindow(source);
        if (owner != null)
        {
            dialog.Owner = owner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        return dialog.ShowDialog() == true;
    }

    private void Show(DependencyObject source, LocalizedAlert alert, MessageBoxImage icon)
    {
        Window? owner = Window.GetWindow(source);
        if (owner != null)
        {
            MessageBox.Show(
                owner,
                alert.GetMessage(_localization),
                _localization[alert.TitleKey],
                MessageBoxButton.OK,
                icon);
            return;
        }

        MessageBox.Show(
            alert.GetMessage(_localization),
            _localization[alert.TitleKey],
            MessageBoxButton.OK,
            icon);
    }

    private readonly struct LocalizedAlert
    {
        internal LocalizedAlert(
            string titleKey,
            string messageKey,
            object[]? messageArguments = null)
        {
            TitleKey = titleKey;
            MessageKey = messageKey;
            MessageArguments = messageArguments ?? System.Array.Empty<object>();
        }

        internal string TitleKey { get; }

        internal string MessageKey { get; }

        internal object[] MessageArguments { get; }

        internal string GetMessage(LocalizationManager localization)
        {
            string message = localization[MessageKey];
            return MessageArguments.Length == 0
                ? message
                : string.Format(message, MessageArguments);
        }
    }
}
