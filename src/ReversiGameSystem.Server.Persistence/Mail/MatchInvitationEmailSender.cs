using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Security;
using MimeKit;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Persistence.Mail;

/// <summary>
/// Delivers private match invitation messages through the configured mail transport.
/// </summary>
public sealed class MatchInvitationEmailSender : IMatchInvitationEmailSender
{
    private const int DeliveryTimeoutMilliseconds = 5000;
    private const int MaximumConcurrentDeliveries = 2;
    private static readonly SemaphoreSlim DeliverySlots =
        new SemaphoreSlim(MaximumConcurrentDeliveries);
    private readonly MailSettings _settings;

    /// <summary>Creates an invitation sender with configuration supplied by the composition root.</summary>
    /// <param name="settings">The validated delivery settings.</param>
    public MatchInvitationEmailSender(MailSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public void Send(MatchInvitationEmail email)
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

    private MimeMessage CreateMessage(MatchInvitationEmail email)
    {
        MimeMessage message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(email.RecipientEmail));
        message.Subject = "Reversi — Invitación a partida / Match invitation";
        message.Body = new TextPart("plain")
        {
            Text = "Hello / Hola,\n\n"
                + email.CreatorDisplayName + " has invited you to play a match of Reversi. / "
                + email.CreatorDisplayName + " te ha invitado a jugar una partida de Reversi.\n\n"
                + "Match code / Código de partida: " + email.MatchCode + "\n\n"
                + "Instructions / Instrucciones:\n"
                + "1. Sign in to your Reversi account. / Inicia sesión en tu cuenta de Reversi.\n"
                + "2. Go to 'Join with code' in Available Matches and enter this code. / "
                + "Ve a 'Unirse con código' en Partidas Disponibles e ingresa este código.\n\n"
                + "«Puedes unirte con este código mientras el creador siga esperando» / "
                + "«You can join with this code while the creator is still waiting»\n"
        };
        return message;
    }
}
