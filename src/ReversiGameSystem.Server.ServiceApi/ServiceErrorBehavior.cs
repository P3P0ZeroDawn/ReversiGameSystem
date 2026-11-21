using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>
/// Converts unexpected WCF operation failures into controlled service faults.
/// </summary>
public sealed class ServiceErrorBehavior : IServiceBehavior, IErrorHandler
{
    private const string UnknownOperation = "UnknownOperation";
    private const string OperationDataKey = "ReversiGameSystem.Operation";
    private const string CorrelationDataKey = "ReversiGameSystem.CorrelationId";
    private const string LoggedDataKey = "ReversiGameSystem.ErrorLogged";
    private readonly ILogger<ServiceErrorBehavior> _logger;

    /// <summary>Initializes the centralized WCF error behavior.</summary>
    /// <param name="logger">The server-side diagnostic logger.</param>
    /// <exception cref="ArgumentNullException">The logger is absent.</exception>
    public ServiceErrorBehavior(ILogger<ServiceErrorBehavior> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void AddBindingParameters(
        ServiceDescription serviceDescription,
        ServiceHostBase serviceHostBase,
        Collection<ServiceEndpoint> endpoints,
        BindingParameterCollection bindingParameters)
    {
    }

    /// <inheritdoc />
    public void ApplyDispatchBehavior(
        ServiceDescription serviceDescription,
        ServiceHostBase serviceHostBase)
    {
        foreach (ChannelDispatcherBase dispatcherBase in serviceHostBase.ChannelDispatchers)
        {
            if (dispatcherBase is ChannelDispatcher dispatcher)
            {
                dispatcher.ErrorHandlers.Add(this);
            }
        }
    }

    /// <inheritdoc />
    public void Validate(
        ServiceDescription serviceDescription,
        ServiceHostBase serviceHostBase)
    {
    }

    /// <inheritdoc />
    public bool HandleError(Exception error)
    {
        if (IsDeclaredFault(error))
        {
            return true;
        }
        if (error.Data[LoggedDataKey] is bool alreadyLogged && alreadyLogged)
        {
            return true;
        }

        string? storedOperation = error.Data[OperationDataKey] as string;
        if (storedOperation == null
            && (error is CommunicationException
                || error is TimeoutException
                || error is ObjectDisposedException))
        {
            return true;
        }

        string operation = storedOperation ?? UnknownOperation;
        string correlationId = error.Data[CorrelationDataKey] as string
            ?? CorrelationContext.ReadOrGenerate();

        SetDiagnosticValueIfWritable(error, LoggedDataKey, true);
        LogUnexpectedFailure(error, operation, correlationId);
        if (IsFatalException(error))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public void ProvideFault(
        Exception error,
        MessageVersion version,
        ref Message fault)
    {
        if (IsDeclaredFault(error))
        {
            return;
        }

        string operation = GetOperationName();
        string correlationId = CorrelationContext.GetOrCreate();
        SetDiagnosticValueIfWritable(error, OperationDataKey, operation);
        SetDiagnosticValueIfWritable(error, CorrelationDataKey, correlationId);
        FaultException<ServiceFault> serviceFault =
            ServiceFaultFactory.CreateUnexpectedFault(correlationId);
        MessageFault messageFault = serviceFault.CreateMessageFault();
        fault = Message.CreateMessage(version, messageFault, serviceFault.Action);
    }

    private void LogUnexpectedFailure(
        Exception error,
        string operation,
        string correlationId)
    {
        try
        {
            _logger.LogError(
                error,
                "The {Operation} operation failed unexpectedly. "
                + "CorrelationId: {CorrelationId}",
                operation,
                correlationId);
        }
        catch (Exception loggingFailure)
        {
            string context =
                "The service diagnostic logger failed. Operation: "
                + operation
                + ". CorrelationId: "
                + correlationId
                + ".";
            FailureDiagnostics.WriteLoggerFallback(context, error, loggingFailure);
        }
    }

    private static string GetOperationName()
    {
        OperationContext? context = OperationContext.Current;
        string? action = context?.IncomingMessageHeaders?.Action;
        if (action == null || string.IsNullOrWhiteSpace(action))
        {
            return UnknownOperation;
        }

        int separatorIndex = action.LastIndexOf('/');
        if (separatorIndex < 0 || separatorIndex == action.Length - 1)
        {
            return UnknownOperation;
        }

        return action.Substring(separatorIndex + 1);
    }

    private static bool IsDeclaredFault(Exception error)
    {
        Type errorType = error.GetType();
        if (!errorType.IsGenericType
            || errorType.GetGenericTypeDefinition() != typeof(FaultException<>))
        {
            return false;
        }

        Type detailType = errorType.GetGenericArguments()[0];
        return detailType.Assembly == typeof(ServiceFault).Assembly;
    }

    private static void SetDiagnosticValueIfWritable(Exception error, string key, object value)
    {
        if (!error.Data.IsReadOnly)
        {
            error.Data[key] = value;
        }
    }

    private static bool IsFatalException(Exception error)
    {
        return error is OutOfMemoryException
            or StackOverflowException
            or AccessViolationException;
    }
}
