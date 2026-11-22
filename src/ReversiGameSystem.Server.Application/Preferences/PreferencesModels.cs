using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Preferences;

public enum PreferencesStatus
{
    Success,
    MissingSessionToken,
    InvalidSession,
    InactiveAccount,
    MissingPreferences,
    InvalidLocale,
    InvalidTimeZone,
    InvalidPieceStyle,
    InvalidImage
}

public sealed class PlayerPreferences
{
    public string PreferredLocaleCode { get; set; } = string.Empty;
    public string? TimeZoneId { get; set; }
    public string PieceStyleCode { get; set; } = string.Empty;
    public bool UseCustomBoardBackground { get; set; }
    public string BoardBackgroundReference { get; set; } = string.Empty;
}

public sealed class PreferencesAccess
{
    private const string RejectedAccessMessage = "The preferences access was rejected.";
    private readonly PlayerPreferences? _preferences;
    private readonly ProfileImageData? _backgroundImage;

    public PreferencesAccess(
        PlayerPreferences preferences, ProfileImageData? backgroundImage = null)
    {
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        _backgroundImage = backgroundImage;
        Status = PreferencesStatus.Success;
    }

    public PreferencesAccess(PreferencesStatus status)
    {
        if (status == PreferencesStatus.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }
        Status = status;
    }

    public PreferencesStatus Status { get; }
    public ProfileImageData? BackgroundImage
    {
        get
        {
            if (Status != PreferencesStatus.Success)
            {
                throw new InvalidOperationException(RejectedAccessMessage);
            }
            return _backgroundImage;
        }
    }
    public PlayerPreferences Preferences
    {
        get
        {
            return _preferences
                ?? throw new InvalidOperationException(RejectedAccessMessage);
        }
    }
}
