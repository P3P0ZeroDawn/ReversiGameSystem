using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace ReversiGameSystem.Services;

internal sealed class CallbackErrorBehavior : IEndpointBehavior, IErrorHandler
{
    private const string CallbackFailureMessage = "An unexpected client callback failure occurred.";
    internal static event Action<Exception>? FailureReported;
    internal static event Action<Exception>? ConnectionFailureReported;

    public void ApplyClientBehavior(ServiceEndpoint endpoint,
        System.ServiceModel.Dispatcher.ClientRuntime clientRuntime)
    {
        clientRuntime.CallbackDispatchRuntime.ChannelDispatcher.ErrorHandlers.Add(this);
    }

    public void AddBindingParameters(ServiceEndpoint endpoint,
        BindingParameterCollection parameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }
    }

    public void ApplyDispatchBehavior(ServiceEndpoint endpoint,
        EndpointDispatcher endpointDispatcher)
    {
        throw new NotSupportedException("This behavior is only for client callbacks.");
    }

    public void Validate(ServiceEndpoint endpoint)
    {
        if (endpoint.Contract.CallbackContractType == null)
        {
            throw new ArgumentException("The endpoint requires a callback contract.",
                nameof(endpoint));
        }
    }

    public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
    {
        if (RemoteCall.IsExpectedFailure(error))
        {
            return;
        }
        FaultException failure = new FaultException(CallbackFailureMessage);
        fault = Message.CreateMessage(version, failure.CreateMessageFault(), null);
    }

    public bool HandleError(Exception error)
    {
        try
        {
            if (RemoteCall.IsExpectedFailure(error))
            {
                ConnectionFailureReported?.Invoke(error);
            }
            else
            {
                FailureReported?.Invoke(error);
            }
        }
        catch (Exception dispatchFailure)
        {
            Console.Error.WriteLine(FailureDiagnostics.ForLogging(error).Message);
            Console.Error.WriteLine(FailureDiagnostics.ForLogging(dispatchFailure).Message);
        }
        return false;
    }
}
