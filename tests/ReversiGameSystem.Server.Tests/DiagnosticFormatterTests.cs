using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using ReversiGameSystem.Server.Host;

namespace ReversiGameSystem.Server.Tests;

/// <summary>Verifies original exception delivery and private diagnostic rendering.</summary>
internal static class DiagnosticFormatterTests
{
    internal static void Run()
    {
        LogError_OriginalProviderFailure_RendersSafeDiagnostics();
        LogInformation_StructuredScope_PreservesOperationalContext();
        ConsoleProvider_OriginalFailure_WritesOnlySafeDiagnostics();
    }

    private static void LogError_OriginalProviderFailure_RendersSafeDiagnostics()
    {
        // Arrange
        Exception failure = CaptureFailure();
        failure.Data["Credential"] = "private-credential";
        FormatterLogger logger = new FormatterLogger();

        // Act
        logger.LogError(failure, "The {Operation} operation failed. CorrelationId: {CorrelationId}",
            "LoadProfile", "diagnostic-correlation");
        string output = logger.Output.ToString();

        // Assert
        TestAssert.IsTrue(ReferenceEquals(failure, logger.Exception),
            "The logging provider must receive the original exception object.");
        TestAssert.IsTrue(output.Contains("Error: DiagnosticTests[0]"),
            "The severity and category must remain available.");
        TestAssert.IsTrue(output.Contains("LoadProfile") && output.Contains("diagnostic-correlation"),
            "The operation and correlation must remain available.");
        TestAssert.IsTrue(output.Contains(typeof(InvalidOperationException).FullName)
            && output.Contains(typeof(TimeoutException).FullName),
            "The exception chain types must remain available.");
        TestAssert.IsTrue(output.Contains("HResult=" + failure.HResult)
            && output.Contains(nameof(CaptureFailure)),
            "The original status and stack location must remain available.");
        TestAssert.IsTrue(!output.Contains("private-"),
            "Provider messages and exception data must never reach console output.");
    }

    private static void LogInformation_StructuredScope_PreservesOperationalContext()
    {
        // Arrange
        FormatterLogger logger = new FormatterLogger();

        // Act
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = "scope-correlation"
        }))
        {
            logger.LogInformation("The server is listening.");
        }
        string output = logger.Output.ToString();

        // Assert
        TestAssert.IsTrue(output.Contains("Information: DiagnosticTests[0]"),
            "Normal information entries must retain their level and category.");
        TestAssert.IsTrue(output.Contains("CorrelationId=scope-correlation")
            && output.Contains("The server is listening."),
            "Scope context and normal messages must remain available.");
        TestAssert.AreEqual<Exception?>(null, logger.Exception);
    }

    private static Exception CaptureFailure()
    {
        try
        {
            throw new InvalidOperationException("private-provider-message",
                new TimeoutException("private-inner-message"));
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
    }

    private static void ConsoleProvider_OriginalFailure_WritesOnlySafeDiagnostics()
    {
        // Arrange
        Exception failure = CaptureFailure();
        failure.Data["Credential"] = "private-credential";
        TextWriter originalOutput = Console.Out;
        StringWriter output = new StringWriter();

        // Act
        try
        {
            Console.SetOut(output);
            using (ILoggerFactory factory = LoggerFactory.Create(builder => builder
                .SetMinimumLevel(LogLevel.Information)
                .AddConsole(options => options.FormatterName = SafeConsoleFormatter.FormatterName)
                .AddConsoleFormatter<SafeConsoleFormatter, ConsoleFormatterOptions>()))
            {
                factory.CreateLogger("ProviderIntegration").LogError(failure,
                    "The provider operation failed. CorrelationId: {CorrelationId}", "provider-correlation");
            }
        }
        finally
        {
            Console.SetOut(originalOutput);
        }

        // Assert
        string rendered = output.ToString();
        TestAssert.IsTrue(rendered.Contains("Error: ProviderIntegration[0]")
            && rendered.Contains("provider-correlation"),
            "The configured console provider must select the safe formatter and flush output.");
        TestAssert.IsTrue(rendered.Contains(typeof(InvalidOperationException).FullName)
            && rendered.Contains(nameof(CaptureFailure)),
            "The real console provider must retain safe exception diagnostics.");
        TestAssert.IsTrue(!rendered.Contains("private-"),
            "The real console provider must suppress private provider messages and data.");
    }

    private sealed class FormatterLogger : ILogger
    {
        private readonly SafeConsoleFormatter _formatter = new SafeConsoleFormatter();
        private readonly LoggerExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

        internal StringWriter Output { get; } = new StringWriter();
        internal Exception? Exception { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _scopes.Push(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Exception = exception;
            LogEntry<TState> entry = new LogEntry<TState>(logLevel, "DiagnosticTests",
                eventId, state, exception, formatter);
            _formatter.Write(in entry, _scopes, Output);
        }
    }
}
