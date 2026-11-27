using System;
using System.Windows;
using ReversiGameSystem.Features.Accounts;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Views;

namespace ReversiGameSystem.Services;

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

    internal void ShowAccountFailure(DependencyObject source, AccountStatus status)
    {
        string resourcePrefix = status switch
        {
            AccountStatus.MissingRequiredInformation =>
                "PasswordModification_MissingRequiredInformation",
            AccountStatus.PasswordMismatch => "PasswordModification_PasswordMismatch",
            AccountStatus.InvalidPassword => "Account_InvalidPassword",
            AccountStatus.IncorrectCurrentPassword =>
                "PasswordModification_CurrentPasswordIncorrect",
            AccountStatus.InvalidEmailFormat => "PasswordReset_InvalidEmail",
            AccountStatus.InvalidVerificationCode => "LoginScreen_InvalidCode",
            AccountStatus.ExpiredVerificationCode => "LoginScreen_ExpiredCode",
            AccountStatus.ActiveMatchInProgress => "AccountDeactivation_ActiveMatch",
            AccountStatus.InactiveAccount => "LoginScreen_AccountInactive",
            AccountStatus.MissingSessionToken or AccountStatus.InvalidSession =>
                "Account_SessionExpired",
            AccountStatus.ServiceUnavailable => "Account_ServiceUnavailable",
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
        ShowWarning(source, resourcePrefix + "Title", resourcePrefix + "Message");
    }

    internal bool TryChangePassword(DependencyObject source)
    {
        PasswordModificationDialogWindow dialog = new PasswordModificationDialogWindow(
            _localization,
            this,
            new AccountViewModel(ClientRuntime.AccountService, ClientRuntime.CurrentSession));
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

    internal int SelectTurnDuration(DependencyObject source, int currentTurnTimeSeconds)
    {
        MatchSettingsDialogWindow dialog = new MatchSettingsDialogWindow(
            _localization,
            this,
            currentTurnTimeSeconds);
        Window? owner = Window.GetWindow(source);
        if (owner != null)
        {
            dialog.Owner = owner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        if (dialog.ShowDialog() == true)
        {
            return dialog.SelectedTurnTimeSeconds;
        }

        return currentTurnTimeSeconds;
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
