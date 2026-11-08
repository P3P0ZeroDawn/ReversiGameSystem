using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Security;
using MimeKit;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Persistence.Mail;

/// <summary>
/// Delivers verification messages through the configured protected mail transport.
/// </summary>
public sealed class VerificationEmailSender : IVerificationEmailSender
{
    private const int DeliveryTimeoutMilliseconds = 5000;
    private const int MaximumConcurrentDeliveries = 2;
    private static readonly SemaphoreSlim DeliverySlots =
        new SemaphoreSlim(MaximumConcurrentDeliveries);
    private readonly MailSettings _settings;

    /// <summary>Creates a sender with configuration supplied by the composition root.</summary>
    /// <param name="settings">The validated delivery settings.</param>
    public VerificationEmailSender(MailSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public void Send(VerificationEmail email)
    {
        if (email == null)
        {
            throw new ArgumentNullException(nameof(email));
        }

        MimeMessage message = CreateMessage(email);
        if (!DeliverySlots.Wait(0))
        {
            throw new InvalidOperationException(
                "The mail delivery capacity is temporarily occupied.");
        }

        try
        {
            using (CancellationTokenSource deadline = new CancellationTokenSource(
                DeliveryTimeoutMilliseconds))
            {
                Deliver(message, deadline.Token).GetAwaiter().GetResult();
            }
        }
        finally
        {
            DeliverySlots.Release();
        }
    }

    private async Task Deliver(MimeMessage message, CancellationToken cancellation)
    {
        using (MailKit.Net.Smtp.SmtpClient client = new MailKit.Net.Smtp.SmtpClient())
        {
            client.Timeout = DeliveryTimeoutMilliseconds;
            SecureSocketOptions security = _settings.EncryptOnConnect
                ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await client.ConnectAsync(_settings.Host, _settings.Port, security, cancellation)
                .ConfigureAwait(false);
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellation)
                .ConfigureAwait(false);
            await client.SendAsync(message, cancellation).ConfigureAwait(false);
        }
    }

    private MimeMessage CreateMessage(VerificationEmail email)
    {
        string purpose = email.Purpose switch
        {
            VerificationPurpose.Login => "sign-in / inicio de sesión",
            VerificationPurpose.Registration => "registration / registro",
            _ => throw new ArgumentOutOfRangeException(nameof(email.Purpose))
        };
        MimeMessage message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(email.RecipientAddress));
        message.Subject = "Reversi: " + purpose;
        string expires = email.ExpiresAtUtc.ToUniversalTime()
            .ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
        message.Body = new TextPart("plain")
        {
            Text = $"Your verification code / Tu código de verificación: {email.Code}\n"
                + $"Purpose / Propósito: {purpose}\nExpires / Vence: {expires}\n"
                + "Use it once. Do not share it. / Úsalo una vez. No lo compartas."
        };
        return message;
    }
}
