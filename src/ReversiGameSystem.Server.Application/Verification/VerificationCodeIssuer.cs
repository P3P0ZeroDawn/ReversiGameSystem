using System;
using System.Globalization;
using System.Security.Cryptography;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Application.Verification;

/// <summary>Generates, protects and delivers a code before it may replace pending state.</summary>
public sealed class VerificationCodeIssuer
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);
    private readonly IEmailSender _emailSender;
    private readonly VerificationCodeProtector _protector;
    private readonly IUtcClock _clock;

    /// <summary>Creates a code issuer with delivery, protection and server time.</summary>
    /// <param name="emailSender">The email delivery service.</param>
    /// <param name="protector">The keyed digest protector.</param>
    /// <param name="clock">The authoritative UTC clock.</param>
    public VerificationCodeIssuer(
        IEmailSender emailSender,
        VerificationCodeProtector protector,
        IUtcClock clock)
    {
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
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
        DateTime expiresAtUtc = createdAtUtc.Add(CodeLifetime);
        PendingVerificationCode pending = new PendingVerificationCode
        {
            IdentityKey = identity.Key,
            RecipientAddress = identity.RecipientAddress,
            Purpose = identity.Purpose,
            CreatedAtUtc = createdAtUtc,
            Digest = _protector.Protect(identity.Key, code),
            ExpiresAtUtc = expiresAtUtc
        };

        EmailMessage message = CreateEmailMessage(identity, code, expiresAtUtc);
        _emailSender.Send(message);
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

    private static EmailMessage CreateEmailMessage(
        VerificationIdentity identity,
        string code,
        DateTime expiresAtUtc)
    {
        string purposeLabel = GetPurposeLabel(identity.Purpose);
        string subject = "Reversi: " + purposeLabel;
        string expires = expiresAtUtc.ToUniversalTime()
            .ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
        string body = $"Your verification code / Tu código de verificación: {code}\n"
            + $"Purpose / Propósito: {purposeLabel}\nExpires / Vence: {expires}\n"
            + "Use it once. Do not share it. / Úsalo una vez. No lo compartas.";

        EmailMessage email = new EmailMessage(identity.RecipientAddress, subject, body);
        return email;
    }

    private static string GetPurposeLabel(VerificationPurpose purpose)
    {
        string label;
        switch (purpose)
        {
            case VerificationPurpose.Login:
                label = "sign-in / inicio de sesión";
                break;
            case VerificationPurpose.Registration:
                label = "registration / registro";
                break;
            case VerificationPurpose.PasswordReset:
                label = "password reset / restablecimiento de contraseña";
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(purpose),
                    "Unsupported verification purpose.");
        }

        return label;
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

            string formattedCode = (value % Range).ToString("D6", CultureInfo.InvariantCulture);
            return formattedCode;
        }
    }
}
