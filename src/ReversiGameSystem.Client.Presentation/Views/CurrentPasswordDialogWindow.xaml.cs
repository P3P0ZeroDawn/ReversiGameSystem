using System.Windows;
using System.Windows.Input;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Views;

internal partial class CurrentPasswordDialogWindow : Window
{
    internal CurrentPasswordDialogWindow(LocalizationManager localization)
    {
        InitializeComponent();
        currentPasswordBox.MaxLength = PasswordPolicy.MaximumInputLength;
        Title = localization["AccountDeactivation_PasswordTitle"];
        dialogTitleTextBlock.Text = Title;
        dialogMessageTextBlock.Text = localization["AccountDeactivation_PasswordMessage"];
        currentPasswordLabel.Content = localization["PasswordModification_CurrentPasswordLabel"];
        confirmButton.Content = localization["Common_ConfirmButton"];
        cancelButton.Content = localization["Common_CancelButton"];
        currentPasswordBox.PasswordChanged += OnPasswordChanged;
        confirmButton.IsEnabled = false;
        Loaded += OnDialogLoaded;
    }

    internal string CurrentPassword { get; private set; } = string.Empty;

    internal void ClearPassword()
    {
        CurrentPassword = string.Empty;
        currentPasswordBox.Clear();
    }

    private void OnDialogLoaded(object sender, RoutedEventArgs e)
    {
        currentPasswordBox.Focus();
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        confirmButton.IsEnabled = !string.IsNullOrEmpty(currentPasswordBox.Password);
    }

    private void OnTitleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        if (!confirmButton.IsEnabled)
        {
            return;
        }
        CurrentPassword = currentPasswordBox.Password;
        currentPasswordBox.Clear();
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        ClearPassword();
        DialogResult = false;
    }
}
