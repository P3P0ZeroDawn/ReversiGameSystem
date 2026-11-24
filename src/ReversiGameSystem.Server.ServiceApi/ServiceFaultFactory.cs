using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

internal static class ServiceFaultFactory
{
    internal static FaultException<ServiceFault> CreateUnexpectedFault(
        string correlationId)
    {
        string? contractName = OperationContext.Current?.EndpointDispatcher?.ContractName;
        string failureCode = contractName switch
        {
            nameof(IAccountService) => "ACCOUNT_OPERATION_FAILURE",
            nameof(ISessionService) => "SESSION_OPERATION_FAILURE",
            nameof(ILeaderboardService) => "LEADERBOARD_OPERATION_FAILURE",
            nameof(IMatchLobbyService) => "MATCH_LOBBY_OPERATION_FAILURE",
            nameof(IGameplayMatchService) => "GAMEPLAY_OPERATION_FAILURE",
            nameof(IMatchChatService) => "MATCH_CHAT_OPERATION_FAILURE",
            nameof(IFriendshipService) => "FRIENDSHIP_OPERATION_FAILURE",
            nameof(IPlayerProfileService) => "PROFILE_OPERATION_FAILURE",
            _ => "UNEXPECTED_SERVICE_FAILURE"
        };
        ServiceFault detail = new ServiceFault
        {
            FailureCode = failureCode,
            CorrelationId = correlationId,
            IsTransient = false
        };
        return new FaultException<ServiceFault>(
            detail,
            "The server could not complete the operation.");
    }

    internal static FaultException<MatchFault> CreateMatchFault(MatchFailure reason)
    {
        return new FaultException<MatchFault>(new MatchFault
        {
            Reason = reason
        }, "The match request was rejected.");
    }

    internal static FaultException<SessionFault> CreateSessionFault(SessionFailure reason)
    {
        return new FaultException<SessionFault>(new SessionFault
        {
            Reason = reason
        }, "The session request was rejected.");
    }

    internal static FaultException<FriendshipFault> CreateFriendshipFault(FriendshipFailure reason)
    {
        return new FaultException<FriendshipFault>(new FriendshipFault
        {
            Reason = reason
        }, "The friendship request was rejected.");
    }
}
