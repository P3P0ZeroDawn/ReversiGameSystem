using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Searches active player accounts and sends friendship requests.
/// </summary>
public partial class AddFriendsPage : Page
{
    private readonly DialogService _dialogService =
        new DialogService(LocalizationManager.Instance);
    private readonly FriendRequestService _friendRequestService;

    /// <summary>
    /// Initializes the add-friends page for the authenticated account.
    /// </summary>
    public AddFriendsPage()
    {
        InitializeComponent();
        string requesterUsername = ClientRuntime.CurrentSession.Username
            ?? LocalizationManager.Instance["Game_DefaultPlayerName"];
        _friendRequestService = FriendshipPreviewState.GetService(requesterUsername);
        LoadCandidates();
    }

    private void LoadCandidates()
    {
        FriendSearchResult result = _friendRequestService.Search(
            searchTextBox.Text,
            out IReadOnlyList<FriendCandidate> candidates);
        if (result != FriendSearchResult.Success)
        {
            resultsItemsControl.ItemsSource = null;
            noResultsTextBlock.Visibility = Visibility.Collapsed;
            string prefix = result == FriendSearchResult.ServerConnectionFailed
                ? "FriendRequest_ServerConnectionFailed"
                : "AddFriends_RetrievalError";
            _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
            return;
        }

        resultsItemsControl.ItemsSource = candidates;
        noResultsTextBlock.Visibility = candidates.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_friendRequestService != null)
        {
            LoadCandidates();
        }
    }

    private void OnSendRequestClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string recipientUsername)
        {
            return;
        }

        ShowFriendRequestResult(
            _friendRequestService.Send(recipientUsername),
            recipientUsername);
    }

    private void ShowFriendRequestResult(
        FriendRequestResult result,
        string recipientUsername)
    {
        if (result == FriendRequestResult.Success)
        {
            _dialogService.ShowInformation(
                this,
                "FriendRequest_SentTitle",
                "FriendRequest_SentMessage",
                recipientUsername);
            return;
        }

        string prefix = result == FriendRequestResult.CannotAddYourself
            ? "FriendRequest_CannotAddYourself"
            : result == FriendRequestResult.FriendshipAlreadyExists
                ? "FriendRequest_FriendshipAlreadyExists"
                : result == FriendRequestResult.FriendRequestAlreadyPending
                    ? "FriendRequest_AlreadyPending"
                    : result == FriendRequestResult.AccountNotAvailable
                        ? "FriendRequest_AccountNotAvailable"
                        : result == FriendRequestResult.PersistenceError
                            ? "FriendRequest_PersistenceError"
                            : "FriendRequest_ServerConnectionFailed";
        _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
    }

    private void OnProfileClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string username)
        {
            NavigationService?.Navigate(new AccountInformationPage(
                username,
                AccountInformationReturnTarget.AddFriends));
        }
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new MainMenuPage());
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string previousCulture = LocalizationManager.Instance.CultureName;
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
        if (LocalizationManager.Instance.CultureName != previousCulture)
        {
            LoadCandidates();
        }
    }
}
