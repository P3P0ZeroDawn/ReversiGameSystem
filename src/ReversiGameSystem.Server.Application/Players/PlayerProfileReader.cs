using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Players;

/// <summary>Reads public profiles and stored images for active authenticated callers.</summary>
public sealed class PlayerProfileReader
{
    private readonly IAuthenticatedSessionStore _sessions;
    private readonly IPlayerProfileStore _profiles;
    private readonly IPlayerImageRepository _images;

    /// <summary>Initializes the authenticated profile reader.</summary>
    /// <param name="sessions">The shared authenticated session store.</param>
    /// <param name="profiles">The active profile and statistics store.</param>
    /// <param name="images">The server-owned image repository.</param>
    /// <exception cref="ArgumentNullException">A required dependency is absent.</exception>
    public PlayerProfileReader(IAuthenticatedSessionStore sessions,
        IPlayerProfileStore profiles, IPlayerImageRepository images)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _images = images ?? throw new ArgumentNullException(nameof(images));
    }

    /// <summary>Reads the public profile using the existing statistics use case.</summary>
    /// <param name="sessionToken">The caller's authenticated session token.</param>
    /// <param name="targetAccountId">The target account, or null for the caller.</param>
    /// <returns>The public identity, image availability and calculated statistics.</returns>
    /// <exception cref="ProfileRejectedException">The caller or target is rejected.</exception>
    /// <exception cref="InvalidOperationException">The profile store is unavailable.</exception>
    public PlayerProfile GetProfile(string sessionToken, long? targetAccountId)
    {
        RequireActiveCaller(sessionToken);
        if (targetAccountId <= 0)
        {
            throw new ProfileRejectedException(ProfileRejection.InvalidAccountId);
        }

        GetPlayerProfileHandler handler = new GetPlayerProfileHandler(_sessions, _profiles);
        PlayerProfileResult result = handler.ExecuteAsync(
            new PlayerProfileQuery(sessionToken, targetAccountId)).GetAwaiter().GetResult();
        return result.Outcome switch
        {
            PlayerProfileOutcome.Success when result.Profile != null => result.Profile,
            PlayerProfileOutcome.InvalidSession => throw new ProfileRejectedException(
                ProfileRejection.InvalidSession),
            PlayerProfileOutcome.AccountNotFound => throw new ProfileRejectedException(
                ProfileRejection.AccountNotFound),
            _ => throw new InvalidOperationException("The profile store is unavailable.")
        };
    }

    /// <summary>Returns null only when an existing profile has no custom image.</summary>
    /// <param name="sessionToken">The caller's authenticated session token.</param>
    /// <param name="targetAccountId">The target account, or null for the caller.</param>
    /// <returns>The stored image, or null when no custom image is associated.</returns>
    /// <exception cref="ProfileRejectedException">The caller or target is rejected.</exception>
    /// <exception cref="InvalidOperationException">The profile store is unavailable.</exception>
    /// <exception cref="System.IO.IOException">An associated image cannot be read.</exception>
    public ProfileImageData? GetImage(string sessionToken, long? targetAccountId)
    {
        PlayerProfile profile = GetProfile(sessionToken, targetAccountId);
        string? imageReference = profile.ProfileImageUri;
        if (imageReference == null || string.IsNullOrWhiteSpace(imageReference))
        {
            return null;
        }

        return _images.Read(imageReference);
    }

    private void RequireActiveCaller(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            throw new ProfileRejectedException(ProfileRejection.MissingSessionToken);
        }

        AuthenticatedSessionRecord session = _sessions.Find(sessionToken)
            ?? throw new ProfileRejectedException(ProfileRejection.InvalidSession);
        PlayerProfileStoreResult result = _profiles.GetAsync(session.AccountId)
            .GetAwaiter().GetResult();
        if (result.Outcome == PlayerProfileStoreOutcome.Unavailable)
        {
            throw new InvalidOperationException("The profile store is unavailable.");
        }

        if (result.Outcome != PlayerProfileStoreOutcome.Found)
        {
            throw new ProfileRejectedException(ProfileRejection.InactiveAccount);
        }
    }
}
