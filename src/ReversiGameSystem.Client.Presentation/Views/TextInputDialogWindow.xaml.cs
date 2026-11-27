using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

internal partial class TextInputDialogWindow : Window
{
    private bool _requestPending;
    private readonly bool _requireText;
    private readonly int _exactDigitCount;

    internal TextInputDialogWindow(TextInputDialogOptions options, LocalizationManager localization)
    {
        InitializeComponent();

        _requireText = options.RequireText;
        _exactDigitCount = options.ExactDigitCount;

        Title = localization[options.TitleKey];
        dialogTitleTextBlock.Text = Title;
        dialogMessageTextBlock.Text = string.Format(
            localization[options.MessageKey], options.MessageArguments);
        inputLabel.Content = localization[options.LabelKey];
        cancelButton.Content = localization[options.CancelButtonKey];
        confirmButton.Content = localization[options.ConfirmButtonKey];
        inputTextBox.MaxLength = _exactDigitCount > 0
            ? _exactDigitCount
            : options.MaximumLength;
        inputTextBox.TextChanged += OnInputTextChanged;
        inputTextBox.Text = options.InitialText;
        AutomationProperties.SetName(inputTextBox, localization[options.LabelKey]);
        UpdateConfirmButtonState();

        Loaded += OnDialogLoaded;
    }

    internal async Task<bool?> ShowDuringRequest<TStatus>(
        Task<TStatus> request, TStatus acceptedStatus)
    {
        _requestPending = true;
        UpdateConfirmButtonState();
        string readyMessage = dialogMessageTextBlock.Text;
        dialogMessageTextBlock.Text = LocalizationManager.Instance["Common_RequestingCodeMessage"];
        Task preparation = PrepareConfirmation();
        bool? confirmed = ShowDialog();
        await preparation;
        return confirmed;

        async Task PrepareConfirmation()
        {
            await Task.Yield();
            bool accepted = false;
            try
            {
                TStatus status = await request;
                accepted = EqualityComparer<TStatus>.Default.Equals(status, acceptedStatus);
                if (accepted && IsVisible)
                {
                    _requestPending = false;
                    dialogMessageTextBlock.Text = readyMessage;
                    UpdateConfirmButtonState();
                }
            }
            finally
            {
                if (!accepted && IsVisible)
                {
                    DialogResult = false;
                }
            }
        }
    }

    internal string EnteredText { get; private set; } = string.Empty;

    private void OnDialogLoaded(object sender, RoutedEventArgs e)
    {
        inputTextBox.Focus();
        inputTextBox.SelectAll();
    }

    private void OnTitleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnInputTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_exactDigitCount > 0)
        {
            string currentText = inputTextBox.Text;
            string digits = new string(currentText.Where(IsAsciiDigit)
                .Take(_exactDigitCount).ToArray());
            if (digits != currentText)
            {
                int caretIndex = inputTextBox.CaretIndex;
                int precedingDigits = currentText.Take(caretIndex).Count(IsAsciiDigit);
                inputTextBox.Text = digits;
                inputTextBox.CaretIndex = System.Math.Min(precedingDigits, digits.Length);
                return;
            }
        }

        UpdateConfirmButtonState();
    }

    private void UpdateConfirmButtonState()
    {
        string text = inputTextBox.Text;
        bool hasRequiredText = !_requireText || !string.IsNullOrWhiteSpace(text);
        bool hasExactDigits = _exactDigitCount == 0
            || (text.Length == _exactDigitCount && text.All(IsAsciiDigit));
        confirmButton.IsEnabled = !_requestPending && hasRequiredText && hasExactDigits;
    }

    private static bool IsAsciiDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        if (!confirmButton.IsEnabled)
        {
            return;
        }

        EnteredText = inputTextBox.Text;
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
