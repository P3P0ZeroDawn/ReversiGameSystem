using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using ReversiGameSystem.Views;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CodeEntryDialogTests
{
    [STATestMethod]
    public void ShowDuringRequest_PendingCode_DisablesConfirmationUntilAccepted()
    {
        TextInputDialogWindow dialog = CreateDialog();
        TaskCompletionSource<bool> request = new TaskCompletionSource<bool>();
        bool blockedWhilePending = false;
        bool enabledAfterAcceptance = false;
        dialog.Loaded += OnLoaded;

        bool? confirmed = dialog.ShowDuringRequest(request.Task, true).GetAwaiter().GetResult();

        Assert.IsTrue(blockedWhilePending);
        Assert.IsTrue(enabledAfterAcceptance);
        Assert.AreEqual(true, confirmed);
        Assert.AreEqual("123456", dialog.EnteredText);

        void OnLoaded(object sender, RoutedEventArgs args)
        {
            TextBox input = (TextBox)dialog.FindName("inputTextBox");
            Button confirm = (Button)dialog.FindName("confirmButton");
            input.Text = "123456";
            blockedWhilePending = !confirm.IsEnabled;
            request.SetResult(true);
            dialog.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                new Action(ConfirmCode));
        }

        void ConfirmCode()
        {
            Button confirm = (Button)dialog.FindName("confirmButton");
            enabledAfterAcceptance = confirm.IsEnabled;
            confirm.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
    }

    [STATestMethod]
    public void ShowDuringRequest_RejectedRequest_ClosesWithoutConfirmation()
    {
        TextInputDialogWindow dialog = CreateDialog();
        TaskCompletionSource<bool> request = new TaskCompletionSource<bool>();
        dialog.Loaded += OnLoaded;

        bool? confirmed = dialog.ShowDuringRequest(request.Task, true).GetAwaiter().GetResult();

        Assert.AreEqual(false, confirmed);
        Assert.AreEqual(string.Empty, dialog.EnteredText);

        void OnLoaded(object sender, RoutedEventArgs args)
        {
            request.SetResult(false);
        }
    }

    [STATestMethod]
    public void ShowDuringRequest_ServiceFault_PropagatesOriginalFailure()
    {
        TextInputDialogWindow dialog = CreateDialog();
        TaskCompletionSource<bool> request = new TaskCompletionSource<bool>();
        FaultException failure = new FaultException("Delivery failed.");
        dialog.Loaded += OnLoaded;

        FaultException actual = Assert.ThrowsExactly<FaultException>(() =>
            dialog.ShowDuringRequest(request.Task, true).GetAwaiter().GetResult());

        Assert.AreSame(failure, actual);
        Assert.IsFalse(dialog.IsVisible);

        void OnLoaded(object sender, RoutedEventArgs args)
        {
            request.SetException(failure);
        }
    }

    [STATestMethod]
    public void ShowDuringRequest_CancelledBeforeResponse_DoesNotReopenDialog()
    {
        TextInputDialogWindow dialog = CreateDialog();
        TaskCompletionSource<bool> request = new TaskCompletionSource<bool>();
        dialog.Loaded += OnLoaded;

        Task<bool?> operation = dialog.ShowDuringRequest(request.Task, true);
        if (!operation.IsCompleted)
        {
            DispatcherFrame frame = new DispatcherFrame();
            operation.GetAwaiter().OnCompleted(() => frame.Continue = false);
            Dispatcher.PushFrame(frame);
        }
        bool? confirmed = operation.GetAwaiter().GetResult();

        Assert.AreEqual(false, confirmed);
        Assert.IsFalse(dialog.IsVisible);
        Assert.AreEqual(string.Empty, dialog.EnteredText);

        void OnLoaded(object sender, RoutedEventArgs args)
        {
            dialog.Close();
            request.SetResult(true);
        }
    }

    private static TextInputDialogWindow CreateDialog()
    {
        if (Application.Current == null)
        {
            App application = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            application.InitializeComponent();
        }
        SynchronizationContext.SetSynchronizationContext(
            new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        TextInputDialogOptions options = new TextInputDialogOptions(
            "LoginScreen_VerificationCodeDialogTitle",
            "LoginScreen_VerificationCodeDialogMessage",
            "LoginScreen_VerificationCodeDialogLabel")
        {
            RequireText = true,
            ExactDigitCount = VerificationCodePolicy.Length
        };
        return new TextInputDialogWindow(options, LocalizationManager.Instance);
    }
}
