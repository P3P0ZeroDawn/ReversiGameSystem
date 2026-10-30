using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays the server-confirmed match and participant chat using the player's visual preferences.
/// </summary>
public partial class GamePage : Page
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private readonly InstructionsService _instructionsService =
        new InstructionsService(key => LocalizationManager.Instance[key]);

    private async void OnSendMessageClick(object sender, RoutedEventArgs e)
    {
        await SendRemoteChatMessageAsync();
    }

    private async void OnChatMessageKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await SendRemoteChatMessageAsync();
        }
    }

    private void HandleChatSendFailure(
        ChatSendResult result,
        DateTime? muteEndsAtUtc)
    {
        switch (result)
        {
            case ChatSendResult.EmptyMessage:
                _dialogService.ShowWarning(
                    this,
                    "Chat_EmptyMessageTitle",
                    "Chat_EmptyMessageMessage");
                break;
            case ChatSendResult.MessageTooLong:
                _dialogService.ShowWarning(
                    this,
                    "Chat_MessageTooLongTitle",
                    "Chat_MessageTooLongMessage");
                break;
            case ChatSendResult.Unauthorized:
                _dialogService.ShowError(
                    this,
                    "Chat_UnauthorizedTitle",
                    "Chat_UnauthorizedMessage");
                break;
            case ChatSendResult.PlayerMuted:
                string muteEnd = muteEndsAtUtc.HasValue
                    ? muteEndsAtUtc.Value.ToLocalTime().ToString(
                        "g",
                        CultureInfo.GetCultureInfo(
                            LocalizationManager.Instance.CultureName))
                    : LocalizationManager.Instance["AccountInformation_UnavailableValue"];
                _dialogService.ShowWarning(
                    this,
                    "Chat_PlayerMutedTitle",
                    "Chat_PlayerMutedMessage",
                    muteEnd);
                break;
            case ChatSendResult.MessageBlocked:
                _dialogService.ShowWarning(
                    this,
                    "Chat_MessageBlockedTitle",
                    "Chat_MessageBlockedMessage");
                break;
            case ChatSendResult.ServerConnectionFailed:
                _dialogService.ShowError(
                    this,
                    "Chat_ServerConnectionFailedTitle",
                    "Chat_ServerConnectionFailedMessage");
                break;
            case ChatSendResult.MessagePersistenceError:
                _dialogService.ShowError(
                    this,
                    "Chat_MessagePersistenceErrorTitle",
                    "Chat_MessagePersistenceErrorMessage");
                break;
            case ChatSendResult.ModerationPersistenceError:
                _dialogService.ShowError(
                    this,
                    "Chat_ModerationPersistenceErrorTitle",
                    "Chat_ModerationPersistenceErrorMessage");
                break;
            case ChatSendResult.MutePersistenceError:
                _dialogService.ShowError(
                    this,
                    "Chat_MutePersistenceErrorTitle",
                    "Chat_MutePersistenceErrorMessage");
                break;
        }
    }

    private void OnInformationClick(object sender, RoutedEventArgs e)
    {
        ShowRemoteInstructions();
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
    }

    private void OnLeaveMatchClick(object sender, RoutedEventArgs e)
    {
        CloseRemoteView();
    }
}
