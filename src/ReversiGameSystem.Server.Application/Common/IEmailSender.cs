using System;

namespace ReversiGameSystem.Server.Application.Common;

public sealed class EmailMessage
{
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

    public string RecipientAddress { get; }

    public string Subject { get; }

    public string Body { get; }
}

public interface IEmailSender
{
    void Send(EmailMessage message);
}
