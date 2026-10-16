using System;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Persistence.Accounts;

namespace ReversiGameSystem.Tests;

internal sealed class CreateAccountHandlerTests
{
    internal void ExecuteAsync_ValidRequest_StoresAccount()
    {
        // Arrange
        AccountRegistrationStoreStub accountStore = new AccountRegistrationStoreStub
        {
            Result = new AccountRegistrationStoreResult(
                AccountRegistrationStoreOutcome.Success,
                42)
        };
        PasswordHasherStub passwordHasher = new PasswordHasherStub();
        ProfileImageStoreStub imageStore = new ProfileImageStoreStub();
        DateTime registeredAtUtc = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);
        CreateAccountHandler handler = CreateHandler(
            accountStore,
            passwordHasher,
            imageStore,
            registeredAtUtc);

        // Act
        CreateAccountResult result = handler.ExecuteAsync(
            new CreateAccountCommand(
                "  PlayerOne  ",
                "  player@example.com  ",
                "A\u0301b12345!",
                "  en-US  ",
                new ProfileImageData(PngSignature(), "image/png")))
            .GetAwaiter()
            .GetResult();

        // Assert
        TestAssert.AreEqual(CreateAccountOutcome.Success, result.Outcome);
        TestAssert.AreEqual<long?>(42, result.AccountId);
        TestAssert.AreEqual("PlayerOne", accountStore.Registration?.Username);
        TestAssert.AreEqual("player@example.com", accountStore.Registration?.EmailAddress);
        TestAssert.AreEqual("Áb12345!", passwordHasher.HashedPassword);
        TestAssert.AreEqual("hashed-password", accountStore.Registration?.PasswordHash);
        TestAssert.AreEqual("en-US", accountStore.Registration?.PreferredLocaleCode);
        TestAssert.AreEqual("profile-images/test.png", accountStore.Registration?.ProfileImageUri);
        TestAssert.AreEqual(registeredAtUtc, accountStore.Registration?.RegisteredAtUtc);
    }

    internal void ExecuteAsync_MissingRequiredValue_ReturnsInvalidRequest()
    {
        // Arrange
        AccountRegistrationStoreStub accountStore = new AccountRegistrationStoreStub();
        PasswordHasherStub passwordHasher = new PasswordHasherStub();
        ProfileImageStoreStub imageStore = new ProfileImageStoreStub();
        CreateAccountHandler handler = CreateHandler(
            accountStore,
            passwordHasher,
            imageStore,
            DateTime.UtcNow);

        // Act
        CreateAccountResult result = handler.ExecuteAsync(
            new CreateAccountCommand("", "player@example.com", "Password1!", "en-US", null))
            .GetAwaiter()
            .GetResult();

        // Assert
        TestAssert.AreEqual(CreateAccountOutcome.InvalidRequest, result.Outcome);
        TestAssert.AreEqual<AccountRegistrationData?>(null, accountStore.Registration);
        TestAssert.AreEqual<string?>(null, passwordHasher.HashedPassword);
    }

    internal void ExecuteAsync_InvalidFields_ReturnSpecificOutcomes()
    {
        // Arrange
        CreateAccountHandler handler = CreateHandler(
            new AccountRegistrationStoreStub(),
            new PasswordHasherStub(),
            new ProfileImageStoreStub(),
            DateTime.UtcNow);

        // Act
        CreateAccountResult invalidUsername = Execute(
            handler,
            "_player",
            "player@example.com",
            "Password1!");
        CreateAccountResult invalidEmail = Execute(
            handler,
            "PlayerOne",
            "player.example.com",
            "Password1!");
        CreateAccountResult invalidPassword = Execute(
            handler,
            "PlayerOne",
            "player@example.com",
            "password");

        // Assert
        TestAssert.AreEqual(CreateAccountOutcome.InvalidUsername, invalidUsername.Outcome);
        TestAssert.AreEqual(CreateAccountOutcome.InvalidEmailAddress, invalidEmail.Outcome);
        TestAssert.AreEqual(CreateAccountOutcome.InvalidPassword, invalidPassword.Outcome);
    }

    internal void ExecuteAsync_InvalidProfileImage_ReturnsInvalidRequest()
    {
        // Arrange
        ProfileImageStoreStub imageStore = new ProfileImageStoreStub();
        CreateAccountHandler handler = CreateHandler(
            new AccountRegistrationStoreStub(),
            new PasswordHasherStub(),
            imageStore,
            DateTime.UtcNow);

        // Act
        CreateAccountResult result = handler.ExecuteAsync(
            new CreateAccountCommand(
                "PlayerOne",
                "player@example.com",
                "Password1!",
                "en-US",
                new ProfileImageData(new byte[] { 1, 2, 3 }, "image/png")))
            .GetAwaiter()
            .GetResult();

        // Assert
        TestAssert.AreEqual(CreateAccountOutcome.InvalidRequest, result.Outcome);
        TestAssert.AreEqual(false, imageStore.SaveWasCalled);
    }

    internal void ExecuteAsync_DuplicateUsername_RemovesStoredImage()
    {
        // Arrange
        AccountRegistrationStoreStub accountStore = new AccountRegistrationStoreStub
        {
            Result = new AccountRegistrationStoreResult(
                AccountRegistrationStoreOutcome.UsernameAlreadyExists)
        };
        ProfileImageStoreStub imageStore = new ProfileImageStoreStub();
        CreateAccountHandler handler = CreateHandler(
            accountStore,
            new PasswordHasherStub(),
            imageStore,
            DateTime.UtcNow);

        // Act
        CreateAccountResult result = handler.ExecuteAsync(
            new CreateAccountCommand(
                "PlayerOne",
                "player@example.com",
                "Password1!",
                "en-US",
                new ProfileImageData(PngSignature(), "image/png")))
            .GetAwaiter()
            .GetResult();

        // Assert
        TestAssert.AreEqual(CreateAccountOutcome.UsernameAlreadyExists, result.Outcome);
        TestAssert.AreEqual("profile-images/test.png", imageStore.DeletedImageUri);
    }

    internal void ExecuteAsync_UnsupportedLocale_ReturnsInvalidRequest()
    {
        // Arrange
        AccountRegistrationStoreStub accountStore = new AccountRegistrationStoreStub
        {
            Result = new AccountRegistrationStoreResult(
                AccountRegistrationStoreOutcome.UnsupportedLocale)
        };
        CreateAccountHandler handler = CreateHandler(
            accountStore,
            new PasswordHasherStub(),
            new ProfileImageStoreStub(),
            DateTime.UtcNow);

        // Act
        CreateAccountResult result = Execute(
            handler,
            "PlayerOne",
            "player@example.com",
            "Password1!",
            "fr-FR");

        // Assert
        TestAssert.AreEqual(CreateAccountOutcome.InvalidRequest, result.Outcome);
    }

    internal void PasswordHasher_HashAndVerify_UsesSaltedPbkdf2()
    {
        // Arrange
        Pbkdf2PasswordHasher hasher = new Pbkdf2PasswordHasher();

        // Act
        string firstHash = hasher.Hash("Password1!");
        string secondHash = hasher.Hash("Password1!");

        // Assert
        TestAssert.AreEqual(false, firstHash == secondHash);
        TestAssert.AreEqual(true, hasher.Verify("Password1!", firstHash));
        TestAssert.AreEqual(false, hasher.Verify("WrongPassword1!", firstHash));
    }

    private static CreateAccountHandler CreateHandler(
        AccountRegistrationStoreStub accountStore,
        PasswordHasherStub passwordHasher,
        ProfileImageStoreStub imageStore,
        DateTime utcNow)
    {
        return new CreateAccountHandler(
            accountStore,
            passwordHasher,
            imageStore,
            new FixedUtcClock(utcNow));
    }

    private static CreateAccountResult Execute(
        CreateAccountHandler handler,
        string username,
        string emailAddress,
        string password,
        string localeCode = "en-US")
    {
        return handler.ExecuteAsync(
            new CreateAccountCommand(username, emailAddress, password, localeCode, null))
            .GetAwaiter()
            .GetResult();
    }

    private static byte[] PngSignature()
    {
        return new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
    }
}

internal sealed class AccountRegistrationStoreStub : IAccountRegistrationStore
{
    internal AccountRegistrationData? Registration { get; private set; }

    internal AccountRegistrationStoreResult Result { get; set; } =
        new AccountRegistrationStoreResult(AccountRegistrationStoreOutcome.Success, 1);

    public Task<AccountRegistrationStoreResult> RegisterAsync(
        AccountRegistrationData registration)
    {
        Registration = registration;
        return Task.FromResult(Result);
    }
}

internal sealed class PasswordHasherStub : IPasswordHasher
{
    internal string? HashedPassword { get; private set; }

    public string Hash(string password)
    {
        HashedPassword = password;
        return "hashed-password";
    }

    public bool Verify(string password, string encodedHash)
    {
        return false;
    }
}

internal sealed class ProfileImageStoreStub : IProfileImageStore
{
    internal bool SaveWasCalled { get; private set; }

    internal string? DeletedImageUri { get; private set; }

    public ProfileImageStorageResult Save(ProfileImageData image)
    {
        SaveWasCalled = true;
        return new ProfileImageStorageResult(
            ProfileImageStorageOutcome.Success,
            "profile-images/test.png");
    }

    public void Delete(string imageUri)
    {
        DeletedImageUri = imageUri;
    }
}

internal sealed class FixedUtcClock : IUtcClock
{
    public FixedUtcClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; }
}
