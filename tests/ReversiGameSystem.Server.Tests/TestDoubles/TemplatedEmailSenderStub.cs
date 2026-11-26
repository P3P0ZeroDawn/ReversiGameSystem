using System;
using System.Collections.Generic;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Tests.TestDoubles;

internal sealed class TemplatedEmailSenderStub : ITemplatedEmailSender
{
    internal Exception? Failure { get; set; }

    internal string? LastRecipientAddress { get; private set; }

    internal string? LastTemplateName { get; private set; }

    internal IReadOnlyDictionary<string, string>? LastTemplateValues { get; private set; }

    internal int SendCallCount { get; private set; }

    public void SendTemplatedEmail(
        string recipientAddress,
        string templateName,
        IReadOnlyDictionary<string, string> templateValues)
    {
        SendCallCount++;
        LastRecipientAddress = recipientAddress;
        LastTemplateName = templateName;
        Dictionary<string, string> copiedValues = new Dictionary<string, string>(
            StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> entry in templateValues)
        {
            copiedValues[entry.Key] = entry.Value;
        }

        LastTemplateValues = copiedValues;
        if (Failure != null)
        {
            throw Failure;
        }
    }
}
