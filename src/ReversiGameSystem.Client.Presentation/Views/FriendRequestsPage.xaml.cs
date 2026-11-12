using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Lists pending friendship requests received by the authenticated player.
/// </summary>
public partial class FriendRequestsPage : Page
{
    private readonly DialogService _dialogService =
        new DialogService(LocalizationManager.Instance);
    private readonly FriendRequestsService _friendRequestsService;
    private readonly FriendRequestResponseService _responseService;
    private IReadOnlyList<FriendshipRequest> _requests =
        System.Array.Empty<FriendshipRequest>();

    /// <summary>
    /// Initializes the pending friend-requests page.
    /// </summary>
    public FriendRequestsPage()
    {
        InitializeComponent();
        string username = ClientRuntime.CurrentSession.Username
            ?? LocalizationManager.Instance["Game_DefaultPlayerName"];
        FriendshipStore store = FriendshipPreviewState.GetStore(username);
        _friendRequestsService = new FriendRequestsService(username, store);
        _responseService = new FriendRequestResponseService(username, store);
        LoadRequests();
    }

    private void LoadRequests()
    {
        FriendRequestsRetrievalResult result =
            _friendRequestsService.TryGetPending(out _requests);
        if (result != FriendRequestsRetrievalResult.Success)
        {
            requestsListBox.ItemsSource = null;
            emptyRequestsTextBlock.Visibility = Visibility.Collapsed;
            string prefix = result == FriendRequestsRetrievalResult.ServerConnectionFailed
                ? "FriendRequests_ServerConnectionFailed"
                : "FriendRequests_RetrievalError";
            _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
            return;
        }

        CultureInfo culture = CultureInfo.GetCultureInfo(
            LocalizationManager.Instance.CultureName);
        List<FriendRequestItem> items = new List<FriendRequestItem>();
        foreach (FriendshipRequest request in _requests)
        {
            string receivedAt = request.CreatedAtUtc
                .ToLocalTime()
                .ToString("g", culture);
            items.Add(new FriendRequestItem
            {
                RequestId = request.Id,
                SenderUsername = request.SenderUsername,
                ReceivedAtText = receivedAt,
                DateText = string.Format(
                    LocalizationManager.Instance["FriendRequests_DateFormat"],
                    receivedAt)
            });
        }

        requestsListBox.ItemsSource = items;
        emptyRequestsTextBlock.Visibility = items.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnRequestSelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (requestsListBox.SelectedItem is not FriendRequestItem item)
        {
            return;
        }

        requestsListBox.SelectedItem = null;
        ActionChoiceDialogOptions options = new ActionChoiceDialogOptions(
            "FriendRequestResponse_Title",
            "FriendRequestResponse_Message",
            "FriendRequestResponse_AcceptButton",
            "FriendRequestResponse_RejectButton",
            "Common_CancelButton")
        {
            MessageArguments = new object[]
            {
                item.SenderUsername,
                item.ReceivedAtText
            }
        };
        ActionChoiceDialogResult choice = _dialogService.Choose(this, options);
        if (choice == ActionChoiceDialogResult.Cancel)
        {
            return;
        }

        FriendRequestResponseResult result = _responseService.Respond(
            item.RequestId,
            choice == ActionChoiceDialogResult.Primary);
        HandleResponseResult(result, item.SenderUsername);
    }

    private void HandleResponseResult(
        FriendRequestResponseResult result,
        string senderUsername)
    {
        switch (result)
        {
            case FriendRequestResponseResult.Accepted:
                _dialogService.ShowInformation(
                    this,
                    "FriendRequestResponse_AcceptedTitle",
                    "FriendRequestResponse_AcceptedMessage",
                    senderUsername);
                LoadRequests();
                break;
            case FriendRequestResponseResult.Rejected:
                _dialogService.ShowInformation(
                    this,
                    "FriendRequestResponse_RejectedTitle",
                    "FriendRequestResponse_RejectedMessage",
                    senderUsername);
                LoadRequests();
                break;
            case FriendRequestResponseResult.AlreadyProcessed:
                _dialogService.ShowWarning(
                    this,
                    "FriendRequestResponse_AlreadyProcessedTitle",
                    "FriendRequestResponse_AlreadyProcessedMessage");
                LoadRequests();
                break;
            case FriendRequestResponseResult.Unauthorized:
                _dialogService.ShowError(
                    this,
                    "FriendRequestResponse_UnauthorizedTitle",
                    "FriendRequestResponse_UnauthorizedMessage");
                break;
            case FriendRequestResponseResult.ServerConnectionFailed:
                _dialogService.ShowError(
                    this,
                    "FriendRequestResponse_ServerConnectionFailedTitle",
                    "FriendRequestResponse_ServerConnectionFailedMessage");
                break;
            case FriendRequestResponseResult.PersistenceError:
                _dialogService.ShowError(
                    this,
                    "FriendRequestResponse_PersistenceErrorTitle",
                    "FriendRequestResponse_PersistenceErrorMessage");
                break;
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
            LoadRequests();
        }
    }

    private sealed class FriendRequestItem
    {
        public string RequestId { get; set; } = string.Empty;
        public string SenderUsername { get; set; } = string.Empty;
        public string ReceivedAtText { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
    }
}
