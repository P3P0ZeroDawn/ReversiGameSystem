using System;
using System.ServiceModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Features.Friendships;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

internal partial class AddFriendsWindow : Window
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private readonly FriendshipViewModel _viewModel = new FriendshipViewModel(
        ClientRuntime.FriendshipService, ClientRuntime.CurrentSession);
    private bool _ready;

    internal AddFriendsWindow()
    {
        InitializeComponent();
        resultsItemsControl.SetBinding(ItemsControl.ItemsSourceProperty,
            new Binding(nameof(FriendshipViewModel.Players)) { Source = _viewModel });
        Loaded += OnLoaded;
        Closed += OnClosed;
        Closing += OnClosing;
    }

    internal PlayerAccount? SelectedPlayer { get; private set; }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        searchTextBox.Focus();
        _viewModel.IsActive = true;
        _ready = true;
        await LoadCandidates();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.IsActive = false;
        _ready = false;
    }

    private async Task LoadCandidates()
    {
        try
        {
            FriendshipStatus status = await _viewModel.FindPlayers(searchTextBox.Text);
            if (!IsLoaded)
            {
                return;
            }
            if (status == FriendshipStatus.Success)
            {
                string messageKey = string.IsNullOrWhiteSpace(searchTextBox.Text)
                    ? "AddFriends_SearchPromptMessage" : "AddFriends_NoResultsMessage";
                noResultsTextBlock.Text = LocalizationManager.Instance[messageKey];
                noResultsTextBlock.Visibility = _viewModel.Players.Length == 0
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
                ClientRuntime.Errors.ReportOperationFailure(exception, nameof(AddFriendsWindow));
            }
            else
            {
                ClientRuntime.Errors.RecordFailure(
                    exception, nameof(AddFriendsWindow), LogLevel.Error);
            }
        }
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_ready)
        {
            await LoadCandidates();
        }
    }

    private async void OnSendRequestClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not PlayerAccount recipient
            || _viewModel.IsSubmitting)
        {
            return;
        }
        IsEnabled = false;
        try
        {
            FriendshipStatus status = await _viewModel.SendFriendRequest(recipient.AccountId);
            if (!IsLoaded)
            {
                return;
            }
            if (status == FriendshipStatus.Success)
            {
                _dialogService.ShowInformation(this, "FriendRequest_SentTitle",
                    "FriendRequest_SentMessage", recipient.Username);
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
                ClientRuntime.Errors.ReportOperationFailure(
                    exception, nameof(AddFriendsWindow));
            }
            else
            {
                ClientRuntime.Errors.RecordFailure(
                    exception, nameof(AddFriendsWindow), LogLevel.Error);
            }
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void OnProfileClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is PlayerAccount account)
        {
            SelectedPlayer = account;
            DialogResult = true;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = _viewModel.IsSubmitting;
    }

    private void OnTitleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
