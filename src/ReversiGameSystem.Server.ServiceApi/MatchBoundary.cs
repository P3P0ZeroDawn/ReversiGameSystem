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
            throw new FaultException<SessionFault>(new SessionFault
            {
                Reason = (SessionFailure)Enum.Parse(typeof(SessionFailure), exception.Reason.ToString())
            }, "The session request was rejected.");
        }
        catch (MatchRejectedException exception)
        {
            throw new FaultException<MatchFault>(new MatchFault
            {
                Reason = (MatchFailure)Enum.Parse(typeof(MatchFailure), exception.Reason.ToString())
            }, "The match request was rejected.");
        }
        catch (FriendshipRejectedException exception)
        {
            throw new FaultException<FriendshipFault>(new FriendshipFault
            {
                Reason = (FriendshipFailure)Enum.Parse(typeof(FriendshipFailure), exception.Reason.ToString())
            }, "The friendship request was rejected.");
        }
        catch (FaultException)
        {
            throw;
        }
        catch (Exception exception)
        {
            Log(operation, exception);
            throw Unsupported(operation);
        }
    }

    internal void Log(string operation, Exception exception) =>
        _logger.LogError(FailureDiagnostics.ForLogging(exception),
            "The {Operation} match operation failed. CorrelationId: {CorrelationId}",
            operation, CorrelationContext.GetOrCreate());

    internal static FaultException<ServiceFault> Unsupported(string operation) =>
        ServiceFaultFactory.Create(operation, CorrelationContext.GetOrCreate());
}
