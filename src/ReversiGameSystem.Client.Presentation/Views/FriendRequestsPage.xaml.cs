using System;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Features.Friendships;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

public partial class FriendRequestsPage : Page
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private readonly FriendshipViewModel _viewModel = new FriendshipViewModel(
        ClientRuntime.FriendshipService, ClientRuntime.CurrentSession);
    private bool _subscribed;

    public FriendRequestsPage()
    {
        InitializeComponent();
        requestsItemsControl.SetBinding(ItemsControl.ItemsSourceProperty,
            new Binding(nameof(FriendshipViewModel.IncomingRequests)) { Source = _viewModel });
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.IsActive = true;
        if (!_subscribed)
        {
            ClientRuntime.FriendshipService.RequestsChanged += OnRequestsChanged;
            _subscribed = true;
        }
        await LoadRequests();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _viewModel.IsActive = false;
        ClientRuntime.FriendshipService.RequestsChanged -= OnRequestsChanged;
        _subscribed = false;
    }

    private void OnRequestsChanged()
    {
        Dispatcher.BeginInvoke(new Action(OnRequestsInvalidated));
    }

    private async void OnRequestsInvalidated()
    {
        if (IsLoaded && _subscribed)
        {
            await LoadRequests();
        }
    }

    private async Task LoadRequests()
    {
        try
        {
            FriendshipStatus status = await _viewModel.LoadPendingRequests();
            if (!IsLoaded)
            {
                return;
            }
            if (status == FriendshipStatus.Success)
            {
                emptyRequestsTextBlock.Visibility = _viewModel.IncomingRequests.Length == 0
                    ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (status != FriendshipStatus.SubmissionInProgress)
            {
                _dialogService.ShowFriendshipFailure(this, status);
            }
        }
        catch (FaultException<ServerProxy.ServiceFault> exception)
        {
            if (IsLoaded)
            {
                ClientRuntime.Errors.ReportOperationFailure(exception, nameof(FriendRequestsPage));
            }
            else
            {
                ClientRuntime.Errors.RecordFailure(
                    exception, nameof(FriendRequestsPage), LogLevel.Error);
            }
        }
    }

    private async void OnReviewRequestClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not FriendRequestItem item
            || _viewModel.IsSubmitting)
        {
            return;
        }
        ActionChoiceDialogOptions options = new ActionChoiceDialogOptions(
            "FriendRequestResponse_Title", "FriendRequestResponse_Message",
            "FriendRequestResponse_AcceptButton", "FriendRequestResponse_RejectButton",
            "Common_CancelButton")
        {
            MessageArguments = new object[] { item.Request.Sender.Username, item.ReceivedAtText }
        };
        ActionChoiceDialogResult choice = _dialogService.Choose(this, options);
        if (choice == ActionChoiceDialogResult.Cancel)
        {
            return;
        }
        bool accept = choice == ActionChoiceDialogResult.Primary;
        IsEnabled = false;
        try
        {
            FriendshipStatus status = await _viewModel.RespondToFriendRequest(item.Request, accept);
            if (!IsLoaded)
            {
                return;
            }
            if (status == FriendshipStatus.Success)
            {
                string prefix = accept ? "FriendRequestResponse_Accepted"
                    : "FriendRequestResponse_Rejected";
                _dialogService.ShowInformation(this, prefix + "Title", prefix + "Message",
                    item.Request.Sender.Username);
            }
            else if (status != FriendshipStatus.SubmissionInProgress)
            {
                _dialogService.ShowFriendshipFailure(this, status);
            }
            if (status is FriendshipStatus.Success or FriendshipStatus.RequestUnavailable)
            {
                await LoadRequests();
            }
        }
        catch (FaultException<ServerProxy.ServiceFault> exception)
        {
            if (IsLoaded)
            {
                ClientRuntime.Errors.ReportOperationFailure(
                    exception, nameof(FriendRequestsPage));
            }
            else
            {
                ClientRuntime.Errors.RecordFailure(
                    exception, nameof(FriendRequestsPage), LogLevel.Error);
            }
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new MainMenuPage());
    }

    private async void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LanguageChangeService.ConfirmSelection((ComboBox)sender);
        _viewModel.RefreshConfirmedRequests();
    }
}
