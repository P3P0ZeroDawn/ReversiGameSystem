using System;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Persistence.Mail;

namespace ReversiGameSystem.Server.Tests;

internal sealed class SessionTestContext : IUtcClock, IEmailSender, ITemplatedEmailSender,
    IVerificationCodeRepository, IAccountRepository
{
    private static readonly string TestPasswordHash = PasswordHasher.Hash("Password1!");
    private readonly TemplatedEmailSender _templatedSender;

    internal SessionTestContext()
    {
        _templatedSender = new TemplatedEmailSender(new HtmlEmailTemplateRenderer(), this);
        Protector = new VerificationCodeProtector(new byte[32]);
        Codes = new VerificationCodeService(Protector, this, this, this);
        Sessions = new SessionStore(this, NullLogger<SessionStore>.Instance);
        ApplicationService = new SessionApplicationService(
            this,
            Codes,
            Sessions,
            NullLogger<SessionApplicationService>.Instance);
    }

    public DateTime UtcNow { get; set; } = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
    internal VerificationCodeProtector Protector { get; }
    internal VerificationCodeService Codes { get; }
    internal SessionStore Sessions { get; }
    internal SessionApplicationService ApplicationService { get; }
    internal EmailMessage? Email { get; private set; }
    internal PendingVerificationCode? Pending { get; private set; }
    internal StoredVerificationCode Stored { get; private set; } = new StoredVerificationCode();
    internal bool FailDelivery { get; set; }
    internal bool Inactive { get; set; }
    internal bool Unavailable { get; set; }
    internal VerificationStatus? ConsumeStatus { get; set; }

    public void SendTemplatedEmail(
        string recipientAddress,
        string templateName,
        IReadOnlyDictionary<string, string> templateValues)
    {
        _templatedSender.SendTemplatedEmail(
            recipientAddress,
            templateName,
            templateValues);
    }

    public void Send(EmailMessage email)
    {
        if (FailDelivery) { throw new TimeoutException("Private provider diagnostic."); }
        Email = email;
    }

    public void SavePendingCode(PendingVerificationCode pendingCode)
    {
        Pending = pendingCode;
        Stored = new StoredVerificationCode(
            pendingCode.Digest,
            pendingCode.ExpiresAtUtc,
            0,
            null,
            null);
    }

    public VerificationStatus ProcessVerificationAttempt(
        string identityKey,
        Func<StoredVerificationCode, VerificationEvaluationResult> evaluateAttempt)
    {
        if (ConsumeStatus.HasValue)
        {
            return ConsumeStatus.Value;
        }

        VerificationEvaluationResult result = evaluateAttempt(Stored);
        if (Stored.Exists)
        {
            if (result.MarkUsedAtUtc.HasValue)
            {
                Stored.UsedAtUtc = result.MarkUsedAtUtc;
            }

            if (result.UpdatedFailedAttempts.HasValue)
            {
                Stored = new StoredVerificationCode(
                    Stored.Digest,
                    Stored.ExpiresAtUtc,
                    result.UpdatedFailedAttempts.Value,
                    Stored.UsedAtUtc,
                    result.InvalidatedAtUtc);
            }
        }

        return result.Status;
    }

    public AccountResult<LoginAccount> FindByLogin(string login)
    {
        if (Unavailable)
        {
            throw new InvalidOperationException("Account lookup is unavailable.");
        }

        LoginAccount account = new LoginAccount(
            42,
            "PlayerOne",
            "player@example.com",
            TestPasswordHash,
            Inactive ? "INACTIVE" : "ACTIVE",
            "en-US");
        return new AccountResult<LoginAccount>(account);
    }

    public AccountResult<LoginAccount> FindByEmail(string emailAddress)
    {
        throw new NotSupportedException();
    }

    public AccountResult<LoginAccount> FindById(long accountId)
    {
        throw new NotSupportedException();
    }

    public AccountStatus CheckRegistrationAvailability(string username, string emailAddress)
    {
        throw new NotSupportedException();
    }

    public AccountResult<long> CreateAccount(AccountRegistrationData registration)
    {
        throw new NotSupportedException();
    }

    public AccountStatus UpdatePasswordHash(long accountId, string newPasswordHash)
    {
        throw new NotSupportedException();
    }

    public AccountStatus DeactivateAccount(long accountId)
    {
        throw new NotSupportedException();
    }
}
