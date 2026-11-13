using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;

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
            ChannelDispatcher? dispatcher = dispatcherBase as ChannelDispatcher;
            if (dispatcher != null)
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

        string? operation = ReadDiagnosticValue(error, OperationDataKey);
        string? correlationId = ReadDiagnosticValue(error, CorrelationDataKey);
        if (operation == null || correlationId == null)
        {
            return true;
        }

        error.Data[LoggedDataKey] = true;
        LogUnexpectedFailure(error, operation, correlationId);
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
        error.Data[OperationDataKey] = operation;
        error.Data[CorrelationDataKey] = correlationId;
        FaultException<ServiceFault> serviceFault =
            ServiceFaultFactory.Create(operation, correlationId);
        MessageFault messageFault = serviceFault.CreateMessageFault();
        fault = Message.CreateMessage(version, messageFault, serviceFault.Action);
    }

    private static bool IsDeclaredFault(Exception error)
    {
        if (error is FaultException<AccountFault>
            || error is FaultException<ChatFault>
            || error is FaultException<FriendshipFault>
            || error is FaultException<LeaderboardFault>
            || error is FaultException<MatchFault>
            || error is FaultException<PreferencesFault>
            || error is FaultException<ProfileFault>
            || error is FaultException<ServiceFault>
            || error is FaultException<SessionFault>)
        {
            return true;
        }

        return false;
    }

    private void LogUnexpectedFailure(
        Exception error,
        string operation,
        string correlationId)
    {
        try
        {
            _logger.LogError(error,
                "The {Operation} operation failed unexpectedly. "
                + "CorrelationId: {CorrelationId}",
                operation, correlationId);
        }
        catch (Exception loggingFailure)
        {
            Console.Error.WriteLine(
                "The service diagnostic logger failed. Operation: {0}. "
                + "CorrelationId: {1}.{2}{3}{2}{4}",
                operation,
                correlationId,
                Environment.NewLine,
                FailureDiagnostics.Describe(error),
                FailureDiagnostics.Describe(loggingFailure));
        }
    }

    private static string GetOperationName()
    {
        OperationContext? context = OperationContext.Current;
        string? action = context?.IncomingMessageHeaders.Action;
        if (action == null)
        {
            return UnknownOperation;
        }
        if (string.IsNullOrWhiteSpace(action))
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

    private static string? ReadDiagnosticValue(Exception error, string key)
    {
        return error.Data[key] as string;
    }
}
