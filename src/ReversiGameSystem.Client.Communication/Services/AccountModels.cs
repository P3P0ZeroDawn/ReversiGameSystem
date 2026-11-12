namespace ReversiGameSystem.Services;

internal enum AccountCreationStatus
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
    PersistenceUnavailable,
    ServiceUnavailable,
    SubmissionInProgress
}
