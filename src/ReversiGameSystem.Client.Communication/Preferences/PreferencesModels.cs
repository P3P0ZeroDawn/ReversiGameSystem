namespace ReversiGameSystem.Services;

internal enum PreferencesStatus
{
    Success,
    MissingPreferences,
    InvalidLocale,
    InvalidTimeZone,
    InvalidPieceStyle,
    InvalidImage,
    InactiveAccount,
    InvalidSession,
    ServiceUnavailable
}
