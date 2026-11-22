using System;
using System.ServiceModel;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Preferences;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using ApplicationPreferences = ReversiGameSystem.Server.Application.Preferences.PlayerPreferences;
using ContractPreferences = ReversiGameSystem.Server.Contracts.Players.PlayerPreferences;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Exposes authenticated preferences and stored board backgrounds.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class PlayerPreferencesService : IPlayerPreferencesService
{
    private const string RejectionMessage = "The preferences request was rejected.";
    private readonly PreferencesApplicationService _applicationService;

    /// <summary>Initializes the preferences transport boundary.</summary>
    /// <param name="applicationService">The preferences application service.</param>
    public PlayerPreferencesService(PreferencesApplicationService applicationService)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
    }

    /// <inheritdoc />
    public ContractPreferences GetPlayerPreferences(string sessionToken)
    {
        PreferencesAccess access = _applicationService.GetPreferences(sessionToken);
        ThrowFaultIfFailed(access.Status);
        ApplicationPreferences preferences = access.Preferences;
        return new ContractPreferences
        {
            PreferredLocaleCode = preferences.PreferredLocaleCode,
            TimeZoneId = preferences.TimeZoneId,
            PieceStyleCode = preferences.PieceStyleCode,
            UseCustomBoardBackground = preferences.UseCustomBoardBackground
        };
    }

    /// <inheritdoc />
    public void UpdatePlayerPreferences(string sessionToken, ContractPreferences preferences)
    {
        ApplicationPreferences? proposed = null;
        if (preferences != null)
        {
            proposed = new ApplicationPreferences
            {
                PreferredLocaleCode = preferences.PreferredLocaleCode,
                TimeZoneId = preferences.TimeZoneId,
                PieceStyleCode = preferences.PieceStyleCode,
                UseCustomBoardBackground = preferences.UseCustomBoardBackground
            };
        }
        PreferencesStatus status = _applicationService.UpdatePreferences(sessionToken, proposed);
        ThrowFaultIfFailed(status);
    }

    /// <inheritdoc />
    public ImageContent? GetBoardBackground(string sessionToken)
    {
        PreferencesAccess access = _applicationService.GetBoardBackground(sessionToken);
        ThrowFaultIfFailed(access.Status);
        ImageContent? image = null;
        if (access.BackgroundImage != null)
        {
            image = new ImageContent
            {
                Content = access.BackgroundImage.Content,
                MediaType = access.BackgroundImage.MediaType
            };
        }
        return image;
    }

    /// <inheritdoc />
    public void ChangeBoardBackground(string sessionToken, ImageContent image)
    {
        ProfileImageData? proposed = null;
        if (image != null)
        {
            proposed = new ProfileImageData(image.Content, image.MediaType);
        }
        ThrowFaultIfFailed(_applicationService.ChangeBoardBackground(sessionToken, proposed));
    }

    private static void ThrowFaultIfFailed(PreferencesStatus status)
    {
        if (status == PreferencesStatus.Success)
        {
            return;
        }
        if (status == PreferencesStatus.MissingSessionToken)
        {
            throw ServiceFaultFactory.CreateSessionFault(SessionFailure.MissingSessionToken);
        }
        if (status == PreferencesStatus.InvalidSession)
        {
            throw ServiceFaultFactory.CreateSessionFault(SessionFailure.InvalidSession);
        }
        PreferencesFailure reason = status switch
        {
            PreferencesStatus.InactiveAccount => PreferencesFailure.InactiveAccount,
            PreferencesStatus.MissingPreferences => PreferencesFailure.MissingPreferences,
            PreferencesStatus.InvalidLocale => PreferencesFailure.InvalidLocale,
            PreferencesStatus.InvalidTimeZone => PreferencesFailure.InvalidTimeZone,
            PreferencesStatus.InvalidPieceStyle => PreferencesFailure.InvalidPieceStyle,
            PreferencesStatus.InvalidImage => PreferencesFailure.InvalidImage,
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
        throw new FaultException<PreferencesFault>(new PreferencesFault { Reason = reason },
            RejectionMessage);
    }
}
