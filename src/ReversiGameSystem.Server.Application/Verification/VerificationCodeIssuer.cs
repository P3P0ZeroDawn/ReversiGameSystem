using System;
using System.Globalization;
using System.Security.Cryptography;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Verification;

/// <summary>Generates, protects and delivers a code before it may replace pending state.</summary>
public sealed class VerificationCodeIssuer
{
    private readonly IVerificationEmailSender _sender;
    private readonly VerificationCodeProtector _protector;
    private readonly IUtcClock _clock;

    /// <summary>Creates a code issuer with delivery, protection and server time.</summary>
    /// <param name="sender">The delivery mechanism.</param>
    /// <param name="protector">The keyed digest protector.</param>
    /// <param name="clock">The authoritative UTC clock.</param>
    public VerificationCodeIssuer(IVerificationEmailSender sender,
        VerificationCodeProtector protector, IUtcClock clock)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Delivers a six-digit code valid for five minutes.</summary>
    /// <param name="identity">The authorized action and recipient binding.</param>
    /// <returns>The protected pending state after delivery acceptance.</returns>
    public PendingVerificationCode Issue(VerificationIdentity identity)
    {
        string code = CreateCode();
        DateTime createdAtUtc = _clock.UtcNow;
        DateTime expiresAtUtc = createdAtUtc.AddMinutes(5);
        PendingVerificationCode pending = new PendingVerificationCode
        {
            IdentityKey = identity.Key,
            RecipientAddress = identity.RecipientAddress,
            Purpose = identity.Purpose,
            CreatedAtUtc = createdAtUtc,
            Digest = _protector.Protect(identity.Key, code),
            ExpiresAtUtc = expiresAtUtc
        };
        _sender.Send(new VerificationEmail
        {
            RecipientAddress = identity.RecipientAddress, Code = code,
            Purpose = identity.Purpose, ExpiresAtUtc = expiresAtUtc
        });
        return pending;
    }

    /// <summary>Protects a submitted candidate using the issuer's server key.</summary>
    /// <param name="identity">The authorized action and identity binding.</param>
    /// <param name="code">The submitted candidate.</param>
    /// <returns>The protected candidate digest.</returns>
    public byte[] Protect(VerificationIdentity identity, string code)
    {
        return _protector.Protect(identity.Key, code);
    }

    private static string CreateCode()
    {
        const uint Range = 1000000;
        const uint Limit = uint.MaxValue - (uint.MaxValue % Range);
        byte[] bytes = new byte[4];
        using (RandomNumberGenerator random = RandomNumberGenerator.Create())
        {
            uint value;
            do
            {
                random.GetBytes(bytes);
                value = BitConverter.ToUInt32(bytes, 0);
            }
            while (value >= Limit);
            return (value % Range).ToString("D6", CultureInfo.InvariantCulture);
        }
    }
}
