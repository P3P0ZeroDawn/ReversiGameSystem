using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class CreateAccountHandlerTests
{
    [TestMethod]
    public void ExecuteAsync_ValidRequest_StoresAccount()
    {
        // Arrange
        AccountRepositoryStub accountStore = new AccountRepositoryStub
        {
            CreateResult = new AccountResult<long>(42)
        };
        PlayerImageRepositoryStub imageStore = new PlayerImageRepositoryStub();
        AccountApplicationService service = CreateService(accountStore, imageStore);

        // Act
        AccountResult<AccountSummary> result = service.CompleteRegistration(
            new NewAccountRegistration(
                "000001",
                "  PlayerOne  ",
                "  player@example.com  ",
                "A\u0301b12345!",
                "  en-US  ",
                new ProfileImageData(PngSignature(), "image/png")));

        // Assert
        Assert.AreEqual(AccountStatus.Success, result.Status);
        Assert.AreEqual<long?>(42, result.Value?.AccountId);
        Assert.AreEqual("PlayerOne", accountStore.Registration?.Username);
        Assert.AreEqual("player@example.com", accountStore.Registration?.EmailAddress);
        Assert.IsTrue(
            PasswordHasher.Verify("Áb12345!", accountStore.Registration!.PasswordHash),
            "Stored PBKDF2 hash must verify the normalized password.");
        Assert.AreEqual("en-US", accountStore.Registration?.PreferredLocaleCode);
        Assert.AreEqual("new-image", accountStore.Registration?.ProfileImageUri);
    }

    [TestMethod]
    public void ExecuteAsync_MissingRequiredValue_ReturnsInvalidRequest()
    {
        // Arrange
        AccountRepositoryStub accountStore = new AccountRepositoryStub();
        PlayerImageRepositoryStub imageStore = new PlayerImageRepositoryStub();
        AccountApplicationService service = CreateService(accountStore, imageStore);

        // Act
        AccountResult<AccountSummary> result = service.CompleteRegistration(
            new NewAccountRegistration("000001", "", "player@example.com", "Password1!", "en-US", null));

        // Assert
        Assert.AreEqual(AccountStatus.InvalidUsername, result.Status);
        Assert.AreEqual<AccountRegistrationData?>(null, accountStore.Registration);
    }

    [TestMethod]
    public void ExecuteAsync_InvalidFields_ReturnSpecificOutcomes()
    {
        // Arrange
        AccountRepositoryStub accountStore = new AccountRepositoryStub();
        AccountApplicationService service = CreateService(accountStore, new PlayerImageRepositoryStub());

        // Act & Assert
        Assert.AreEqual(
            AccountStatus.InvalidUsername,
            service.CompleteRegistration(
                new NewAccountRegistration("000001", "ab", "player@example.com", "Password1!", "en-US", null)).Status);
        Assert.AreEqual(
            AccountStatus.InvalidEmailAddress,
            service.CompleteRegistration(
                new NewAccountRegistration("000001", "PlayerOne", "invalid", "Password1!", "en-US", null)).Status);
        Assert.AreEqual(
            AccountStatus.InvalidPassword,
            service.CompleteRegistration(
                new NewAccountRegistration("000001", "PlayerOne", "player@example.com", "weak", "en-US", null)).Status);
    }

    [TestMethod]
    public void ExecuteAsync_InvalidProfileImage_ReturnsInvalidRequest()
    {
        // Arrange
        AccountRepositoryStub accountStore = new AccountRepositoryStub();
        AccountApplicationService service = CreateService(accountStore, new PlayerImageRepositoryStub());

        // Act
        AccountResult<AccountSummary> result = service.CompleteRegistration(
            new NewAccountRegistration(
                "000001",
                "PlayerOne",
                "player@example.com",
                "Password1!",
                "en-US",
                new ProfileImageData(new byte[] { 1, 2, 3 }, "image/png")));

        // Assert
        Assert.AreEqual(AccountStatus.InvalidImage, result.Status);
    }

    [TestMethod]
    public void ExecuteAsync_DuplicateUsername_RemovesStoredImage()
    {
        // Arrange
        AccountRepositoryStub accountStore = new AccountRepositoryStub
        {
            CreateResult = new AccountResult<long>(AccountStatus.UsernameUnavailable)
        };
        PlayerImageRepositoryStub imageStore = new PlayerImageRepositoryStub();
        AccountApplicationService service = CreateService(accountStore, imageStore);

        // Act
        AccountResult<AccountSummary> result = service.CompleteRegistration(
            new NewAccountRegistration(
                "000001",
                "PlayerOne",
                "player@example.com",
                "Password1!",
                "en-US",
                new ProfileImageData(PngSignature(), "image/png")));

        // Assert
        Assert.AreEqual(AccountStatus.UsernameUnavailable, result.Status);
        Assert.AreEqual("new-image", imageStore.Deleted);
    }

    [TestMethod]
    public void ExecuteAsync_UnsupportedLocale_ReturnsInvalidRequest()
    {
        // Arrange
        AccountRepositoryStub accountStore = new AccountRepositoryStub
        {
            CreateResult = new AccountResult<long>(AccountStatus.InvalidLocale)
        };
        AccountApplicationService service = CreateService(accountStore, new PlayerImageRepositoryStub());

        // Act
        AccountResult<AccountSummary> result = service.CompleteRegistration(
            new NewAccountRegistration(
                "000001",
                "PlayerOne",
                "player@example.com",
                "Password1!",
                "fr-FR",
                null));

        // Assert
        Assert.AreEqual(AccountStatus.InvalidLocale, result.Status);
    }

    [TestMethod]
    public void PasswordHasher_HashAndVerify_UsesSaltedPbkdf2()
    {
        // Act
        string firstHash = PasswordHasher.Hash("Password1!");
        string secondHash = PasswordHasher.Hash("Password1!");

        // Assert
        Assert.IsTrue(
            firstHash.StartsWith("PBKDF2-SHA256$100000$", StringComparison.Ordinal),
            "Hash must use PBKDF2-SHA256 with 100000 iterations.");
        Assert.IsTrue(firstHash != secondHash, "Salts must produce distinct hashes.");
        Assert.AreEqual(true, PasswordHasher.Verify("Password1!", firstHash));
        Assert.AreEqual(false, PasswordHasher.Verify("Wrong1!", firstHash));
    }

    private static AccountApplicationService CreateService(
        IAccountRepository accounts,
        IPlayerImageRepository images)
    {
        IUtcClock clock = new TestUtcClock(new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc));
        VerificationCodeService verificationCodes = new VerificationCodeService(
            new VerificationCodeProtector(new byte[32]),
            new VerificationCodeRepositoryStub(),
            new VerificationEmailSenderStub(),
            clock);
        return new AccountApplicationService(
            accounts,
            images,
            verificationCodes,
            new SessionStore(clock));
    }

    private static byte[] PngSignature()
    {
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    }

    private sealed class AccountRepositoryStub : IAccountRepository
    {
        internal AccountRegistrationData? Registration { get; private set; }

        internal AccountResult<long> CreateResult { get; set; } =
            new AccountResult<long>(1);

        public AccountResult<LoginAccount> FindByLogin(string loginIdentifier)
        {
            return new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        }

        public AccountResult<LoginAccount> FindByEmail(string emailAddress)
        {
            return new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        }

        public AccountResult<LoginAccount> FindById(long accountId)
        {
            return new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        }

        public AccountStatus CheckRegistrationAvailability(string username, string emailAddress)
        {
            return AccountStatus.Success;
        }

        public AccountResult<long> CreateAccount(AccountRegistrationData registration)
        {
            Registration = registration;
            return CreateResult;
        }

        public AccountStatus UpdatePasswordHash(long accountId, string newPasswordHash)
        {
            return AccountStatus.Success;
        }

        public AccountStatus DeactivateAccount(long accountId)
        {
            return AccountStatus.Success;
        }
    }
}
