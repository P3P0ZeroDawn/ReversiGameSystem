using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

internal static class ServiceFaultFactory
{
    private const string FaultReason = "The server could not complete the operation.";

    internal static FaultException<ServiceFault> Create(
        string operationName,
        string correlationId)
    {
        string failureCode = operationName switch
        {
            nameof(IAccountAccessService.RegisterAccountAsync) => "ACCOUNT_REGISTRATION_FAILURE",
            nameof(ILeaderboardService.GetLeaderboard) => "LEADERBOARD_OPERATION_FAILURE",
            nameof(IPlayerProfileService.GetPlayerProfile)
                or nameof(IPlayerProfileService.GetProfileImage)
                or nameof(IPlayerProfileService.ChangeUsername)
                or nameof(IPlayerProfileService.UpdateProfileImage) => "PROFILE_OPERATION_FAILURE",
            _ => "SESSION_OPERATION_FAILURE"
        };
        ServiceFault detail = new ServiceFault
        {
            FailureCode = failureCode,
            CorrelationId = correlationId,
            IsTransient = false
        };
        return new FaultException<ServiceFault>(detail, FaultReason);
    }
}
