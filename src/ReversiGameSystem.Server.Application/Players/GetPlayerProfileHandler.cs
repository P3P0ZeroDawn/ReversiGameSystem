using System;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Players;

/// <summary>
/// Represents get player profile handler.
/// </summary>
public sealed class GetPlayerProfileHandler : IGetPlayerProfileHandler
{
    private readonly IAuthenticatedSessionStore _sessionStore;
    private readonly IPlayerProfileStore _profileStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPlayerProfileHandler"/> class.
    /// </summary>
    /// <param name="sessionStore">The session store.</param>
    /// <param name="profileStore">The profile store.</param>
    public GetPlayerProfileHandler(
        IAuthenticatedSessionStore sessionStore,
        IPlayerProfileStore profileStore)
    {
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _profileStore = profileStore ?? throw new ArgumentNullException(nameof(profileStore));
    }

    /// <summary>
    /// Performs the execute async operation.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<PlayerProfileResult> ExecuteAsync(PlayerProfileQuery query)
    {
        if (query == null
            || string.IsNullOrWhiteSpace(query.SessionToken)
            || query.AccountId <= 0)
        {
            return new PlayerProfileResult(PlayerProfileOutcome.InvalidRequest);
        }

        if (!_sessionStore.TryGet(query.SessionToken, out AuthenticatedSessionRecord? session)
            || session == null)
        {
            return new PlayerProfileResult(PlayerProfileOutcome.InvalidSession);
        }

        long accountId = query.AccountId ?? session.AccountId;
        PlayerProfileStoreResult storeResult = await _profileStore.GetAsync(accountId);
        if (storeResult.Outcome == PlayerProfileStoreOutcome.Unavailable)
        {
            return new PlayerProfileResult(PlayerProfileOutcome.ServiceUnavailable);
        }

        if (storeResult.Outcome == PlayerProfileStoreOutcome.NotFound
            || storeResult.Profile == null)
        {
            return new PlayerProfileResult(PlayerProfileOutcome.AccountNotFound);
        }

        return CreateSuccessResult(storeResult.Profile);
    }

    private static PlayerProfileResult CreateSuccessResult(PlayerProfileData profile)
    {
        int matchesPlayed = profile.Wins + profile.Losses + profile.Draws;
        double winRate = matchesPlayed == 0
            ? 0d
            : (double)profile.Wins / matchesPlayed;

        return new PlayerProfileResult(
            PlayerProfileOutcome.Success,
            new PlayerProfile(
                profile.AccountId,
                profile.Username,
                profile.ProfileImageUri,
                new PlayerStatistics(
                    matchesPlayed,
                    profile.Wins,
                    profile.Losses,
                    profile.Draws,
                    winRate)));
    }
}
