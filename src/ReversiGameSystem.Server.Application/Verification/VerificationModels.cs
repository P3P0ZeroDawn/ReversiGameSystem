using System;
using System.Globalization;

namespace ReversiGameSystem.Server.Application.Verification;

public enum VerificationPurpose
{
    Login,
    Registration,
    PasswordReset
}

public sealed class VerificationIdentity
{
    private const string DefaultLocaleCode = "en-US";
    private const string LoginKeyPrefix = "login";
    private const string RegistrationKeyPrefix = "registration";
    private const string PasswordResetKeyPrefix = "password-reset";

    private VerificationIdentity(
        VerificationPurpose purpose,
        string key,
        string recipientAddress,
        string preferredLocaleCode)
    {
        Purpose = purpose;
        Key = key;
        RecipientAddress = recipientAddress;
        PreferredLocaleCode = string.IsNullOrWhiteSpace(preferredLocaleCode)
            ? DefaultLocaleCode
            : preferredLocaleCode.Trim();
    }

    public VerificationPurpose Purpose { get; }

    public string Key { get; }

    public string RecipientAddress { get; }

    public string PreferredLocaleCode { get; }

    public static VerificationIdentity ForLogin(
        long accountId,
        string emailAddress,
        string? preferredLocaleCode = null)
    {
        VerificationIdentity identity = CreateAccountIdentity(
            VerificationPurpose.Login,
            LoginKeyPrefix,
            accountId,
            emailAddress,
            preferredLocaleCode);
        return identity;
    }

    public static VerificationIdentity ForRegistration(
        string username,
        string emailAddress,
        string? preferredLocaleCode = null)
    {
        string normalizedName = username.Trim().ToUpperInvariant();
        string recipient = emailAddress.Trim();
        string normalizedEmail = recipient.ToUpperInvariant();
        string identityKey = RegistrationKeyPrefix
            + ":"
            + normalizedName.Length
            + ":"
            + normalizedName
            + normalizedEmail;
        VerificationIdentity identity = new VerificationIdentity(
            VerificationPurpose.Registration,
            identityKey,
            recipient,
            preferredLocaleCode ?? DefaultLocaleCode);
        return identity;
    }

    public static VerificationIdentity ForPasswordReset(
        long accountId,
        string emailAddress,
        string? preferredLocaleCode = null)
    {
        VerificationIdentity identity = CreateAccountIdentity(
            VerificationPurpose.PasswordReset,
            PasswordResetKeyPrefix,
            accountId,
            emailAddress,
            preferredLocaleCode);
        return identity;
    }

    private static VerificationIdentity CreateAccountIdentity(
        VerificationPurpose purpose,
        string prefix,
        long accountId,
        string emailAddress,
        string? preferredLocaleCode)
    {
        string recipient = emailAddress.Trim();
        string accountKey = accountId.ToString(CultureInfo.InvariantCulture);
        string identityKey = prefix + ":" + accountKey + ":" + recipient.ToUpperInvariant();
        VerificationIdentity identity = new VerificationIdentity(
            purpose,
            identityKey,
            recipient,
            preferredLocaleCode ?? DefaultLocaleCode);
        return identity;
    }
}

public sealed class PendingVerificationCode
{
    public string RecipientAddress { get; set; } = string.Empty;

    public VerificationPurpose Purpose { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string IdentityKey { get; set; } = string.Empty;

    public byte[] Digest { get; set; } = Array.Empty<byte>();

    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class StoredVerificationCode
{
    public StoredVerificationCode()
    {
        Exists = false;
        Digest = Array.Empty<byte>();
    }

    public StoredVerificationCode(
        byte[] digest,
        DateTime expiresAtUtc,
        int failedAttempts,
        DateTime? usedAtUtc,
        DateTime? invalidatedAtUtc)
    {
        Exists = true;
        Digest = digest ?? throw new ArgumentNullException(nameof(digest));
        ExpiresAtUtc = expiresAtUtc;
        FailedAttempts = failedAttempts;
        UsedAtUtc = usedAtUtc;
        InvalidatedAtUtc = invalidatedAtUtc;
    }

    public bool Exists { get; }

    public byte[] Digest { get; }

    public DateTime ExpiresAtUtc { get; }

    public int FailedAttempts { get; }

    public DateTime? UsedAtUtc { get; set; }

    public DateTime? InvalidatedAtUtc { get; set; }
}

public sealed class VerificationEvaluationResult
{
    public VerificationEvaluationResult(VerificationStatus status)
    {
        if (status == VerificationStatus.Accepted)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Status = status;
    }

    public VerificationEvaluationResult(DateTime usedAtUtc)
    {
        Status = VerificationStatus.Accepted;
        MarkUsedAtUtc = usedAtUtc;
    }

    public VerificationEvaluationResult(
        int updatedFailedAttempts,
        DateTime? invalidatedAtUtc)
    {
        Status = VerificationStatus.Invalid;
        UpdatedFailedAttempts = updatedFailedAttempts;
        InvalidatedAtUtc = invalidatedAtUtc;
    }

    public VerificationStatus Status { get; }

    public DateTime? MarkUsedAtUtc { get; }

    public int? UpdatedFailedAttempts { get; }

    public DateTime? InvalidatedAtUtc { get; }
}

public enum VerificationStatus
{
    Accepted,
    Invalid,
    Expired
}
