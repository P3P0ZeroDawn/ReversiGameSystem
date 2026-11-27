using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Input;
using ReversiGameSystem.Features.Accounts;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

internal partial class PasswordModificationDialogWindow : Window
{
    private readonly DialogService _dialogService;
    private readonly AccountViewModel _viewModel;

    internal PasswordModificationDialogWindow(
        LocalizationManager localization,
        DialogService dialogService,
        AccountViewModel viewModel)
    {
        InitializeComponent();
        _dialogService = dialogService;
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Closing += OnDialogClosing;

        Title = localization["PasswordModification_DialogTitle"];
        dialogTitleTextBlock.Text = Title;
        dialogMessageTextBlock.Text = localization["PasswordModification_DialogMessage"];
        currentPasswordLabel.Content = localization["PasswordModification_CurrentPasswordLabel"];
        newPasswordLabel.Content = localization["PasswordModification_NewPasswordLabel"];
        confirmationLabel.Content = localization["PasswordModification_ConfirmationLabel"];
        passwordHintTextBlock.Text = localization["PasswordModification_PasswordHint"];
        cancelButton.Content = localization["Common_CancelButton"];
        confirmButton.Content = localization["PasswordModification_ConfirmButton"];
        Loaded += OnDialogLoaded;
    }

    private void OnDialogLoaded(object sender, RoutedEventArgs e)
    {
        currentPasswordBox.Focus();
    }

    private void OnTitleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private async void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.IsSubmitting)
        {
            return;
        }
        IsEnabled = false;
        try
        {
            AccountStatus status = await _viewModel.ChangePassword(
                currentPasswordBox.Password, newPasswordBox.Password, confirmationBox.Password);
            if (status == AccountStatus.Success)
            {
                DialogResult = true;
            }
            else
            {
                _dialogService.ShowAccountFailure(this, status);
                currentPasswordBox.Clear();
            }
        }
        catch (FaultException<ServerProxy.ServiceFault> exception)
        {
            ClientRuntime.Errors.ReportOperationFailure(
                exception, nameof(PasswordModificationDialogWindow));
        }
        finally
        {
            currentPasswordBox.Clear();
            newPasswordBox.Clear();
            confirmationBox.Clear();
            IsEnabled = true;
        }
    }

    private void OnDialogClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = _viewModel.IsSubmitting;
        if (!e.Cancel)
        {
            currentPasswordBox.Clear();
            newPasswordBox.Clear();
            confirmationBox.Clear();
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
