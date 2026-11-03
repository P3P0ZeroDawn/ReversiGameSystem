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
        GetPlayerProfileResult result = _applicationService.GetProfileAsync(
            sessionToken,
            targetAccountId).GetAwaiter().GetResult();
        if (result.Status != ProfileOperationStatus.Success)
        {
            throw MapFailure(result.Status);
        }
        if (result.Profile == null)
        {
            throw new InvalidOperationException(
                "A successful profile result must contain a profile.");
        }

        return MapProfile(result.Profile);
    }

    /// <inheritdoc />
    public ImageContent? GetProfileImage(string sessionToken, long? targetAccountId)
    {
        GetProfileImageResult result = _applicationService.GetImageAsync(
            sessionToken,
            targetAccountId).GetAwaiter().GetResult();
        if (result.Status != ProfileOperationStatus.Success)
        {
            throw MapFailure(result.Status);
        }
        ImageContent? image;
        if (result.Image == null)
        {
            image = null;
        }
        else
        {
            image = new ImageContent
            {
                MediaType = result.Image.MediaType,
                Content = result.Image.Content
            };
        }

        return image;
    }

    /// <inheritdoc />
    public void ChangeUsername(string sessionToken, string username)
    {
        ProfileOperationStatus status = _applicationService.ChangeUsernameAsync(
            sessionToken,
            username).GetAwaiter().GetResult();
        if (status != ProfileOperationStatus.Success)
        {
            throw MapFailure(status);
        }
    }

    /// <inheritdoc />
    public void UpdateProfileImage(string sessionToken, ImageContent image)
    {
        ProfileImageData? imageData = image == null ? null
            : new ProfileImageData(image.Content, image.MediaType);
        ProfileOperationStatus status = _applicationService.UpdateImageAsync(
            sessionToken,
            imageData).GetAwaiter().GetResult();
        if (status != ProfileOperationStatus.Success)
        {
            throw MapFailure(status);
        }
    }

    private static FaultException MapFailure(ProfileOperationStatus status)
    {
        FaultException fault;
        if (status == ProfileOperationStatus.MissingSessionToken)
        {
            fault = SessionFaultFactory.Create(SessionFailure.MissingSessionToken);
        }
        else if (status == ProfileOperationStatus.InvalidSession)
        {
            fault = SessionFaultFactory.Create(SessionFailure.InvalidSession);
        }
        else
        {
            ProfileFailure failure;
            if (status == ProfileOperationStatus.InactiveAccount)
            {
                failure = ProfileFailure.InactiveAccount;
            }
            else if (status == ProfileOperationStatus.InvalidAccountId)
            {
                failure = ProfileFailure.InvalidAccountId;
            }
            else if (status == ProfileOperationStatus.AccountNotFound)
            {
                failure = ProfileFailure.AccountNotFound;
            }
            else if (status == ProfileOperationStatus.InvalidUsername)
            {
                failure = ProfileFailure.InvalidUsername;
            }
            else if (status == ProfileOperationStatus.UsernameUnavailable)
            {
                failure = ProfileFailure.UsernameUnavailable;
            }
            else if (status == ProfileOperationStatus.InvalidImage)
            {
                failure = ProfileFailure.InvalidImage;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            fault = CreateProfileFault(failure);
        }

        return fault;
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
