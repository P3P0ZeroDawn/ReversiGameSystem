using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Application.Preferences;

public sealed class PreferencesApplicationService
{
    private const string EnglishLocale = "en-US";
    private const string SpanishLocale = "es-MX";
    private const string StandardPieceStyle = "STANDARD";
    private const string ProfilePicturesPieceStyle = "PROFILE_PICTURES";
    private readonly ISessionRepository _sessions;
    private readonly IPreferencesRepository _preferences;
    private readonly IPlayerImageRepository _images;

    public PreferencesApplicationService(ISessionRepository sessions,
        IPreferencesRepository preferences, IPlayerImageRepository images)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        _images = images ?? throw new ArgumentNullException(nameof(images));
    }

    public PreferencesAccess GetPreferences(string sessionToken)
    {
        SessionOperationResult<AuthenticatedSession> access = ResolveSession(sessionToken);
        PreferencesAccess preferences;
        if (access.Status != SessionOperationStatus.Success)
        {
            preferences = new PreferencesAccess(MapSessionStatus(access.Status));
        }
        else
        {
            preferences = _preferences.ReadPreferences(access.GetValue().AccountId);
        }
        return preferences;
    }

    public PreferencesStatus UpdatePreferences(string sessionToken, PlayerPreferences? preferences)
    {
        SessionOperationResult<AuthenticatedSession> access = ResolveSession(sessionToken);
        if (access.Status != SessionOperationStatus.Success)
        {
            return MapSessionStatus(access.Status);
        }
        PreferencesAccess caller = _preferences.ReadPreferences(access.GetValue().AccountId);
        if (caller.Status != PreferencesStatus.Success)
        {
            return caller.Status;
        }
        PreferencesStatus status = ValidatePreferences(preferences);
        if (status == PreferencesStatus.Success && preferences != null)
        {
            status = _preferences.SavePreferences(access.GetValue().AccountId, preferences);
        }
        return status;
    }

    public PreferencesAccess GetBoardBackground(string sessionToken)
    {
        PreferencesAccess access = GetPreferences(sessionToken);
        if (access.Status != PreferencesStatus.Success)
        {
            return access;
        }
        PlayerPreferences preferences = access.Preferences;
        ProfileImageData? image = null;
        if (!string.IsNullOrWhiteSpace(preferences.BoardBackgroundReference))
        {
            image = _images.ReadImage(preferences.BoardBackgroundReference);
        }
        return new PreferencesAccess(preferences, image);
    }

    public PreferencesStatus ChangeBoardBackground(string sessionToken, ProfileImageData? image)
    {
        SessionOperationResult<AuthenticatedSession> session = ResolveSession(sessionToken);
        if (session.Status != SessionOperationStatus.Success)
        {
            return MapSessionStatus(session.Status);
        }
        long accountId = session.GetValue().AccountId;
        PreferencesAccess access = _preferences.ReadPreferences(accountId);
        if (access.Status != PreferencesStatus.Success)
        {
            return access.Status;
        }
        if (image == null || !ProfileImagePolicy.IsSupported(image.Content, image.MediaType))
        {
            return PreferencesStatus.InvalidImage;
        }
        ProfileResult<string> stored = _images.SaveImage(image);
        if (stored.Status == ProfileOperationStatus.InvalidImage)
        {
            return PreferencesStatus.InvalidImage;
        }
        string reference = stored.Value
            ?? throw new InvalidOperationException("The stored image reference is unavailable.");
        PreferencesStatus status;
        try
        {
            status = _preferences.SetBoardBackground(accountId, reference);
        }
        catch (Exception associationFailure)
        {
            try
            {
                _images.DeleteImage(reference);
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(associationFailure, cleanupFailure);
            }
            throw;
        }
        if (status != PreferencesStatus.Success)
        {
            _images.DeleteImage(reference);
        }
        return status;
    }

    private SessionOperationResult<AuthenticatedSession> ResolveSession(string token)
    {
        SessionOperationResult<AuthenticatedSession> access;
        if (string.IsNullOrWhiteSpace(token))
        {
            access = SessionOperationResult<AuthenticatedSession>.CreateFailure(
                SessionOperationStatus.MissingSessionToken);
        }
        else
        {
            access = _sessions.FindAuthenticated(token);
        }
        return access;
    }

    private static PreferencesStatus MapSessionStatus(SessionOperationStatus status)
    {
        PreferencesStatus mapped = status switch
        {
            SessionOperationStatus.MissingSessionToken => PreferencesStatus.MissingSessionToken,
            SessionOperationStatus.InvalidSession => PreferencesStatus.InvalidSession,
            SessionOperationStatus.InactiveAccount => PreferencesStatus.InactiveAccount,
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
        return mapped;
    }

    private static PreferencesStatus ValidatePreferences(PlayerPreferences? preferences)
    {
        PreferencesStatus status = PreferencesStatus.Success;
        if (preferences == null)
        {
            return PreferencesStatus.MissingPreferences;
        }
        if (preferences.PreferredLocaleCode != EnglishLocale
            && preferences.PreferredLocaleCode != SpanishLocale)
        {
            status = PreferencesStatus.InvalidLocale;
        }
        else if (preferences.PieceStyleCode != StandardPieceStyle
            && preferences.PieceStyleCode != ProfilePicturesPieceStyle)
        {
            status = PreferencesStatus.InvalidPieceStyle;
        }
        else if (preferences.TimeZoneId != null)
        {
            if (string.IsNullOrWhiteSpace(preferences.TimeZoneId))
            {
                status = PreferencesStatus.InvalidTimeZone;
            }
            else
            {
                try
                {
                    TimeZoneInfo.FindSystemTimeZoneById(preferences.TimeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                    status = PreferencesStatus.InvalidTimeZone;
                }
            }
        }
        return status;
    }
}
