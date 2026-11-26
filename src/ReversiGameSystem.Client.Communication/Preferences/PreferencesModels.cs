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
    ServiceUnavailable,
    OperationInProgress
}

internal sealed class PlayerPreferences
{
    public const string StandardPieceStyle = "STANDARD";
    public const string ProfilePicturesPieceStyle = "PROFILE_PICTURES";

    public string PreferredLocaleCode { get; set; } = string.Empty;

    public string? TimeZoneId { get; set; }

    public string PieceStyleCode { get; set; } = StandardPieceStyle;

    public bool UseCustomBoardBackground { get; set; }
}
