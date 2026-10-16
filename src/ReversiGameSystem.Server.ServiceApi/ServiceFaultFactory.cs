using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.ServiceApi;

internal static class ServiceFaultFactory
{
    private const string FaultReason = "The server could not complete the operation.";

    internal static FaultException<ServiceFault> Create(
        string operationName,
        string correlationId)
    {
        string failureCode = operationName == nameof(AccountAccessService.LoginAsync)
            ? "ACCOUNT_LOGIN_FAILURE"
            : "ACCOUNT_REGISTRATION_FAILURE";
        ServiceFault detail = new ServiceFault
        {
            FailureCode = failureCode,
            CorrelationId = correlationId,
            IsTransient = false
        };
        return new FaultException<ServiceFault>(detail, FaultReason);
    }
}
