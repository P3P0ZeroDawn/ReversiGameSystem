using System;
using System.Collections.Generic;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Persistence.Mail;

public sealed class TemplatedEmailSender : ITemplatedEmailSender
{
    private readonly HtmlEmailTemplateRenderer _templateRenderer;
    private readonly IEmailSender _emailSender;

    public TemplatedEmailSender(
        HtmlEmailTemplateRenderer templateRenderer,
        IEmailSender emailSender)
    {
        _templateRenderer = templateRenderer
            ?? throw new ArgumentNullException(nameof(templateRenderer));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
    }

    public void SendTemplatedEmail(
        string recipientAddress,
        string templateName,
        IReadOnlyDictionary<string, string> templateValues)
    {
        RenderedEmailTemplate rendered = _templateRenderer.Render(templateName, templateValues);
        EmailMessage emailMessage = new EmailMessage(
            recipientAddress,
            rendered.Subject,
            rendered.HtmlBody,
            isHtml: true);
        _emailSender.Send(emailMessage);
    }
}
