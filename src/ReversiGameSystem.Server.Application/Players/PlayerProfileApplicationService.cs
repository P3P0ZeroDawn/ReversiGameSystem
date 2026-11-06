using System;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Application.Players;

/// <summary>Coordinates the application operations for player profiles.</summary>
public sealed class PlayerProfileApplicationService
{
    private readonly ISessionRepository _sessions;
    private readonly IPlayerProfileRepository _profiles;
    private readonly IPlayerImageRepository _images;

    /// <summary>Initializes the player profile application service.</summary>
    /// <param name="sessions">The authenticated session store.</param>
    /// <param name="profiles">The player profile repository.</param>
    /// <param name="images">The server-owned image repository.</param>
    /// <exception cref="ArgumentNullException">A dependency is absent.</exception>
    public PlayerProfileApplicationService(
        ISessionRepository sessions,
        IPlayerProfileRepository profiles,
        IPlayerImageRepository images)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _images = images ?? throw new ArgumentNullException(nameof(images));
    }

    /// <summary>Gets the caller's profile or another active public profile.</summary>
    /// <param name="sessionToken">The caller's authenticated session token.</param>
    /// <param name="targetAccountId">The target account, or null for the caller.</param>
    /// <returns>The expected profile result.</returns>
    public async Task<GetPlayerProfileResult> GetProfileAsync(
        string sessionToken,
        long? targetAccountId)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return new GetPlayerProfileResult(ProfileOperationStatus.MissingSessionToken);
        }
        if (!_sessions.TryGet(sessionToken, out AuthenticatedSession? session)
            || session == null)
        {
            return new GetPlayerProfileResult(ProfileOperationStatus.InvalidSession);
        }
        if (targetAccountId.HasValue && targetAccountId.Value <= 0)
        {
            return new GetPlayerProfileResult(ProfileOperationStatus.InvalidAccountId);
        }

        long callerAccountId = session.AccountId;
        PlayerProfileLookupResult callerLookup = await _profiles.GetAsync(callerAccountId);
        if (!callerLookup.IsFound)
        {
            return new GetPlayerProfileResult(ProfileOperationStatus.InactiveAccount);
        }
        PlayerProfile callerProfile = callerLookup.GetFoundProfile();
        GetPlayerProfileResult result;
        if (!targetAccountId.HasValue || targetAccountId.Value == callerAccountId)
        {
            result = new GetPlayerProfileResult(
                ProfileOperationStatus.Success,
                callerProfile);
        }
        else
        {
            PlayerProfileLookupResult targetLookup = await _profiles.GetAsync(
                targetAccountId.Value);
            if (!targetLookup.IsFound)
            {
                result = new GetPlayerProfileResult(
                    ProfileOperationStatus.AccountNotFound);
            }
            else
            {
                result = new GetPlayerProfileResult(
                    ProfileOperationStatus.Success,
                    targetLookup.GetFoundProfile());
            }
        }

        return result;
    }

    /// <summary>Gets the custom image associated with an accessible public profile.</summary>
    /// <param name="sessionToken">The caller's authenticated session token.</param>
    /// <param name="targetAccountId">The target account, or null for the caller.</param>
    /// <returns>The expected image result; an absent image is successful.</returns>
    public async Task<GetProfileImageResult> GetImageAsync(
        string sessionToken,
        long? targetAccountId)
    {
        GetPlayerProfileResult profileResult = await GetProfileAsync(
            sessionToken,
            targetAccountId);
        if (profileResult.Status != ProfileOperationStatus.Success)
        {
            return new GetProfileImageResult(profileResult.Status);
        }
        if (profileResult.Profile == null)
        {
            throw new InvalidOperationException(
                "A successful profile result must contain a profile.");
        }

        string? imageReference = profileResult.Profile.ProfileImageUri;
        GetProfileImageResult result;
        if (imageReference == null || string.IsNullOrWhiteSpace(imageReference))
        {
            result = new GetProfileImageResult(ProfileOperationStatus.Success);
        }
        else
        {
            ProfileImageData image = _images.Read(imageReference);
            result = new GetProfileImageResult(ProfileOperationStatus.Success, image);
        }

        return result;
    }

    /// <summary>Normalizes and saves an available username for the caller.</summary>
    /// <param name="sessionToken">The caller's authenticated session token.</param>
    /// <param name="username">The proposed public username.</param>
    /// <returns>The expected modification status.</returns>
    public async Task<ProfileOperationStatus> ChangeUsernameAsync(
        string sessionToken,
        string username)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return ProfileOperationStatus.MissingSessionToken;
        }
        if (!_sessions.TryGet(sessionToken, out AuthenticatedSession? session)
            || session == null)
        {
            return ProfileOperationStatus.InvalidSession;
        }
        long accountId = session.AccountId;
        if (!await _profiles.IsActiveAsync(accountId))
        {
            return ProfileOperationStatus.InactiveAccount;
        }

        string normalized = username == null ? string.Empty : username.Trim();
        if (UsernamePolicy.Validate(normalized) != UsernameValidationError.None)
        {
            return ProfileOperationStatus.InvalidUsername;
        }

        ProfileRepositoryUpdateStatus updateStatus = await _profiles.ChangeUsernameAsync(
            accountId,
            normalized);
        return MapUpdateStatus(updateStatus);
    }

    /// <summary>Stores and associates a validated image with the caller's profile.</summary>
    /// <param name="sessionToken">The caller's authenticated session token.</param>
    /// <param name="image">The proposed image content and media type.</param>
    /// <returns>The expected modification status.</returns>
    /// <exception cref="AggregateException">Association and cleanup both failed.</exception>
    public async Task<ProfileOperationStatus> UpdateImageAsync(
        string sessionToken,
        ProfileImageData? image)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return ProfileOperationStatus.MissingSessionToken;
        }
        if (!_sessions.TryGet(sessionToken, out AuthenticatedSession? session)
            || session == null)
        {
            return ProfileOperationStatus.InvalidSession;
        }
        long accountId = session.AccountId;
        if (!await _profiles.IsActiveAsync(accountId))
        {
            return ProfileOperationStatus.InactiveAccount;
        }
        if (image == null || !_images.TrySave(image, out string imageReference))
        {
            return ProfileOperationStatus.InvalidImage;
        }

        ProfileRepositoryUpdateStatus updateStatus;
        try
        {
            updateStatus = await _profiles.SetProfileImageAsync(accountId, imageReference);
        }
        catch (Exception failure)
        {
            try
            {
                _images.Delete(imageReference);
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(
                    "The image association and cleanup failed.",
                    failure,
                    cleanupFailure);
            }

            throw;
        }

        if (updateStatus != ProfileRepositoryUpdateStatus.Success)
        {
            _images.Delete(imageReference);
        }

        return MapUpdateStatus(updateStatus);
    }

    private static ProfileOperationStatus MapUpdateStatus(
        ProfileRepositoryUpdateStatus status)
    {
        ProfileOperationStatus operationStatus;
        if (status == ProfileRepositoryUpdateStatus.Success)
        {
            operationStatus = ProfileOperationStatus.Success;
        }
        else if (status == ProfileRepositoryUpdateStatus.InactiveAccount)
        {
            operationStatus = ProfileOperationStatus.InactiveAccount;
        }
        else if (status == ProfileRepositoryUpdateStatus.UsernameUnavailable)
        {
            operationStatus = ProfileOperationStatus.UsernameUnavailable;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        return operationStatus;
    }
}
