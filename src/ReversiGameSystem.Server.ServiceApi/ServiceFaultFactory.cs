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
            nameof(IMatchLobbyService.OpenLobbyChannel)
                or nameof(IMatchLobbyService.CreateMatch)
                or nameof(IMatchLobbyService.GetAvailableMatches)
                or nameof(IMatchLobbyService.JoinMatch)
                or nameof(IMatchLobbyService.ChangeTurnDuration)
                or nameof(IMatchLobbyService.StartSystemMatch)
                or nameof(IMatchLobbyService.CancelMatch) => "MATCH_LOBBY_OPERATION_FAILURE",
            nameof(IGameplayMatchService.OpenMatchChannel)
                or nameof(IGameplayMatchService.GetMatchInformation)
                or nameof(IGameplayMatchService.GetMatchState)
                or nameof(IGameplayMatchService.PlacePiece)
                or nameof(IGameplayMatchService.ForfeitMatch)
                or nameof(IGameplayMatchService.ObserveMatch)
                or nameof(IGameplayMatchService.GetMatchHistory)
                or nameof(IGameplayMatchService.GetMatchDetails)
                or nameof(IGameplayMatchService.GetMoveHistory) => "GAMEPLAY_OPERATION_FAILURE",
            nameof(IMatchChatService.OpenChatChannel)
                or nameof(IMatchChatService.SendChatMessage)
                or nameof(IMatchChatService.GetChatHistory) => "MATCH_CHAT_OPERATION_FAILURE",
            nameof(IFriendshipService.OpenFriendshipChannel)
                or nameof(IFriendshipService.GetFriends)
                or nameof(IFriendshipService.FindPlayers)
                or nameof(IFriendshipService.SendFriendRequest)
                or nameof(IFriendshipService.GetPendingFriendRequests)
                or nameof(IFriendshipService.RespondToFriendRequest)
                or nameof(IFriendshipService.RemoveFriend) => "FRIENDSHIP_OPERATION_FAILURE",
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
