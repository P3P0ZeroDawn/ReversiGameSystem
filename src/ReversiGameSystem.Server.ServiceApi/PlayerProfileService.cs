using System;
using System.ServiceModel;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using ApplicationProfile = ReversiGameSystem.Server.Application.Players.PlayerProfile;
using ContractProfile = ReversiGameSystem.Server.Contracts.Players.PlayerProfile;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Exposes public profile lookup and authenticated profile modifications.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class PlayerProfileService : IPlayerProfileService
{
    private readonly PlayerProfileApplicationService _applicationService;

    /// <summary>Initializes the profile boundary.</summary>
    /// <param name="applicationService">The player profile application service.</param>
    /// <exception cref="ArgumentNullException">A required dependency is absent.</exception>
    public PlayerProfileService(PlayerProfileApplicationService applicationService)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
    }

    /// <summary>
    /// Reads the profile and statistics for the caller or a requested target account.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="targetAccountId">
    /// Optional target account identifier, or <c>null</c> to read the caller's own profile.
    /// </param>
    /// <returns>The requested player profile.</returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ProfileFault}">
    /// A declared profile rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public ContractProfile GetPlayerProfile(string sessionToken, long? targetAccountId)
    {
        ProfileResult<ApplicationProfile> result = _applicationService.GetProfile(
            sessionToken,
            targetAccountId);
        ThrowFaultIfFailed(result.Status);

        return MapProfile(RequireValue(result));
    }

    /// <summary>
    /// Reads the profile image for the caller or a requested target account.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="targetAccountId">
    /// Optional target account identifier, or <c>null</c> to read the caller's own image.
    /// </param>
    /// <returns>The profile image content, or <c>null</c> when none is stored.</returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ProfileFault}">
    /// A declared profile rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public ImageContent? GetProfileImage(string sessionToken, long? targetAccountId)
    {
        GetProfileImageResult result = _applicationService.GetImage(
            sessionToken,
            targetAccountId);
        ThrowFaultIfFailed(result.Status);
        if (result.Image == null)
        {
            return null;
        }

        return new ImageContent
        {
            MediaType = result.Image.MediaType,
            Content = result.Image.Content
        };
    }

    /// <summary>
    /// Validates and updates the authenticated caller's public username.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="username">The proposed new username.</param>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ProfileFault}">
    /// A declared profile rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public void ChangeUsername(string sessionToken, string username)
    {
        ProfileStatus status = _applicationService.ChangeUsername(
            sessionToken,
            username);
        ThrowFaultIfFailed(status);
    }

    /// <summary>
    /// Validates and updates the authenticated caller's profile image.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="image">The proposed profile image content.</param>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ProfileFault}">
    /// A declared profile rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public void UpdateProfileImage(string sessionToken, ImageContent image)
    {
        ProfileImageData? imageData = image == null ? null
            : new ProfileImageData(image.Content, image.MediaType);
        ProfileStatus status = _applicationService.UpdateImage(
            sessionToken,
            imageData);
        ThrowFaultIfFailed(status);
    }

    private static ApplicationProfile RequireValue(ProfileResult<ApplicationProfile> result)
    {
        return result.Value
            ?? throw new InvalidOperationException(
                "A successful profile result must contain its value.");
    }

    private static void ThrowFaultIfFailed(ProfileStatus status)
    {
        if (status == ProfileStatus.Success)
        {
            return;
        }
        if (status == ProfileStatus.MissingSessionToken)
        {
            throw ServiceFaultFactory.CreateSessionFault(SessionFailure.MissingSessionToken);
        }
        if (status == ProfileStatus.InvalidSession)
        {
            throw ServiceFaultFactory.CreateSessionFault(SessionFailure.InvalidSession);
        }

        ProfileFailure failure = MapProfileFailure(status);
        throw new FaultException<ProfileFault>(new ProfileFault
        {
            Reason = failure
        }, "The profile request was rejected.");
    }

    private static ProfileFailure MapProfileFailure(ProfileStatus status)
    {
        ProfileFailure failure = status switch
        {
            ProfileStatus.InactiveAccount => ProfileFailure.InactiveAccount,
            ProfileStatus.InvalidAccountId => ProfileFailure.InvalidAccountId,
            ProfileStatus.AccountNotFound => ProfileFailure.AccountNotFound,
            ProfileStatus.InvalidUsername => ProfileFailure.InvalidUsername,
            ProfileStatus.UsernameUnavailable => ProfileFailure.UsernameUnavailable,
            ProfileStatus.InvalidImage => ProfileFailure.InvalidImage,
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        return failure;
    }

    private static ContractProfile MapProfile(ApplicationProfile profile)
    {
        return new ContractProfile
        {
            Account = new AccountReference
            {
                AccountId = profile.AccountId,
                Username = profile.Username
            },
            HasProfileImage = !string.IsNullOrWhiteSpace(profile.ProfileImageUri),
            Statistics = new ReversiGameSystem.Server.Contracts.Players.PlayerStatistics
            {
                MatchesPlayed = profile.MatchesPlayed,
                Wins = profile.Wins,
                Losses = profile.Losses,
                Draws = profile.Draws,
                WinRate = profile.WinRate
            }
        };
    }
}
