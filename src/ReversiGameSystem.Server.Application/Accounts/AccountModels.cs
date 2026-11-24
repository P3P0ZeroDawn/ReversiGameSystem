using System;

namespace ReversiGameSystem.Server.Application.Accounts;

public enum AccountStatus
{
    Success,
    MissingRegistrationData,
    InvalidUsername,
    InvalidEmailAddress,
    InvalidPassword,
    InvalidLocale,
    InvalidImage,
    UsernameUnavailable,
    EmailUnavailable,
    InvalidVerificationCode,
    ExpiredVerificationCode,
    IncorrectCurrentPassword,
    InactiveAccount,
    AccountNotFound,
    ActiveMatchInProgress,
    MissingSessionToken,
    InvalidSession
}

public sealed class AccountResult<T>
{
    public AccountResult(AccountStatus status)
    {
        if (status == AccountStatus.Success)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "A failure result cannot use Success status.");
        }

        Status = status;
    }

    public AccountResult(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        Status = AccountStatus.Success;
        Value = value;
    }

    public AccountStatus Status { get; }

    public T? Value { get; }
}

public sealed class NewAccountRegistration
{
    public NewAccountRegistration(
        string verificationCode,
        string username,
        string emailAddress,
        string password,
        string preferredLocaleCode,
        ProfileImageData? profileImage)
    {
        VerificationCode = verificationCode;
        Username = username;
        EmailAddress = emailAddress;
        Password = password;
        PreferredLocaleCode = preferredLocaleCode;
        ProfileImage = profileImage;
    }

    public string VerificationCode { get; }

    public string Username { get; }

    public string EmailAddress { get; }

    public string Password { get; }

    public string PreferredLocaleCode { get; }

    public ProfileImageData? ProfileImage { get; }
}

public sealed class ProfileImageData
{
    public ProfileImageData(byte[] content, string mediaType)
    {
        Content = content;
        MediaType = mediaType;
    }

    public byte[] Content { get; }

    public string MediaType { get; }
}

public sealed class AccountRegistrationData
{
    public AccountRegistrationData(
        string username,
        string emailAddress,
        string passwordHash,
        string preferredLocaleCode,
        string? profileImageUri)
    {
        Username = username;
        EmailAddress = emailAddress;
        PasswordHash = passwordHash;
        PreferredLocaleCode = preferredLocaleCode;
        ProfileImageUri = profileImageUri;
    }

    public string Username { get; }

    public string EmailAddress { get; }

    public string PasswordHash { get; }

    public string PreferredLocaleCode { get; }

    public string? ProfileImageUri { get; }
}

public sealed class AccountSummary
{
    public AccountSummary(long accountId, string username)
    {
        AccountId = accountId;
        Username = username;
    }

    public long AccountId { get; }

    public string Username { get; }
}
