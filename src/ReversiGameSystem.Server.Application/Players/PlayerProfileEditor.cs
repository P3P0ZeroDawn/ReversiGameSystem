using System;
using ReversiGameSystem.Domain.Accounts;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Players;

/// <summary>Authorizes and commits changes exclusively to the caller's own profile.</summary>
public sealed class PlayerProfileEditor
{
    private readonly IAuthenticatedSessionStore _sessions;
    private readonly IPlayerProfileEditorStore _profiles;
    private readonly IPlayerImageRepository _images;

    /// <summary>Initializes the authenticated profile editor.</summary>
    /// <param name="sessions">The shared authenticated session store.</param>
    /// <param name="profiles">The public profile mutation store.</param>
    /// <param name="images">The server-owned image repository.</param>
    /// <exception cref="ArgumentNullException">A required dependency is absent.</exception>
    public PlayerProfileEditor(IAuthenticatedSessionStore sessions,
        IPlayerProfileEditorStore profiles, IPlayerImageRepository images)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _images = images ?? throw new ArgumentNullException(nameof(images));
    }

    /// <summary>Normalizes and saves an available public username.</summary>
    /// <param name="sessionToken">The caller's authenticated session token.</param>
    /// <param name="username">The proposed public username.</param>
    /// <exception cref="ProfileRejectedException">The caller or username is rejected.</exception>
    public void ChangeUsername(string sessionToken, string username)
    {
        long accountId = RequireActiveCaller(sessionToken);
        string normalized = username?.Trim() ?? string.Empty;
        if (UsernamePolicy.Validate(normalized) != UsernameValidationError.None)
        {
            throw new ProfileRejectedException(ProfileRejection.InvalidUsername);
        }

        _profiles.ChangeUsername(accountId, normalized);
    }

    /// <summary>Stores and associates a validated image before confirming success.</summary>
    /// <param name="sessionToken">The caller's authenticated session token.</param>
    /// <param name="image">The proposed image content and media type.</param>
    /// <exception cref="ProfileRejectedException">The caller or image is rejected.</exception>
    /// <exception cref="AggregateException">Association and cleanup both failed.</exception>
    /// <exception cref="System.IO.IOException">The image cannot be stored or removed.</exception>
    public void UpdateImage(string sessionToken, ProfileImageData? image)
    {
        long accountId = RequireActiveCaller(sessionToken);
        if (image == null)
        {
            throw new ProfileRejectedException(ProfileRejection.InvalidImage);
        }

        string reference = _images.Save(image);
        try
        {
            _profiles.SetProfileImage(accountId, reference);
        }
        catch (Exception failure)
        {
            // The failed association leaves this new file unreferenced.
            try
            {
                _images.Delete(reference);
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException("The image association and cleanup failed.",
                    failure, cleanupFailure);
            }

            throw;
        }
    }

    private long RequireActiveCaller(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            throw new ProfileRejectedException(ProfileRejection.MissingSessionToken);
        }

        if (!_sessions.TryGet(sessionToken, out AuthenticatedSessionRecord? session)
            || session == null)
        {
            throw new ProfileRejectedException(ProfileRejection.InvalidSession);
        }
        if (!_profiles.IsActive(session.AccountId))
        {
            throw new ProfileRejectedException(ProfileRejection.InactiveAccount);
        }

        return session.AccountId;
    }
}
