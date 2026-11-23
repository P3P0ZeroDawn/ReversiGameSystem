using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Application.Players;

public sealed class PlayerProfileApplicationService
{
    private readonly SessionStore _sessions;
    private readonly IPlayerProfileRepository _profiles;
    private readonly IPlayerImageRepository _images;

    public PlayerProfileApplicationService(
        SessionStore sessions,
        IPlayerProfileRepository profiles,
        IPlayerImageRepository images)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _images = images ?? throw new ArgumentNullException(nameof(images));
    }

    public ProfileResult<PlayerProfile> GetProfile(
        string sessionToken,
        long? targetAccountId)
    {
        SessionResult<AuthenticatedSession> sessionResult =
            _sessions.FindAuthenticated(sessionToken);
        if (sessionResult.Status != SessionStatus.Success || sessionResult.Value == null)
        {
            return new ProfileResult<PlayerProfile>(MapSessionStatus(sessionResult.Status));
        }
        if (targetAccountId.HasValue && targetAccountId.Value <= 0)
        {
            return new ProfileResult<PlayerProfile>(ProfileStatus.InvalidAccountId);
        }

        long callerAccountId = sessionResult.Value.AccountId;
        if (!targetAccountId.HasValue || targetAccountId.Value == callerAccountId)
        {
            ProfileResult<PlayerProfile> callerLookup = _profiles.GetProfile(callerAccountId);
            if (callerLookup.Value == null)
            {
                return new ProfileResult<PlayerProfile>(ProfileStatus.InactiveAccount);
            }

            return callerLookup;
        }

        if (!_profiles.IsActive(callerAccountId))
        {
            return new ProfileResult<PlayerProfile>(ProfileStatus.InactiveAccount);
        }

        return _profiles.GetProfile(targetAccountId.Value);
    }

    public GetProfileImageResult GetImage(
        string sessionToken,
        long? targetAccountId)
    {
        SessionResult<AuthenticatedSession> sessionResult =
            _sessions.FindAuthenticated(sessionToken);
        if (sessionResult.Status != SessionStatus.Success || sessionResult.Value == null)
        {
            return new GetProfileImageResult(MapSessionStatus(sessionResult.Status));
        }
        if (targetAccountId.HasValue && targetAccountId.Value <= 0)
        {
            return new GetProfileImageResult(ProfileStatus.InvalidAccountId);
        }

        long callerAccountId = sessionResult.Value.AccountId;
        ProfileResult<string> referenceResult = ResolveProfileImageReference(
            callerAccountId,
            targetAccountId);
        if (referenceResult.Value == null)
        {
            return new GetProfileImageResult(referenceResult.Status);
        }

        if (string.IsNullOrWhiteSpace(referenceResult.Value))
        {
            return new GetProfileImageResult();
        }

        ProfileImageData image = _images.ReadImage(referenceResult.Value);
        return new GetProfileImageResult(image);
    }

    public ProfileStatus ChangeUsername(
        string sessionToken,
        string username)
    {
        SessionResult<AuthenticatedSession> sessionResult =
            _sessions.FindAuthenticated(sessionToken);
        if (sessionResult.Status != SessionStatus.Success || sessionResult.Value == null)
        {
            return MapSessionStatus(sessionResult.Status);
        }

        long accountId = sessionResult.Value.AccountId;
        string normalized = username == null ? string.Empty : username.Trim();
        if (UsernamePolicy.Validate(normalized) != UsernameValidationError.None)
        {
            return ProfileStatus.InvalidUsername;
        }

        return _profiles.ChangeUsername(accountId, normalized);
    }

    /// <summary>Stores and associates a validated image with the caller's profile.</summary>
    /// <param name="sessionToken">The caller's authenticated session token.</param>
    /// <param name="image">The proposed image content and media type.</param>
    /// <returns>The expected modification status.</returns>
    /// <exception cref="AggregateException">Association and cleanup both failed.</exception>
    public ProfileStatus UpdateImage(
        string sessionToken,
        ProfileImageData? image)
    {
        SessionResult<AuthenticatedSession> sessionResult =
            _sessions.FindAuthenticated(sessionToken);
        if (sessionResult.Status != SessionStatus.Success || sessionResult.Value == null)
        {
            return MapSessionStatus(sessionResult.Status);
        }

        long accountId = sessionResult.Value.AccountId;
        if (image == null)
        {
            return ProfileStatus.InvalidImage;
        }

        ProfileResult<string> saveResult = _images.SaveImage(image);
        if (saveResult.Value == null)
        {
            return saveResult.Status;
        }

        return AssociateProfileImage(accountId, saveResult.Value);
    }

    private ProfileStatus AssociateProfileImage(
        long accountId,
        string imageReference)
    {
        ProfileStatus updateStatus;
        try
        {
            updateStatus = _profiles.SetProfileImage(accountId, imageReference);
        }
        catch (Exception failure)
        {
            DeleteImageOnFailure(imageReference, failure);
            throw;
        }

        if (updateStatus != ProfileStatus.Success)
        {
            _images.DeleteImage(imageReference);
        }

        return updateStatus;
    }

    private void DeleteImageOnFailure(string imageReference, Exception originalFailure)
    {
        try
        {
            _images.DeleteImage(imageReference);
        }
        catch (Exception cleanupFailure)
        {
            throw new AggregateException(
                "The profile image cleanup failed after an operation failure.",
                originalFailure,
                cleanupFailure);
        }
    }

    private ProfileResult<string> ResolveProfileImageReference(
        long callerAccountId,
        long? targetAccountId)
    {
        if (!targetAccountId.HasValue || targetAccountId.Value == callerAccountId)
        {
            ProfileResult<string> callerReference =
                _profiles.GetProfileImageReference(callerAccountId);
            if (callerReference.Value == null)
            {
                return new ProfileResult<string>(ProfileStatus.InactiveAccount);
            }

            return callerReference;
        }

        if (!_profiles.IsActive(callerAccountId))
        {
            return new ProfileResult<string>(ProfileStatus.InactiveAccount);
        }

        return _profiles.GetProfileImageReference(targetAccountId.Value);
    }

    private static ProfileStatus MapSessionStatus(SessionStatus status)
    {
        ProfileStatus mapped;
        switch (status)
        {
            case SessionStatus.MissingSessionToken:
                mapped = ProfileStatus.MissingSessionToken;
                break;
            case SessionStatus.InvalidSession:
                mapped = ProfileStatus.InvalidSession;
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    "Unsupported session status.");
        }

        return mapped;
    }
}
