using System;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Accounts;

public interface IAccountRegistrationStore
{
    Task<AccountRegistrationStoreResult> RegisterAsync(AccountRegistrationData registration);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string encodedHash);
}

public interface IProfileImageStore
{
    Task<ProfileImageStorageResult> SaveAsync(ProfileImageData image);

    Task DeleteAsync(string imageUri);
}

public interface IUtcClock
{
    DateTime UtcNow { get; }
}

public sealed class AccountRegistrationData
{
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

    public string Username { get; }

    public string EmailAddress { get; }

    public string PasswordHash { get; }

    public string PreferredLocaleCode { get; }

    public string? ProfileImageUri { get; }

    public DateTime RegisteredAtUtc { get; }
}

public sealed class AccountRegistrationStoreResult
{
    public AccountRegistrationStoreResult(
        AccountRegistrationStoreOutcome outcome,
        long? accountId = null)
    {
        Outcome = outcome;
        AccountId = accountId;
    }

    public AccountRegistrationStoreOutcome Outcome { get; }

    public long? AccountId { get; }
}

public enum AccountRegistrationStoreOutcome
{
    Success,
    UsernameAlreadyExists,
    EmailAddressAlreadyExists,
    UnsupportedLocale,
    Unavailable
}

public sealed class ProfileImageStorageResult
{
    public ProfileImageStorageResult(ProfileImageStorageOutcome outcome, string? imageUri = null)
    {
        Outcome = outcome;
        ImageUri = imageUri;
    }

    public ProfileImageStorageOutcome Outcome { get; }

    public string? ImageUri { get; }
}

public enum ProfileImageStorageOutcome
{
    Success,
    InvalidImage,
    Unavailable
}
