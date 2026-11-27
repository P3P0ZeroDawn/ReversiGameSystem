using System;
using System.ComponentModel;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Features.Matches;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

public partial class MatchHistoryPage : Page
{
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private MatchHistoryViewModel? _viewModel;

    public MatchHistoryPage()
    {
        InitializeComponent();
        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        ClientSession session = ClientRuntime.CurrentSession;
        GameplayClient? gameplay = session.AccountId.HasValue && !session.IsGuest
            ? ClientRuntime.GetGameplayClient(false) : null;
        MatchHistoryViewModel viewModel = new MatchHistoryViewModel(session, gameplay);
        _viewModel = viewModel;
        viewModel.PropertyChanged += OnHistoryChanged;
        historyItemsControl.SetBinding(ItemsControl.ItemsSourceProperty,
            new Binding(nameof(MatchHistoryViewModel.Matches)) { Source = viewModel });
        try
        {
            MatchStatus status = await viewModel.LoadHistory();
            if (viewModel.IsCurrentSession() && status != MatchStatus.Success)
            {
                ShowFailure(status);
            }
        }
        catch (OperationCanceledException) when (!viewModel.IsCurrentSession())
        {
            viewModel.Deactivate();
        }
        catch (FaultException<ReversiGameSystem.ServerProxy.ServiceFault> exception)
        {
            OnHistoryFailure(exception, nameof(OnPageLoaded));
        }
    }

    private void OnHistoryChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            historyItemsControl.IsEnabled = !_viewModel.IsBusy;
            emptyHistoryTextBlock.Visibility = !_viewModel.IsBusy && _viewModel.Matches.Length == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private async void OnViewMatchClick(object sender, RoutedEventArgs e)
    {
        MatchHistoryViewModel? viewModel = _viewModel;
        if (sender is not Button { Tag: long matchId } || viewModel == null || viewModel.IsBusy)
        {
            return;
        }
        try
        {
            MatchStatus status = await viewModel.LoadRecording(matchId);
            if (!viewModel.IsCurrentSession())
            {
                return;
            }
            if (status == MatchStatus.Success)
            {
                MatchRecording recording = viewModel.ReadRecording(matchId);
                NavigationService?.Navigate(new MatchInformationPage(recording));
            }
            else
            {
                ShowFailure(status);
            }
        }
        catch (OperationCanceledException) when (!viewModel.IsCurrentSession())
        {
            viewModel.Deactivate();
        }
        catch (FaultException<ReversiGameSystem.ServerProxy.ServiceFault> exception)
        {
            OnHistoryFailure(exception, nameof(OnViewMatchClick));
        }
    }

    private void OnHistoryFailure(Exception exception, string operation)
    {
        if (IsLoaded && _viewModel?.IsCurrentSession() == true)
        {
            ClientRuntime.Errors.ReportOperationFailure(exception, operation);
        }
        else
        {
            ClientRuntime.Errors.RecordFailure(exception, operation, LogLevel.Error);
        }
    }

    private void ShowFailure(MatchStatus status)
    {
        _dialogService.ShowError(this, "MatchHistory_RetrievalErrorTitle",
            RemoteMatchMessages.GetMessageResourceKey(status, false));
    }

    private void OnBackToMenuClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new MainMenuPage());
    }

    private async void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LanguageChangeService.ConfirmSelection((ComboBox)sender);
        _viewModel?.RefreshLanguage();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnHistoryChanged;
            _viewModel.Deactivate();
        }
    }
}
