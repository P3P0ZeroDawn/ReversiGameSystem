using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.VerificationCodeServiceTests;

public abstract class VerificationCodeServiceTestBase
{
    private protected VerificationCodeProtector Protector { get; private set; } = null!;

    private protected TestDoubles.VerificationCodeRepositoryStub Repository { get; private set; } =
        null!;

    private protected TemplatedEmailSenderStub EmailSender { get; private set; } = null!;

    private protected UtcClockStub Clock { get; private set; } = null!;

    private protected VerificationCodeService Service { get; private set; } = null!;

    [TestInitialize]
    public void SetUp()
    {
        Protector = new VerificationCodeProtector(new byte[32]);
        Repository = new TestDoubles.VerificationCodeRepositoryStub();
        EmailSender = new TemplatedEmailSenderStub();
        Clock = new UtcClockStub(new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc));
        Service = new VerificationCodeService(Protector, Repository, EmailSender, Clock);
    }

    private protected static VerificationIdentity CreateLoginIdentity()
    {
        return VerificationIdentity.ForLogin(42, "player@example.com", "es-MX");
    }

    private protected StoredVerificationCode CreateStoredCode(
        VerificationIdentity identity,
        string code = "000012",
        int failedAttempts = 0,
        DateTime? usedAtUtc = null,
        DateTime? invalidatedAtUtc = null,
        DateTime? expiresAtUtc = null)
    {
        byte[] digest = Protector.ComputeCodeDigest(identity.Key, code);
        return new StoredVerificationCode(
            digest,
            expiresAtUtc ?? Clock.UtcNow.AddMinutes(5),
            failedAttempts,
            usedAtUtc,
            invalidatedAtUtc);
    }
}
