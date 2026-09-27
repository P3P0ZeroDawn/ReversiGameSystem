using System;
using System.Linq;
using System.Threading.Tasks;
using ReversiGameSystem.Contracts.Leaderboards;
using ReversiGameSystem.Contracts.Players;
using ReversiGameSystem.Contracts.Services;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Application.Players;

namespace ReversiGameSystem.LocalRuntime.Services;

/// <summary>
/// Represents local player service.
/// </summary>
public sealed class LocalPlayerService : IPlayerService
{
    private readonly IGetPlayerProfileHandler _getPlayerProfileHandler;
    private readonly IGetLeaderboardHandler _getLeaderboardHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalPlayerService"/> class.
    /// </summary>
    /// <param name="getPlayerProfileHandler">The get player profile handler.</param>
    /// <param name="getLeaderboardHandler">The get leaderboard handler.</param>
    public LocalPlayerService(
        IGetPlayerProfileHandler getPlayerProfileHandler,
        IGetLeaderboardHandler getLeaderboardHandler)
    {
        _getPlayerProfileHandler = getPlayerProfileHandler
            ?? throw new ArgumentNullException(nameof(getPlayerProfileHandler));
        _getLeaderboardHandler = getLeaderboardHandler
            ?? throw new ArgumentNullException(nameof(getLeaderboardHandler));
    }

    /// <summary>
    /// Performs the get player profile async operation.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<PlayerProfileResponse> GetPlayerProfileAsync(PlayerProfileRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        PlayerProfileResult result = await _getPlayerProfileHandler.ExecuteAsync(
            new PlayerProfileQuery(request.SessionToken, request.AccountId));

        PlayerProfileResponse response = new PlayerProfileResponse
        {
            ResultCode = Map(result.Outcome)
        };
        if (result.Profile != null)
        {
            response.Profile = Map(result.Profile);
        }

        return response;
    }

    /// <summary>
    /// Performs the get leaderboard async operation.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<LeaderboardResponse> GetLeaderboardAsync(LeaderboardRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        LeaderboardResult result = await _getLeaderboardHandler.ExecuteAsync(
            new LeaderboardQuery(request.MaximumEntries));

        return new LeaderboardResponse
        {
            ResultCode = Map(result.Outcome),
            Entries = result.Entries.Select(Map).ToArray()
        };
    }

    private static PlayerProfileResultCode Map(PlayerProfileOutcome outcome)
    {
        return outcome switch
        {
            PlayerProfileOutcome.Success => PlayerProfileResultCode.Success,
            PlayerProfileOutcome.InvalidRequest => PlayerProfileResultCode.InvalidRequest,
            PlayerProfileOutcome.InvalidSession => PlayerProfileResultCode.InvalidSession,
            PlayerProfileOutcome.AccountNotFound => PlayerProfileResultCode.AccountNotFound,
            PlayerProfileOutcome.ServiceUnavailable => PlayerProfileResultCode.ServiceUnavailable,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        };
    }

    private static LeaderboardResultCode Map(LeaderboardOutcome outcome)
    {
        return outcome switch
        {
            LeaderboardOutcome.Success => LeaderboardResultCode.Success,
            LeaderboardOutcome.InvalidRequest => LeaderboardResultCode.InvalidRequest,
            LeaderboardOutcome.ServiceUnavailable => LeaderboardResultCode.ServiceUnavailable,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        };
    }

    private static PlayerProfileDto Map(PlayerProfile profile)
    {
        return new PlayerProfileDto
        {
            AccountId = profile.AccountId,
            Username = profile.Username,
            ProfileImageUri = profile.ProfileImageUri,
            Statistics = new PlayerStatisticsDto
            {
                MatchesPlayed = profile.Statistics.MatchesPlayed,
                Wins = profile.Statistics.Wins,
                Losses = profile.Statistics.Losses,
                Draws = profile.Statistics.Draws,
                WinRate = profile.Statistics.WinRate
            }
        };
    }

    private static LeaderboardEntryDto Map(LeaderboardEntry entry)
    {
        return new LeaderboardEntryDto
        {
            Rank = entry.Rank,
            AccountId = entry.AccountId,
            Username = entry.Username,
            ProfileImageUri = entry.ProfileImageUri,
            MatchesPlayed = entry.MatchesPlayed,
            Wins = entry.Wins,
            WinRate = entry.WinRate
        };
    }
}
