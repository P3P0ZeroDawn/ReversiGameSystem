using System.Collections.Generic;

namespace ReversiGameSystem.Server.Application.Common;

public static class EmailTemplateNames
{
    public const string VerificationLogin = "VerificationLogin";
    public const string VerificationRegistration = "VerificationRegistration";
    public const string VerificationPasswordReset = "VerificationPasswordReset";
    public const string MatchInvitation = "MatchInvitation";
}

public interface ITemplatedEmailSender
{
    void SendTemplatedEmail(
        string recipientAddress,
        string templateName,
        IReadOnlyDictionary<string, string> templateValues);
}
