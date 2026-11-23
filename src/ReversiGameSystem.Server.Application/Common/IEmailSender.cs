using System;

namespace ReversiGameSystem.Server.Application.Common;

/// <summary>
/// Represents an outgoing plain-text email message.
/// </summary>
public sealed class EmailMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailMessage"/> class.
    /// </summary>
    /// <param name="recipientAddress">The destination email address.</param>
    /// <param name="subject">The email subject line.</param>
    /// <param name="body">The plain-text email body.</param>
    /// <exception cref="ArgumentException">
    /// A required email field is null or whitespace.
    /// </exception>
    public EmailMessage(string recipientAddress, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(recipientAddress))
        {
            throw new ArgumentException(
                "A recipient email address is required.",
                nameof(recipientAddress));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException(
                "An email subject is required.",
                nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException(
                "An email body is required.",
                nameof(body));
        }

        RecipientAddress = recipientAddress;
        Subject = subject;
        Body = body;
    }

    /// <summary>Gets the destination email address.</summary>
    public string RecipientAddress { get; }

    /// <summary>Gets the email subject line.</summary>
    public string Subject { get; }

    /// <summary>Gets the plain-text email body.</summary>
    public string Body { get; }
}

/// <summary>
/// Delivers outgoing email messages through the configured mail transport.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email message synchronously.
    /// </summary>
    /// <param name="message">The email message to deliver.</param>
    void Send(EmailMessage message);
}
