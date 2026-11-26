using System;

namespace ReversiGameSystem.Server.Application.Common;

public sealed class EmailMessage
{
    public EmailMessage(
        string recipientAddress,
        string subject,
        string body,
        bool isHtml = false)
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
        IsHtml = isHtml;
    }

    public string RecipientAddress { get; }

    public string Subject { get; }

    public string Body { get; }

    public bool IsHtml { get; }
}

public interface IEmailSender
{
    public void Send(EmailMessage message);
}
