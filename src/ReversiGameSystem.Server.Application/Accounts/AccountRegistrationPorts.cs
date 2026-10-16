using System;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>
/// Defines the account registration store contract.
/// </summary>
public interface IAccountRegistrationStore
{
    /// <summary>
    /// Performs the register async operation.
    /// </summary>
    /// <param name="registration">The registration.</param>
    /// <returns>The result of the operation.</returns>
    public Task<AccountRegistrationStoreResult> RegisterAsync(
        AccountRegistrationData registration);
}

/// <summary>
/// Defines the password hashing contract.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Performs the hash operation.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <returns>The result of the operation.</returns>
    public string Hash(string password);

    /// <summary>
    /// Performs the verify operation.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <param name="encodedHash">The encoded hash.</param>
    /// <returns>The result of the operation.</returns>
    public bool Verify(string password, string encodedHash);
}

/// <summary>
/// Defines the profile image storage contract.
/// </summary>
public interface IProfileImageStore
{
    /// <summary>
    /// Saves a profile image.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <returns>The result of the operation.</returns>
    public ProfileImageStorageResult Save(ProfileImageData image);

    /// <summary>
    /// Deletes a profile image.
    /// </summary>
    /// <param name="imageUri">The image uri.</param>
    /// <returns>The result of the operation.</returns>
    public void Delete(string imageUri);
}

/// <summary>
/// Defines the UTC clock contract.
/// </summary>
public interface IUtcClock
{
    /// <summary>
    /// Gets the utc now.
    /// </summary>
    public DateTime UtcNow { get; }
}

/// <summary>
/// Represents account registration data.
/// </summary>
public sealed class AccountRegistrationData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRegistrationData"/> class.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="emailAddress">The email address.</param>
    /// <param name="passwordHash">The password hash.</param>
    /// <param name="preferredLocaleCode">The preferred locale code.</param>
    /// <param name="profileImageUri">The profile image uri.</param>
    /// <param name="registeredAtUtc">The registered at utc.</param>
    public AccountRegistrationData(
        string username,
        string emailAddress,
        string passwordHash,
        string preferredLocaleCode,
        string? profileImageUri,
        DateTime registeredAtUtc)
    {
        Username = username;
        EmailAddress = emailAddress;
        PasswordHash = passwordHash;
        PreferredLocaleCode = preferredLocaleCode;
        ProfileImageUri = profileImageUri;
        RegisteredAtUtc = registeredAtUtc;
    }

    /// <summary>
    /// Gets the username.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Gets the email address.
    /// </summary>
    public string EmailAddress { get; }

    /// <summary>
    /// Gets the password hash.
    /// </summary>
    public string PasswordHash { get; }

    /// <summary>
    /// Gets the preferred locale code.
    /// </summary>
    public string PreferredLocaleCode { get; }

    /// <summary>
    /// Gets the profile image uri.
    /// </summary>
    public string? ProfileImageUri { get; }

    /// <summary>
    /// Gets the registered at utc.
    /// </summary>
    public DateTime RegisteredAtUtc { get; }
}

/// <summary>
/// Represents account registration store result.
/// </summary>
public sealed class AccountRegistrationStoreResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRegistrationStoreResult"/> class.
    /// </summary>
    /// <param name="outcome">The outcome.</param>
    /// <param name="accountId">The account id.</param>
    public AccountRegistrationStoreResult(
        AccountRegistrationStoreOutcome outcome,
        long? accountId = null)
    {
        Outcome = outcome;
        AccountId = accountId;
    }

    /// <summary>
    /// Gets the outcome.
    /// </summary>
    public AccountRegistrationStoreOutcome Outcome { get; }

    /// <summary>
    /// Gets the account id.
    /// </summary>
    public long? AccountId { get; }
}

/// <summary>
/// Defines the supported account registration store outcome values.
/// </summary>
public enum AccountRegistrationStoreOutcome
{
    /// <summary>
    /// Represents success.
    /// </summary>
    Success,
    /// <summary>
    /// Represents username already exists.
    /// </summary>
    UsernameAlreadyExists,
    /// <summary>
    /// Represents email address already exists.
    /// </summary>
    EmailAddressAlreadyExists,
    /// <summary>
    /// Represents unsupported locale.
    /// </summary>
    UnsupportedLocale,
    /// <summary>
    /// Represents unavailable.
    /// </summary>
    Unavailable
}

/// <summary>
/// Represents profile image storage result.
/// </summary>
public sealed class ProfileImageStorageResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileImageStorageResult"/> class.
    /// </summary>
    /// <param name="outcome">The outcome.</param>
    /// <param name="imageUri">The image uri.</param>
    public ProfileImageStorageResult(ProfileImageStorageOutcome outcome, string? imageUri = null)
    {
        Outcome = outcome;
        ImageUri = imageUri;
    }

    /// <summary>
    /// Gets the outcome.
    /// </summary>
    public ProfileImageStorageOutcome Outcome { get; }

    /// <summary>
    /// Gets the image uri.
    /// </summary>
    public string? ImageUri { get; }
}

/// <summary>
/// Defines the supported profile image storage outcome values.
/// </summary>
public enum ProfileImageStorageOutcome
{
    /// <summary>
    /// Represents success.
    /// </summary>
    Success,
    /// <summary>
    /// Represents invalid image.
    /// </summary>
    InvalidImage,
    /// <summary>
    /// Represents unavailable.
    /// </summary>
    Unavailable
}
