using System;

namespace ReversiGameSystem.Server.Application.Verification;

/// <summary>Identifies the action authorized by a verification code.</summary>
public enum VerificationPurpose
{
    /// <summary>Authorizes opening an account session.</summary>
    Login,
    /// <summary>Authorizes creating the proposed account.</summary>
    Registration
}

/// <summary>Contains the verification message accepted by the delivery service.</summary>
public sealed class VerificationEmail
{
    /// <summary>Gets or sets the destination mailbox.</summary>
    public string RecipientAddress { get; set; } = string.Empty;
    /// <summary>Gets or sets the plaintext code used only for delivery.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the authorized action.</summary>
    public VerificationPurpose Purpose { get; set; }
    /// <summary>Gets or sets the UTC code expiry.</summary>
    public DateTime ExpiresAtUtc { get; set; }
}

/// <summary>Identifies one account or the proposed registration identity.</summary>
public sealed class VerificationIdentity
{
    private VerificationIdentity(VerificationPurpose purpose, string key, string recipient)
    {
        Purpose = purpose;
        Key = key;
        RecipientAddress = recipient;
    }

    /// <summary>Gets the authorized action.</summary>
    public VerificationPurpose Purpose { get; }
    /// <summary>Gets the purpose-bound persistent identity key.</summary>
    public string Key { get; }
    /// <summary>Gets the destination mailbox.</summary>
    public string RecipientAddress { get; }

    /// <summary>Binds a login code to a stable account identifier.</summary>
    /// <param name="accountId">The persistent account identifier.</param>
    /// <param name="emailAddress">The account mailbox.</param>
    /// <returns>The login verification identity.</returns>
    public static VerificationIdentity ForLogin(long accountId, string emailAddress)
    {
        string recipient = emailAddress.Trim();
        string accountKey = accountId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new VerificationIdentity(VerificationPurpose.Login,
            "login:" + accountKey + ":" + recipient.ToUpperInvariant(), recipient);
    }

    /// <summary>Binds a registration code to both proposed identity fields.</summary>
    /// <param name="username">The validated proposed account name.</param>
    /// <param name="emailAddress">The validated proposed mailbox.</param>
    /// <returns>The registration verification identity.</returns>
    public static VerificationIdentity ForRegistration(string username, string emailAddress)
    {
        string name = username.Trim().ToUpperInvariant();
        string email = emailAddress.Trim().ToUpperInvariant();
        return new VerificationIdentity(VerificationPurpose.Registration,
            "registration:" + name.Length + ":" + name + email, emailAddress.Trim());
    }
}

/// <summary>Contains the protected code and its validity interval.</summary>
public sealed class PendingVerificationCode
{
    /// <summary>Gets or sets the validated destination mailbox.</summary>
    public string RecipientAddress { get; set; } = string.Empty;
    /// <summary>Gets or sets the authorized action.</summary>
    public VerificationPurpose Purpose { get; set; }
    /// <summary>Gets or sets the UTC issue instant.</summary>
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>Gets or sets the purpose-bound identity.</summary>
    public string IdentityKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the protected code digest.</summary>
    public byte[] Digest { get; set; } = Array.Empty<byte>();
    /// <summary>Gets or sets the UTC expiry.</summary>
    public DateTime ExpiresAtUtc { get; set; }
}

/// <summary>Contains the candidate submitted for an atomic verification.</summary>
public sealed class VerificationAttempt
{
    /// <summary>Gets or sets the purpose-bound identity.</summary>
    public string IdentityKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the protected candidate digest.</summary>
    public byte[] Digest { get; set; } = Array.Empty<byte>();
    /// <summary>Gets or sets the server UTC verification instant.</summary>
    public DateTime NowUtc { get; set; }
}

/// <summary>Identifies the result of a durable verification attempt.</summary>
public enum VerificationStatus
{
    /// <summary>The pending code was consumed by this operation.</summary>
    Accepted,
    /// <summary>The code is incorrect, exhausted, consumed, or absent.</summary>
    Invalid,
    /// <summary>The pending code expired.</summary>
    Expired
}
