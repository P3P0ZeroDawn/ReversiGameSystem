using System;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>
/// Identifies the expected outcome of an account operation.
/// </summary>
public enum AccountStatus
{
    /// <summary>The account operation completed successfully.</summary>
    Success,
    /// <summary>The registration payload is absent.</summary>
    MissingRegistrationData,
    /// <summary>The proposed username violates the username policy.</summary>
    InvalidUsername,
    /// <summary>The email address violates the email policy.</summary>
    InvalidEmailAddress,
    /// <summary>The password violates the password policy.</summary>
    InvalidPassword,
    /// <summary>The preferred locale code is absent or unsupported.</summary>
    InvalidLocale,
    /// <summary>The profile image is absent or invalid.</summary>
    InvalidImage,
    /// <summary>Another account already uses the username.</summary>
    UsernameUnavailable,
    /// <summary>Another account already uses the email address.</summary>
    EmailUnavailable,
    /// <summary>The verification code is incorrect or already consumed.</summary>
    InvalidVerificationCode,
    /// <summary>The pending verification code has expired.</summary>
    ExpiredVerificationCode,
    /// <summary>The supplied current password does not match the account.</summary>
    IncorrectCurrentPassword,
    /// <summary>The requesting or target account is not active.</summary>
    InactiveAccount,
    /// <summary>No account matched the requested identifier.</summary>
    AccountNotFound,
    /// <summary>The account participates in an active or waiting match.</summary>
    ActiveMatchInProgress,
    /// <summary>The required session token is absent.</summary>
    MissingSessionToken,
    /// <summary>The session token is invalid, expired, or belongs to a guest.</summary>
    InvalidSession
}

/// <summary>
/// Contains an expected account outcome and its optional value.
/// </summary>
/// <typeparam name="T">The successful result type.</typeparam>
public sealed class AccountResult<T>
{
    /// <summary>Creates a failed account result.</summary>
    /// <param name="status">The expected failure status.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The supplied status represents success.
    /// </exception>
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

    /// <summary>Creates a successful account result.</summary>
    /// <param name="value">The successful operation value.</param>
    /// <exception cref="ArgumentNullException">The value is absent.</exception>
    public AccountResult(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        Status = AccountStatus.Success;
        Value = value;
    }

    /// <summary>Gets the expected operation status.</summary>
    public AccountStatus Status { get; }

    /// <summary>Gets the successful operation value, or default on failure.</summary>
    public T? Value { get; }
}

/// <summary>
/// Contains validated registration inputs for creating a registered account.
/// </summary>
public sealed class NewAccountRegistration
{
    /// <summary>Initializes a new registration request.</summary>
    /// <param name="verificationCode">The one-use registration verification code.</param>
    /// <param name="username">The proposed public username.</param>
    /// <param name="emailAddress">The account email address.</param>
    /// <param name="password">The account password.</param>
    /// <param name="preferredLocaleCode">The preferred interface locale code.</param>
    /// <param name="profileImage">The optional custom profile image.</param>
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

    /// <summary>Gets the one-use registration verification code.</summary>
    public string VerificationCode { get; }

    /// <summary>Gets the proposed public username.</summary>
    public string Username { get; }

    /// <summary>Gets the account email address.</summary>
    public string EmailAddress { get; }

    /// <summary>Gets the account password.</summary>
    public string Password { get; }

    /// <summary>Gets the preferred interface locale code.</summary>
    public string PreferredLocaleCode { get; }

    /// <summary>Gets the optional custom profile image.</summary>
    public ProfileImageData? ProfileImage { get; }
}

/// <summary>
/// Represents the public identity of a newly registered account.
/// </summary>
public sealed class AccountSummary
{
    /// <summary>Initializes a new account summary.</summary>
    /// <param name="accountId">The persistent account identifier.</param>
    /// <param name="username">The public username.</param>
    public AccountSummary(long accountId, string username)
    {
        AccountId = accountId;
        Username = username;
    }

    /// <summary>Gets the persistent account identifier.</summary>
    public long AccountId { get; }

    /// <summary>Gets the public username.</summary>
    public string Username { get; }
}
