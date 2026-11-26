using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.SessionApplicationServiceTests;

public abstract class SessionApplicationServiceTestBase
{
    private protected AccountRepositoryStub Accounts { get; private set; } = null!;

    private protected TestDoubles.VerificationCodeRepositoryStub VerificationCodes
    {
        get;
        private set;
    } = null!;

    private protected TemplatedEmailSenderStub EmailSender { get; private set; } = null!;

    private protected UtcClockStub Clock { get; private set; } = null!;

    private protected SessionStore Sessions { get; private set; } = null!;

    private protected VerificationCodeService VerificationService { get; private set; } = null!;

    private protected SessionApplicationService Service { get; private set; } = null!;

    [TestInitialize]
    public void SetUp()
    {
        Accounts = new AccountRepositoryStub();
        VerificationCodes = new TestDoubles.VerificationCodeRepositoryStub();
        EmailSender = new TemplatedEmailSenderStub();
        Clock = new UtcClockStub(new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc));
        Sessions = new SessionStore(Clock);
        VerificationService = new VerificationCodeService(
            new VerificationCodeProtector(new byte[32]),
            VerificationCodes,
            EmailSender,
            Clock);
        Service = new SessionApplicationService(Accounts, VerificationService, Sessions);
    }

    private protected static LoginAccount CreateLoginAccount(
        long accountId = 42,
        string password = "Password1!",
        string statusCode = "ACTIVE")
    {
        string passwordHash = PasswordHasher.Hash(password);
        return new LoginAccount(
            accountId,
            "PlayerOne",
            "player@example.com",
            passwordHash,
            statusCode,
            "en-US");
    }
}
