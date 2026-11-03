using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Builds the shared wire representation of an expected session rejection.</summary>
internal static class SessionFaultFactory
{
    internal static FaultException<SessionFault> Create(SessionFailure reason)
    {
        return new FaultException<SessionFault>(new SessionFault
        {
            Reason = reason
        }, "The session request was rejected.");
    }
}
