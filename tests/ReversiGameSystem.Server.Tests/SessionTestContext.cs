using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests;

internal sealed class SessionTestContext : IUtcClock, IEmailSender,
    IVerificationCodeRepository, IAccountRepository
{
    private static readonly string TestPasswordHash = PasswordHasher.Hash("Password1!");

    internal SessionTestContext()
    {
        Protector = new VerificationCodeProtector(new byte[32]);
        Issuer = new VerificationCodeIssuer(this, Protector, this);
        Codes = new VerificationCodeService(Issuer, this, this);
        Sessions = new SessionStore(this);
        ApplicationService = new SessionApplicationService(
            this,
            Codes,
            Sessions);
    }

    public DateTime UtcNow { get; set; } = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
    internal VerificationCodeProtector Protector { get; }
    internal VerificationCodeIssuer Issuer { get; }
    internal VerificationCodeService Codes { get; }
    internal SessionStore Sessions { get; }
    internal SessionApplicationService ApplicationService { get; }
    internal EmailMessage? Email { get; private set; }
    internal PendingVerificationCode? Pending { get; private set; }
    internal VerificationAttempt? Attempt { get; private set; }
    internal bool FailDelivery { get; set; }
    internal bool Inactive { get; set; }
    internal bool Unavailable { get; set; }
    internal VerificationStatus ConsumeStatus { get; set; } = VerificationStatus.Accepted;

    public void Send(EmailMessage email)
    {
        if (FailDelivery) { throw new TimeoutException("Private provider diagnostic."); }
        Email = email;
    }

    public void Replace(PendingVerificationCode code) => Pending = code;

    public VerificationStatus Consume(VerificationAttempt attempt)
    {
        Attempt = attempt;
        return ConsumeStatus;
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
