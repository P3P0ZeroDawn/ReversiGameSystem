using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.AccountApplicationServiceTests;

public abstract class AccountApplicationServiceTestBase
{
    private protected AccountRepositoryStub Accounts { get; private set; } = null!;

    private protected TestDoubles.PlayerImageRepositoryStub Images { get; private set; } = null!;

    private protected TestDoubles.VerificationCodeRepositoryStub VerificationCodes
    {
        get;
        private set;
    } = null!;

    private protected TemplatedEmailSenderStub EmailSender { get; private set; } = null!;

    private protected UtcClockStub Clock { get; private set; } = null!;

    private protected SessionStore Sessions { get; private set; } = null!;

    private protected VerificationCodeService VerificationService { get; private set; } = null!;

    private protected AccountApplicationService Service { get; private set; } = null!;

    [TestInitialize]
    public void SetUp()
    {
        Accounts = new AccountRepositoryStub();
        Images = new TestDoubles.PlayerImageRepositoryStub();
        VerificationCodes = new TestDoubles.VerificationCodeRepositoryStub();
        EmailSender = new TemplatedEmailSenderStub();
        Clock = new UtcClockStub(new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc));
        Sessions = new SessionStore(Clock);
        VerificationService = new VerificationCodeService(
            new VerificationCodeProtector(new byte[32]),
            VerificationCodes,
            EmailSender,
            Clock);
        Service = new AccountApplicationService(
            Accounts,
            Images,
            VerificationService,
            Sessions);
    }

    private protected static byte[] CreatePngBytes()
    {
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
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

    private protected AuthenticatedSession CreateSession(
        long accountId = 42,
        string username = "PlayerOne")
    {
        return TestSessionHelper.CreateSession(Sessions, accountId, username);
    }
}
