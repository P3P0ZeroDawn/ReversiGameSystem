using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.ServiceApi;

internal sealed class MatchBoundary
{
    private readonly ILogger _logger;

    internal MatchBoundary(ILogger logger) => _logger = logger;

    internal T Execute<T>(string operation, Func<T> action)
    {
        try
        {
            return action();
        }
        catch (SessionRejectedException exception)
        {
            throw CreateSessionFault(exception);
        }
        catch (MatchRejectedException exception)
        {
            throw CreateMatchFault(exception);
        }
        catch (FaultException)
        {
            throw;
        }
        catch (Exception exception)
        {
            Log(operation, exception);
            throw CreateServiceFault(operation);
        }
    }

    internal void Execute(string operation, Action action) => Execute(operation, () =>
    {
        action();
        return true;
    });

    private static FaultException<SessionFault> CreateSessionFault(SessionRejectedException exception) =>
        SessionFaultFactory.Create((SessionFailure)Enum.Parse(
            typeof(SessionFailure), exception.Reason.ToString()));

    private static FaultException<MatchFault> CreateMatchFault(MatchRejectedException exception) =>
        new FaultException<MatchFault>(new MatchFault
        {
            Reason = (MatchFailure)Enum.Parse(typeof(MatchFailure), exception.Reason.ToString())
        }, "The match request was rejected.");

    internal void Log(string operation, Exception exception) =>
        _logger.LogError(exception,
            "The {Operation} match operation failed. CorrelationId: {CorrelationId}",
            operation, CorrelationContext.GetOrCreate());

    internal static FaultException<ServiceFault> Unsupported(string operation) =>
        CreateServiceFault(operation);

    private static FaultException<ServiceFault> CreateServiceFault(string operation) =>
        ServiceFaultFactory.Create(operation, CorrelationContext.GetOrCreate());
}
