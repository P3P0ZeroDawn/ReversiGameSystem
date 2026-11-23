using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Security;
using MimeKit;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Persistence.Mail;

public sealed class SmtpEmailSender : IEmailSender
{
    private const int DeliveryTimeoutMilliseconds = 5000;
    private const int MaximumConcurrentDeliveries = 2;
    private static readonly SemaphoreSlim DeliverySlots =
        new SemaphoreSlim(MaximumConcurrentDeliveries);
    private readonly MailSettings _settings;

    public SmtpEmailSender(MailSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void Send(EmailMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        MimeMessage mimeMessage = CreateMimeMessage(message);
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
                DeliverAsync(mimeMessage, deadline.Token).GetAwaiter().GetResult();
            }
        }
        finally
        {
            DeliverySlots.Release();
        }
    }

    private async Task DeliverAsync(MimeMessage message, CancellationToken cancellation)
    {
        using (MailKit.Net.Smtp.SmtpClient client = new MailKit.Net.Smtp.SmtpClient())
        {
            client.Timeout = DeliveryTimeoutMilliseconds;
            SecureSocketOptions security = _settings.EncryptOnConnect
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;
            await client.ConnectAsync(_settings.Host, _settings.Port, security, cancellation)
                .ConfigureAwait(false);
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellation)
                .ConfigureAwait(false);
            await client.SendAsync(message, cancellation).ConfigureAwait(false);
        }
    }

    private MimeMessage CreateMimeMessage(EmailMessage email)
    {
        MimeMessage mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(email.RecipientAddress));
        mimeMessage.Subject = email.Subject;
        mimeMessage.Body = new TextPart("plain")
        {
            Text = email.Body
        };
        return mimeMessage;
    }
}
