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
    private readonly object _backgroundReplacementLock = new object();
    private readonly SessionStore _sessions;
    private readonly IPreferencesRepository _preferences;
    private readonly IPlayerImageRepository _images;

    public PreferencesApplicationService(SessionStore sessions,
        IPreferencesRepository preferences, IPlayerImageRepository images)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        _images = images ?? throw new ArgumentNullException(nameof(images));
    }

    public PreferencesAccess GetPreferences(string sessionToken)
    {
        SessionResult<AuthenticatedSession> access =
            _sessions.FindAuthenticated(sessionToken);
        PreferencesAccess preferences;
        if (access.Status != SessionStatus.Success || access.Value == null)
        {
            preferences = new PreferencesAccess(MapSessionStatus(access.Status));
        }
        else
        {
            preferences = _preferences.ReadPreferences(access.Value.AccountId);
        }
        return preferences;
    }

    public PreferencesStatus UpdatePreferences(string sessionToken, PlayerPreferences? preferences)
    {
        SessionResult<AuthenticatedSession> access =
            _sessions.FindAuthenticated(sessionToken);
        if (access.Status != SessionStatus.Success || access.Value == null)
        {
            return MapSessionStatus(access.Status);
        }
        PreferencesAccess caller = _preferences.ReadPreferences(access.Value.AccountId);
        if (caller.Status != PreferencesStatus.Success)
        {
            return caller.Status;
        }
        PreferencesStatus status = ValidatePreferences(preferences);
        if (status == PreferencesStatus.Success && preferences != null)
        {
            status = _preferences.SavePreferences(access.Value.AccountId, preferences);
        }
        return status;
    }

    public PreferencesAccess GetBoardBackground(string sessionToken)
    {
        lock (_backgroundReplacementLock)
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
    }

    public PreferencesStatus ChangeBoardBackground(string sessionToken, ProfileImageData? image)
    {
        SessionResult<AuthenticatedSession> session =
            _sessions.FindAuthenticated(sessionToken);
        if (session.Status != SessionStatus.Success || session.Value == null)
        {
            return MapSessionStatus(session.Status);
        }
        if (image == null || !PlayerImagePolicy.IsSupported(image.Content, image.MediaType))
        {
            return PreferencesStatus.InvalidImage;
        }
        long accountId = session.Value.AccountId;
        lock (_backgroundReplacementLock)
        {
            PreferencesAccess access = _preferences.ReadPreferences(accountId);
            if (access.Status != PreferencesStatus.Success)
            {
                return access.Status;
            }
            string previousReference = access.Preferences.BoardBackgroundReference;
            ProfileResult<string> stored = _images.SaveImage(
                image, PlayerImageKind.BoardBackground);
            if (stored.Status == ProfileStatus.InvalidImage)
            {
                return PreferencesStatus.InvalidImage;
            }
            string reference = stored.Value
                ?? throw new InvalidOperationException(
                    "The stored image reference is unavailable.");
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
            else if (!string.IsNullOrWhiteSpace(previousReference)
                && previousReference != reference)
            {
                _images.DeleteImage(previousReference);
            }
            return status;
        }
    }

    private static PreferencesStatus MapSessionStatus(SessionStatus status)
    {
        PreferencesStatus mapped = status switch
        {
            SessionStatus.MissingSessionToken => PreferencesStatus.MissingSessionToken,
            SessionStatus.InvalidSession => PreferencesStatus.InvalidSession,
            SessionStatus.InactiveAccount => PreferencesStatus.InactiveAccount,
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
