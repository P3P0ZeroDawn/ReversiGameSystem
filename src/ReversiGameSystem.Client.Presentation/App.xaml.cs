using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Services;

namespace ReversiGameSystem;

public partial class App : Application
{
    private const string DispatcherOperation = "UnhandledDispatcherOperation";
    private const string BackgroundOperation = "UnobservedBackgroundTask";
    private const string CallbackConnectionOperation = "CallbackConnectionFailure";
    private const string CallbackOperation = "UnexpectedClientCallback";
    private const string ProcessOperation = "UnhandledClientProcess";
    private const int FailureExitCode = 1;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        CallbackErrorBehavior.FailureReported += OnCallbackFailure;
        CallbackErrorBehavior.ConnectionFailureReported += OnCallbackConnectionFailure;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        base.OnStartup(e);
    }

    private void OnDispatcherUnhandledException(object sender,
        DispatcherUnhandledExceptionEventArgs arguments)
    {
        arguments.Handled = true;
        bool canContinue = false;
        try
        {
            canContinue = ClientRuntime.Errors.HandleUnhandledFailure(
                arguments.Exception, DispatcherOperation);
        }
        finally
        {
            if (!canContinue)
            {
                Shutdown(FailureExitCode);
            }
        }
    }

    private static void OnCallbackConnectionFailure(Exception exception)
    {
        ClientRuntime.Errors.RecordFailure(exception, CallbackConnectionOperation, LogLevel.Warning);
    }

    private void OnCallbackFailure(Exception exception)
    {
        ReportBackgroundFailure(exception, CallbackOperation);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs arguments)
    {
        arguments.SetObserved();
        ReportBackgroundFailure(arguments.Exception, BackgroundOperation);
    }

    private void ReportBackgroundFailure(Exception exception, string operation)
    {
        void ReportFailure()
        {
            try
            {
                ClientRuntime.Errors.ReportFatalFailure(exception, operation);
            }
            finally
            {
                Shutdown(FailureExitCode);
            }
        }
        Dispatcher.BeginInvoke(new Action(ReportFailure));
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs arguments)
    {
        if (arguments.ExceptionObject is Exception exception)
        {
            ClientRuntime.Errors.RecordFailure(exception, ProcessOperation, LogLevel.Critical);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        CallbackErrorBehavior.FailureReported -= OnCallbackFailure;
        CallbackErrorBehavior.ConnectionFailureReported -= OnCallbackConnectionFailure;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        ClientRuntime.Dispose();
        base.OnExit(e);
    }
}
