using System;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Localization;

namespace ReversiGameSystem.Services;

internal sealed class ClientErrorHandler
{
    private const string OperationTitleKey = "Client_OperationFailedTitle";
    private const string OperationMessageKey = "Client_OperationFailedMessage";
    private const string FatalTitleKey = "Client_FatalErrorTitle";
    private const string FatalMessageKey = "Client_FatalErrorMessage";
    private const string DiagnosticsUnavailableMessageKey = "Client_DiagnosticsUnavailableMessage";
    private const string FailureTemplate =
        "The {Operation} client operation failed. CorrelationId: {CorrelationId}. "
        + "ServerCorrelationId: {ServerCorrelationId}";
    private readonly ILogger<ClientErrorHandler> _logger;
    private readonly Action<string, string> _showMessage;
    private readonly EmergencyFailureLog _emergencyLog;
    private readonly object _failureLogLock = new object();

    internal ClientErrorHandler(ILogger<ClientErrorHandler> logger,
        Action<string, string> showMessage)
        : this(logger, showMessage, new EmergencyFailureLog())
    {
    }

    internal ClientErrorHandler(ILogger<ClientErrorHandler> logger,
        Action<string, string> showMessage, EmergencyFailureLog emergencyLog)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
        _emergencyLog = emergencyLog ?? throw new ArgumentNullException(nameof(emergencyLog));
    }

    internal bool DiagnosticsUnavailable { get; private set; }

    internal void ReportOperationFailure(Exception exception, string operation)
    {
        LocalizationManager localization = LocalizationManager.Instance;
        string message;
        lock (_failureLogLock)
        {
            string correlationId = RecordFailure(exception, operation, LogLevel.Error);
            message = string.Format(localization[OperationMessageKey], correlationId);
            if (DiagnosticsUnavailable)
            {
                message += Environment.NewLine + string.Format(
                    localization[DiagnosticsUnavailableMessageKey], correlationId);
            }
        }
        _showMessage(localization[OperationTitleKey], message);
    }

    internal void ReportFatalFailure(Exception exception, string operation)
    {
        LocalizationManager localization = LocalizationManager.Instance;
        string message;
        lock (_failureLogLock)
        {
            string correlationId = RecordFailure(exception, operation, LogLevel.Critical);
            message = string.Format(localization[FatalMessageKey], correlationId);
            if (DiagnosticsUnavailable)
            {
                message += Environment.NewLine + string.Format(
                    localization[DiagnosticsUnavailableMessageKey], correlationId);
            }
        }
        _showMessage(localization[FatalTitleKey], message);
    }

    internal string RecordFailure(Exception exception, string operation, LogLevel level)
    {
        lock (_failureLogLock)
        {
            string correlationId = Guid.NewGuid().ToString("D");
            DiagnosticsUnavailable = false;
            try
            {
                _logger.Log(level, exception, FailureTemplate, operation, correlationId,
                    RemoteCall.ReadServerCorrelation(exception));
            }
            catch (Exception loggingFailure)
            {
                DiagnosticsUnavailable = !_emergencyLog.PersistFailure(
                    exception, loggingFailure, operation, correlationId);
            }
            return correlationId;
        }
    }
}
