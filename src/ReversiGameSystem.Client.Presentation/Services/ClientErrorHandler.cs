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
    private const string FailureTemplate =
        "The {Operation} client operation failed. CorrelationId: {CorrelationId}. "
        + "ServerCorrelationId: {ServerCorrelationId}";
    private readonly ILogger<ClientErrorHandler> _logger;
    private readonly Action<string, string> _showMessage;

    internal ClientErrorHandler(ILogger<ClientErrorHandler> logger,
        Action<string, string> showMessage)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
    }

    internal bool HandleUnhandledFailure(Exception exception, string operation)
    {
        bool canContinue = RemoteCall.IsDeclaredFault(exception);
        if (canContinue)
        {
            ReportOperationFailure(exception, operation);
        }
        else
        {
            ReportFatalFailure(exception, operation);
        }
        return canContinue;
    }

    internal void ReportOperationFailure(Exception exception, string operation)
    {
        string correlationId = RecordFailure(exception, operation, LogLevel.Error);
        LocalizationManager localization = LocalizationManager.Instance;
        _showMessage(localization[OperationTitleKey],
            string.Format(localization[OperationMessageKey], correlationId));
    }

    internal void ReportFatalFailure(Exception exception, string operation)
    {
        string correlationId = RecordFailure(exception, operation, LogLevel.Critical);
        LocalizationManager localization = LocalizationManager.Instance;
        _showMessage(localization[FatalTitleKey],
            string.Format(localization[FatalMessageKey], correlationId));
    }

    internal string RecordFailure(Exception exception, string operation, LogLevel level)
    {
        string correlationId = Guid.NewGuid().ToString("D");
        try
        {
            _logger.Log(level, exception, FailureTemplate, operation, correlationId,
                RemoteCall.ReadServerCorrelation(exception));
        }
        catch (Exception loggingFailure)
        {
            Console.Error.WriteLine(FailureDiagnostics.ForLogging(exception).Message);
            Console.Error.WriteLine(FailureDiagnostics.ForLogging(loggingFailure).Message);
        }
        return correlationId;
    }
}
