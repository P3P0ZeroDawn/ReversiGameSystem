using System;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Players;

public sealed class GetPlayerProfileHandler : IGetPlayerProfileHandler
{
    private readonly IAuthenticatedSessionStore sessionStore;
    private readonly IPlayerProfileStore profileStore;

    public GetPlayerProfileHandler(
        IAuthenticatedSessionStore sessionStore,
        IPlayerProfileStore profileStore)
    {
        this.sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        this.profileStore = profileStore ?? throw new ArgumentNullException(nameof(profileStore));
    }

    public async Task<PlayerProfileResult> ExecuteAsync(PlayerProfileQuery query)
    {
        if (query == null
            || string.IsNullOrWhiteSpace(query.SessionToken)
            || query.AccountId <= 0)
        {
            return new PlayerProfileResult(PlayerProfileOutcome.InvalidRequest);
        }

        AuthenticatedSessionRecord? session;
        try
        {
            session = await sessionStore.FindAsync(query.SessionToken);
        }
        catch (Exception)
        {
            return new PlayerProfileResult(PlayerProfileOutcome.ServiceUnavailable);
        }

        if (session == null)
        {
            return new PlayerProfileResult(PlayerProfileOutcome.InvalidSession);
        }

        long accountId = query.AccountId ?? session.AccountId;
        PlayerProfileStoreResult storeResult;
        try
        {
            storeResult = await profileStore.GetAsync(accountId);
        }
        catch (Exception)
        {
            return new PlayerProfileResult(PlayerProfileOutcome.ServiceUnavailable);
        }

        if (storeResult.Outcome == PlayerProfileStoreOutcome.Unavailable)
        {
            return new PlayerProfileResult(PlayerProfileOutcome.ServiceUnavailable);
        }

        if (storeResult.Outcome == PlayerProfileStoreOutcome.NotFound
            || storeResult.Profile == null)
        {
            return new PlayerProfileResult(PlayerProfileOutcome.AccountNotFound);
        }

        PlayerProfileData profile = storeResult.Profile;
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
