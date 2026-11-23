using System;

namespace ReversiGameSystem.Server.Application.Verification;

public enum VerificationPurpose
{
    Login,
    Registration,
    PasswordReset
}

public sealed class VerificationEmail
{
    public string RecipientAddress { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public VerificationPurpose Purpose { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class VerificationIdentity
{
    private VerificationIdentity(VerificationPurpose purpose, string key, string recipient)
    {
        Purpose = purpose;
        Key = key;
        RecipientAddress = recipient;
    }

    public VerificationPurpose Purpose { get; }
    public string Key { get; }
    public string RecipientAddress { get; }

    public static VerificationIdentity ForLogin(long accountId, string emailAddress)
    {
        string recipient = emailAddress.Trim();
        string accountKey = accountId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new VerificationIdentity(VerificationPurpose.Login,
            "login:" + accountKey + ":" + recipient.ToUpperInvariant(), recipient);
    }

    public static VerificationIdentity ForRegistration(string username, string emailAddress)
    {
        string name = username.Trim().ToUpperInvariant();
        string email = emailAddress.Trim().ToUpperInvariant();
        return new VerificationIdentity(VerificationPurpose.Registration,
            "registration:" + name.Length + ":" + name + email, emailAddress.Trim());
    }

    public static VerificationIdentity ForPasswordReset(long accountId, string emailAddress)
    {
        string recipient = emailAddress.Trim();
        string accountKey = accountId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string identityKey = "password-reset:" + accountKey + ":" + recipient.ToUpperInvariant();
        VerificationIdentity identity = new VerificationIdentity(
            VerificationPurpose.PasswordReset,
            identityKey,
            recipient);
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

public sealed class VerificationAttempt
{
    public string IdentityKey { get; set; } = string.Empty;
    public byte[] Digest { get; set; } = Array.Empty<byte>();
    public DateTime NowUtc { get; set; }
}

public enum VerificationStatus
{
    Accepted,
    Invalid,
    Expired
}
