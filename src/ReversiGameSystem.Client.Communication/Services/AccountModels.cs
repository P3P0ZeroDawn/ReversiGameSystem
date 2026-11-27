namespace ReversiGameSystem.Services;

internal enum AccountStatus
{
    Success,
    MissingRequiredInformation,
    UsernameUnavailable,
    EmailUnavailable,
    InvalidUsername,
    InvalidEmailFormat,
    EmailTooLong,
    InvalidEmailLocalPart,
    InvalidEmailDomain,
    PasswordMismatch,
    PasswordTooShort,
    PasswordTooLong,
    PasswordMissingUppercase,
    PasswordMissingLowercase,
    PasswordMissingDigit,
    PasswordMissingSymbol,
    PasswordUnsupportedCharacters,
    InvalidLocale,
    InvalidProfileImage,
    ServiceUnavailable,
    InvalidVerificationCode,
    ExpiredVerificationCode,
    InvalidPassword,
    SubmissionInProgress
}

internal sealed class NewAccount
{
    internal string VerificationCode { get; set; } = string.Empty;

    internal string Username { get; set; } = string.Empty;

    internal string EmailAddress { get; set; } = string.Empty;

    internal string Password { get; set; } = string.Empty;

    internal string PreferredLocaleCode { get; set; } = string.Empty;

    internal PlayerImageContent? ProfileImage { get; set; }
}
