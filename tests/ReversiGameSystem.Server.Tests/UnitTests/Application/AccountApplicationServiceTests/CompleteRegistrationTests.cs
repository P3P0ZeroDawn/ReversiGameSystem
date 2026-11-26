using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.AccountApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class CompleteRegistrationTests : AccountApplicationServiceTestBase
{
    [TestMethod]
    public void CompleteRegistration_NullRegistration_ReturnsMissingRegistrationData()
    {
        AccountStatus expectedStatus = AccountStatus.MissingRegistrationData;

        AccountResult<AccountSummary> actualResult = Service.CompleteRegistration(null);

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("ab")]
    public void CompleteRegistration_InvalidUsername_ReturnsInvalidUsernameAndSkipsRepository(
        string username)
    {
        AccountStatus expectedStatus = AccountStatus.InvalidUsername;
        int expectedCallCount = 0;
        NewAccountRegistration registration = new NewAccountRegistration(
            "000001",
            username,
            "player@example.com",
            "Password1!",
            "en-US",
            null);

        AccountResult<AccountSummary> actualResult = Service.CompleteRegistration(registration);

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.AreEqual(expectedCallCount, Accounts.CallCount);
    }

    [TestMethod]
    public void CompleteRegistration_InvalidEmailAddress_ReturnsInvalidEmailAddress()
    {
        AccountStatus expectedStatus = AccountStatus.InvalidEmailAddress;
        NewAccountRegistration registration = new NewAccountRegistration(
            "000001",
            "PlayerOne",
            "invalid",
            "Password1!",
            "en-US",
            null);

        AccountResult<AccountSummary> actualResult = Service.CompleteRegistration(registration);

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void CompleteRegistration_InvalidPassword_ReturnsInvalidPassword()
    {
        AccountStatus expectedStatus = AccountStatus.InvalidPassword;
        NewAccountRegistration registration = new NewAccountRegistration(
            "000001",
            "PlayerOne",
            "player@example.com",
            "weak",
            "en-US",
            null);

        AccountResult<AccountSummary> actualResult = Service.CompleteRegistration(registration);

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("en-US-123456789012345678901234567890")]
    public void CompleteRegistration_InvalidLocale_ReturnsInvalidLocale(string localeCode)
    {
        AccountStatus expectedStatus = AccountStatus.InvalidLocale;
        NewAccountRegistration registration = new NewAccountRegistration(
            "000001",
            "PlayerOne",
            "player@example.com",
            "Password1!",
            localeCode,
            null);

        AccountResult<AccountSummary> actualResult = Service.CompleteRegistration(registration);

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void CompleteRegistration_UnsupportedImage_ReturnsInvalidImageAndSkipsImageSave()
    {
        AccountStatus expectedStatus = AccountStatus.InvalidImage;
        int expectedSaveCount = 0;
        NewAccountRegistration registration = new NewAccountRegistration(
            "000001",
            "PlayerOne",
            "player@example.com",
            "Password1!",
            "en-US",
            new ProfileImageData(new byte[] { 1, 2, 3 }, "image/png"));

        AccountResult<AccountSummary> actualResult = Service.CompleteRegistration(registration);

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.AreEqual(expectedSaveCount, Images.SaveCount);
    }

    [TestMethod]
    public void CompleteRegistration_InvalidVerificationCode_ReturnsInvalidVerificationCode()
    {
        VerificationCodes.VerificationResult = VerificationStatus.Invalid;
        AccountStatus expectedStatus = AccountStatus.InvalidVerificationCode;
        NewAccountRegistration registration = new NewAccountRegistration(
            "000001",
            "PlayerOne",
            "player@example.com",
            "Password1!",
            "en-US",
            null);

        AccountResult<AccountSummary> actualResult = Service.CompleteRegistration(registration);

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void CompleteRegistration_ExpiredVerificationCode_ReturnsExpiredVerificationCode()
    {
        VerificationCodes.VerificationResult = VerificationStatus.Expired;
        AccountStatus expectedStatus = AccountStatus.ExpiredVerificationCode;
        NewAccountRegistration registration = new NewAccountRegistration(
            "000001",
            "PlayerOne",
            "player@example.com",
            "Password1!",
            "en-US",
            null);

        AccountResult<AccountSummary> actualResult = Service.CompleteRegistration(registration);

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void CompleteRegistration_ImageStorageRejected_ReturnsInvalidImage()
    {
        Images.SaveResult = new ProfileResult<string>(ProfileStatus.InvalidImage);
        AccountStatus expectedStatus = AccountStatus.InvalidImage;
        NewAccountRegistration registration = new NewAccountRegistration(
            "000001",
            "PlayerOne",
            "player@example.com",
            "Password1!",
            "en-US",
            new ProfileImageData(CreatePngBytes(), "image/png"));

        AccountResult<AccountSummary> actualResult = Service.CompleteRegistration(registration);

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void CompleteRegistration_CreateFailsAfterImageSave_DeletesOrphanedImage()
    {
        Accounts.CreateAccountResult = new AccountResult<long>(AccountStatus.UsernameUnavailable);
        AccountStatus expectedStatus = AccountStatus.UsernameUnavailable;
        string expectedDeletedUri = TestDoubles.PlayerImageRepositoryStub.DefaultImageReference;
        NewAccountRegistration registration = new NewAccountRegistration(
            "000001",
            "PlayerOne",
            "player@example.com",
            "Password1!",
            "en-US",
            new ProfileImageData(CreatePngBytes(), "image/png"));

        AccountResult<AccountSummary> actualResult = Service.CompleteRegistration(registration);

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.AreEqual(expectedDeletedUri, Images.DeletedReference);
    }

    [TestMethod]
    public void CompleteRegistration_ValidRegistration_ReturnsCreatedAccountId()
    {
        Accounts.CreateAccountResult = new AccountResult<long>(42);
        long expectedAccountId = 42;
        NewAccountRegistration registration = new NewAccountRegistration(
            "000001",
            "  PlayerOne  ",
            "  player@example.com  ",
            "A\u0301b12345!",
            "  en-US  ",
            new ProfileImageData(CreatePngBytes(), "image/png"));

        AccountResult<AccountSummary> actualResult = Service.CompleteRegistration(registration);

        Assert.AreEqual(expectedAccountId, actualResult.Value?.AccountId);
    }

    [TestMethod]
    public void CompleteRegistration_ValidRegistration_NormalizesUsernameForPersistence()
    {
        string expectedUsername = "PlayerOne";
        NewAccountRegistration registration = new NewAccountRegistration(
            "000001",
            "  PlayerOne  ",
            "  player@example.com  ",
            "A\u0301b12345!",
            "  en-US  ",
            null);

        Service.CompleteRegistration(registration);

        Assert.AreEqual(expectedUsername, Accounts.LastRegistration?.Username);
    }

    [TestMethod]
    public void CompleteRegistration_ValidRegistration_HashesNormalizedPassword()
    {
        NewAccountRegistration registration = new NewAccountRegistration(
            "000001",
            "PlayerOne",
            "player@example.com",
            "A\u0301b12345!",
            "en-US",
            null);

        Service.CompleteRegistration(registration);

        bool actualVerified = PasswordHasher.Verify(
            "\u00C1b12345!",
            Accounts.LastRegistration!.PasswordHash);
        Assert.IsTrue(actualVerified);
    }

    [TestMethod]
    public void CompleteRegistration_ValidRegistrationWithImage_PersistsSavedImageUri()
    {
        string expectedImageUri = TestDoubles.PlayerImageRepositoryStub.DefaultImageReference;
        NewAccountRegistration registration = new NewAccountRegistration(
            "000001",
            "PlayerOne",
            "player@example.com",
            "Password1!",
            "en-US",
            new ProfileImageData(CreatePngBytes(), "image/png"));

        Service.CompleteRegistration(registration);

        Assert.AreEqual(expectedImageUri, Accounts.LastRegistration?.ProfileImageUri);
    }
}
