using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Persistence.Model;
using ReversiGameSystem.Server.Persistence.Verification;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
[TestCategory("Database")]
public sealed class VerificationCodeRepositoryIntegrationTests : RepositoryIntegrationTestBase
{
    private const string SampleIdentityKey = "ACCOUNT_CREATION:player@example.com";
    private const string SampleRecipient = "player@example.com";
    private const string LoginPurposeCode = "LOGIN";
    private const int DigestLength = 32;

    private static LocalDbTestDatabase? _database;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _database = InitializeDatabase();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        CleanupDatabase(_database);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _database?.ResetMutableTables();
    }

    [TestMethod]
    public void SavePendingCode_SameIdentityTwice_UpsertsSingleRowAndResetsState()
    {
        VerificationCodeRepository repository = new VerificationCodeRepository();
        DateTime createdAtUtc = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
        byte[] initialDigest = CreateDigest(0x11);
        byte[] replacementDigest = CreateDigest(0x22);
        int expectedRowCount = 1;
        int expectedFailedAttempts = 0;

        repository.SavePendingCode(new PendingVerificationCode
        {
            IdentityKey = SampleIdentityKey,
            RecipientAddress = SampleRecipient,
            Purpose = VerificationPurpose.Registration,
            Digest = initialDigest,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = createdAtUtc.AddMinutes(10)
        });
        repository.ProcessVerificationAttempt(
            SampleIdentityKey,
            stored => new VerificationEvaluationResult(1, null));
        repository.SavePendingCode(new PendingVerificationCode
        {
            IdentityKey = SampleIdentityKey,
            RecipientAddress = SampleRecipient,
            Purpose = VerificationPurpose.Login,
            Digest = replacementDigest,
            CreatedAtUtc = createdAtUtc.AddMinutes(1),
            ExpiresAtUtc = createdAtUtc.AddMinutes(11)
        });

        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        VerificationCode[] actualRows = verificationContext.VerificationCode
            .Where(row => row.identityKey == SampleIdentityKey)
            .ToArray();
        Assert.AreEqual(expectedRowCount, actualRows.Length);
        Assert.AreEqual(LoginPurposeCode, actualRows[0].purposeCode);
        Assert.AreEqual(expectedFailedAttempts, actualRows[0].failedAttempts);
        Assert.IsNull(actualRows[0].usedAtUtc);
        Assert.IsNull(actualRows[0].invalidatedAtUtc);
    }

    [TestMethod]
    public void ProcessVerificationAttempt_ValidEvaluation_MarksCodeUsedInDatabase()
    {
        VerificationCodeRepository repository = new VerificationCodeRepository();
        DateTime createdAtUtc = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime expectedUsedAtUtc = createdAtUtc.AddMinutes(2);
        byte[] expectedDigest = CreateDigest(0x33);
        VerificationStatus expectedStatus = VerificationStatus.Accepted;
        repository.SavePendingCode(new PendingVerificationCode
        {
            IdentityKey = SampleIdentityKey,
            RecipientAddress = SampleRecipient,
            Purpose = VerificationPurpose.PasswordReset,
            Digest = expectedDigest,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = createdAtUtc.AddMinutes(10)
        });

        byte[]? observedDigest = null;
        VerificationStatus actualStatus = repository.ProcessVerificationAttempt(
            SampleIdentityKey,
            stored => EvaluateAndCaptureDigest(
                stored,
                expectedUsedAtUtc,
                digest => observedDigest = digest));

        Assert.AreEqual(expectedStatus, actualStatus);
        CollectionAssert.AreEqual(expectedDigest, observedDigest);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        DateTime? actualUsedAtUtc = verificationContext.VerificationCode
            .Single(row => row.identityKey == SampleIdentityKey)
            .usedAtUtc;
        Assert.AreEqual(expectedUsedAtUtc, actualUsedAtUtc);
    }

    [TestMethod]
    public void ProcessVerificationAttempt_RejectedWithInvalidation_PersistsAttemptsAndTimestamp()
    {
        VerificationCodeRepository repository = new VerificationCodeRepository();
        DateTime createdAtUtc = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime expectedInvalidatedAtUtc = createdAtUtc.AddMinutes(3);
        int expectedFailedAttempts = 5;
        VerificationStatus expectedStatus = VerificationStatus.Invalid;
        repository.SavePendingCode(new PendingVerificationCode
        {
            IdentityKey = SampleIdentityKey,
            RecipientAddress = SampleRecipient,
            Purpose = VerificationPurpose.Registration,
            Digest = CreateDigest(0x44),
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = createdAtUtc.AddMinutes(10)
        });

        VerificationStatus actualStatus = repository.ProcessVerificationAttempt(
            SampleIdentityKey,
            stored => new VerificationEvaluationResult(
                expectedFailedAttempts,
                expectedInvalidatedAtUtc));

        Assert.AreEqual(expectedStatus, actualStatus);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        VerificationCode actualPersisted = verificationContext.VerificationCode
            .Single(row => row.identityKey == SampleIdentityKey);
        Assert.AreEqual(expectedFailedAttempts, actualPersisted.failedAttempts);
        Assert.AreEqual(expectedInvalidatedAtUtc, actualPersisted.invalidatedAtUtc);
    }

    private static VerificationEvaluationResult EvaluateAndCaptureDigest(
        StoredVerificationCode stored,
        DateTime usedAtUtc,
        Action<byte[]> captureDigest)
    {
        captureDigest(stored.Digest);
        return new VerificationEvaluationResult(usedAtUtc);
    }

    private static byte[] CreateDigest(byte fillByte)
    {
        byte[] digest = new byte[DigestLength];
        for (int index = 0; index < digest.Length; index++)
        {
            digest[index] = fillByte;
        }

        return digest;
    }
}
