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

    /// <inheritdoc />
    public ContractProfile GetPlayerProfile(string sessionToken, long? targetAccountId)
    {
        ProfileResult<ApplicationProfile> result = _applicationService.GetProfile(
            sessionToken,
            targetAccountId);
        ThrowFaultIfFailed(result.Status);

        return MapProfile(RequireValue(result));
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void ChangeUsername(string sessionToken, string username)
    {
        ProfileOperationStatus status = _applicationService.ChangeUsername(
            sessionToken,
            username);
        ThrowFaultIfFailed(status);
    }

    /// <inheritdoc />
    public void UpdateProfileImage(string sessionToken, ImageContent image)
    {
        ProfileImageData? imageData = image == null ? null
            : new ProfileImageData(image.Content, image.MediaType);
        ProfileOperationStatus status = _applicationService.UpdateImage(
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

    private static void ThrowFaultIfFailed(ProfileOperationStatus status)
    {
        if (status == ProfileOperationStatus.Success)
        {
            return;
        }
        if (status == ProfileOperationStatus.MissingSessionToken)
        {
            throw SessionFaultFactory.Create(SessionFailure.MissingSessionToken);
        }
        if (status == ProfileOperationStatus.InvalidSession)
        {
            throw SessionFaultFactory.Create(SessionFailure.InvalidSession);
        }

        throw CreateProfileFault(MapProfileFailure(status));
    }

    private static ProfileFailure MapProfileFailure(ProfileOperationStatus status)
    {
        switch (status)
        {
            case ProfileOperationStatus.InactiveAccount:
                return ProfileFailure.InactiveAccount;
            case ProfileOperationStatus.InvalidAccountId:
                return ProfileFailure.InvalidAccountId;
            case ProfileOperationStatus.AccountNotFound:
                return ProfileFailure.AccountNotFound;
            case ProfileOperationStatus.InvalidUsername:
                return ProfileFailure.InvalidUsername;
            case ProfileOperationStatus.UsernameUnavailable:
                return ProfileFailure.UsernameUnavailable;
            case ProfileOperationStatus.InvalidImage:
                return ProfileFailure.InvalidImage;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }

    private static FaultException<ProfileFault> CreateProfileFault(ProfileFailure failure)
    {
        return new FaultException<ProfileFault>(new ProfileFault
        {
            Reason = failure
        }, "The profile request was rejected.");
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
