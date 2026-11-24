using System;
using System.Collections.Generic;
using System.IO;
using ReversiGameSystem.Server.Domain.Accounts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;

namespace ReversiGameSystem.Server.Tests;

/// <summary>Verifies the player profile application operations in isolation.</summary>
[TestClass]
public sealed class PlayerProfileApplicationServiceTests
{
    [TestMethod]
    public void GetProfile_CurrentPlayer_ReturnsCalculatedStatistics()
    {
        // Arrange
        SessionStore sessions = CreateSessionStore(CreateSession(42));
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        profiles.Profiles.Add(42, new PlayerProfile(
            42,
            "PlayerOne",
            "profile-images/player.png",
            7,
            3,
            2));
        PlayerProfileApplicationService applicationService = CreateService(sessions, profiles);

        // Act
        ProfileResult<PlayerProfile> result = applicationService.GetProfile("session-token", null);

        // Assert
        Assert.AreEqual(ProfileStatus.Success, result.Status);
        Assert.AreEqual(1, profiles.AccountIds.Count);
        Assert.AreEqual(42L, profiles.AccountIds[0]);
        Assert.AreEqual("PlayerOne", result.Value?.Username);
        Assert.AreEqual("profile-images/player.png", result.Value?.ProfileImageUri);
        Assert.AreEqual(12, result.Value?.MatchesPlayed);
        Assert.AreEqual(7, result.Value?.Wins);
        Assert.AreEqual(3, result.Value?.Losses);
        Assert.AreEqual(2, result.Value?.Draws);
        Assert.AreEqual(7d / 12d, result.Value?.WinRate);
    }

    [TestMethod]
    public void GetProfile_PublicProfile_VerifiesCallerThenUsesTargetAccount()
    {
        // Arrange
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        profiles.Profiles.Add(42, new PlayerProfile(42, "PlayerOne", null, 1, 0, 0));
        profiles.Profiles.Add(99, new PlayerProfile(99, "OtherPlayer", null, 0, 0, 0));
        PlayerProfileApplicationService applicationService = CreateService(
            CreateSessionStore(CreateSession(42)),
            profiles);

        // Act
        ProfileResult<PlayerProfile> result = applicationService.GetProfile("session-token", 99);

        // Assert
        Assert.AreEqual(ProfileStatus.Success, result.Status);
        Assert.AreEqual(2, profiles.AccountIds.Count);
        Assert.AreEqual(42L, profiles.AccountIds[0]);
        Assert.AreEqual(99L, profiles.AccountIds[1]);
        Assert.AreEqual("OtherPlayer", result.Value?.Username);
        Assert.AreEqual(0d, result.Value?.WinRate);
    }

    [TestMethod]
    public void GetProfile_MissingSessionToken_RejectsBeforeRepositoryAccess()
    {
        // Arrange
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        PlayerProfileApplicationService applicationService = CreateService(
            CreateSessionStore(CreateSession(42)),
            profiles);

        // Act
        ProfileResult<PlayerProfile> result = applicationService.GetProfile(string.Empty, null);

        // Assert
        Assert.AreEqual(ProfileStatus.MissingSessionToken, result.Status);
        Assert.AreEqual(0, profiles.AccountIds.Count);
    }

    [TestMethod]
    public void GetProfile_InvalidTargetAccountId_RejectsBeforeRepositoryAccess()
    {
        // Arrange
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        PlayerProfileApplicationService applicationService = CreateService(
            CreateSessionStore(CreateSession(42)),
            profiles);

        // Act
        ProfileResult<PlayerProfile> result = applicationService.GetProfile("session-token", 0);

        // Assert
        Assert.AreEqual(ProfileStatus.InvalidAccountId, result.Status);
        Assert.AreEqual(0, profiles.AccountIds.Count);
    }

    [TestMethod]
    public void GetProfile_InvalidSession_PrecedesInvalidTargetAccount()
    {
        // Arrange
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        PlayerProfileApplicationService applicationService = CreateService(
            CreateSessionStore(),
            profiles);

        // Act
        ProfileResult<PlayerProfile> result = applicationService.GetProfile("unknown-token", 0);

        // Assert
        Assert.AreEqual(ProfileStatus.InvalidSession, result.Status);
        Assert.AreEqual(0, profiles.AccountIds.Count);
    }

    [TestMethod]
    public void GetProfile_UnknownSession_RejectsBeforeRepositoryAccess()
    {
        // Arrange
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        PlayerProfileApplicationService applicationService = CreateService(
            CreateSessionStore(),
            profiles);

        // Act
        ProfileResult<PlayerProfile> result = applicationService.GetProfile(
            "unknown-token",
            null);

        // Assert
        Assert.AreEqual(ProfileStatus.InvalidSession, result.Status);
        Assert.AreEqual(0, profiles.AccountIds.Count);
    }

    [TestMethod]
    public void GetProfile_InactiveCaller_DoesNotReadRequestedTarget()
    {
        // Arrange
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        PlayerProfileApplicationService applicationService = CreateService(
            CreateSessionStore(CreateSession(42)),
            profiles);

        // Act
        ProfileResult<PlayerProfile> result = applicationService.GetProfile("session-token", 99);

        // Assert
        Assert.AreEqual(ProfileStatus.InactiveAccount, result.Status);
        Assert.AreEqual(1, profiles.AccountIds.Count);
        Assert.AreEqual(42L, profiles.AccountIds[0]);
    }

    [TestMethod]
    public void GetProfile_MissingTarget_ReturnsAccountNotFound()
    {
        // Arrange
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        profiles.Profiles.Add(42, new PlayerProfile(42, "PlayerOne", null, 1, 0, 0));
        PlayerProfileApplicationService applicationService = CreateService(
            CreateSessionStore(CreateSession(42)),
            profiles);

        // Act
        ProfileResult<PlayerProfile> result = applicationService.GetProfile("session-token", 99);

        // Assert
        Assert.AreEqual(ProfileStatus.AccountNotFound, result.Status);
        Assert.AreEqual(2, profiles.AccountIds.Count);
    }

    [TestMethod]
    public void GetProfile_RepositoryFailure_PropagatesException()
    {
        // Arrange
        PlayerProfileApplicationService applicationService = CreateService(
            CreateSessionStore(CreateSession(42)),
            new PlayerProfileRepositoryStub { ThrowOnGet = true });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            applicationService.GetProfile("session-token", null));
    }

    [TestMethod]
    public void GetImage_SelfWithoutImage_QueriesImageReferenceWithoutLoadingProfile()
    {
        // Arrange
        PlayerProfileRepositoryStub profiles = CreateActiveProfiles();
        PlayerImageRepositoryStub images = new PlayerImageRepositoryStub();
        PlayerProfileApplicationService applicationService = new PlayerProfileApplicationService(
            CreateSessionStore(CreateSession(42)),
            profiles,
            images);

        // Act
        GetProfileImageResult result = applicationService.GetImage("session-token", null);

        // Assert
        Assert.AreEqual(ProfileStatus.Success, result.Status);
        Assert.AreEqual<ProfileImageData?>(null, result.Image);
        Assert.AreEqual(0, profiles.GetProfileCalls);
        Assert.AreEqual(1, profiles.GetImageReferenceCalls);
        Assert.AreEqual(false, images.ReadCalled);
    }

    [TestMethod]
    public void GetImage_PublicProfile_VerifiesCallerThenReadsTargetImage()
    {
        // Arrange
        PlayerProfileRepositoryStub profiles = CreateActiveProfiles();
        profiles.Profiles.Add(99, new PlayerProfile(
            99,
            "OtherPlayer",
            "profile-images/other.png",
            2,
            1,
            0));
        PlayerImageRepositoryStub images = new PlayerImageRepositoryStub();
        PlayerProfileApplicationService applicationService = new PlayerProfileApplicationService(
            CreateSessionStore(CreateSession(42)),
            profiles,
            images);

        // Act
        GetProfileImageResult result = applicationService.GetImage("session-token", 99);

        // Assert
        Assert.AreEqual(ProfileStatus.Success, result.Status);
        ProfileImageData image = TestAssert.IsNotNull(result.Image);
        Assert.AreEqual("image/png", image.MediaType);
        Assert.AreEqual(0, profiles.GetProfileCalls);
        Assert.AreEqual(1, profiles.GetImageReferenceCalls);
        Assert.AreEqual(true, images.ReadCalled);
    }

    [TestMethod]
    public void ChangeUsername_ValidUsername_NormalizesAndUpdatesCaller()
    {
        // Arrange
        PlayerProfileRepositoryStub profiles = CreateActiveProfiles();
        PlayerProfileApplicationService applicationService = CreateService(
            CreateSessionStore(CreateSession(42)),
            profiles);

        // Act
        ProfileStatus status = applicationService.ChangeUsername(
            "session-token",
            "  NewPlayer  ");

        // Assert
        Assert.AreEqual(ProfileStatus.Success, status);
        Assert.AreEqual<long?>(42, profiles.ChangedAccountId);
        Assert.AreEqual("NewPlayer", profiles.ChangedUsername);
    }

    [TestMethod]
    public void ChangeUsername_InvalidUsername_RejectsBeforeRepositoryAccess()
    {
        // Arrange
        PlayerProfileRepositoryStub profiles = CreateActiveProfiles();
        PlayerProfileApplicationService applicationService = CreateService(
            CreateSessionStore(CreateSession(42)),
            profiles);

        // Act
        ProfileStatus status = applicationService.ChangeUsername("session-token", "a");

        // Assert
        Assert.AreEqual(ProfileStatus.InvalidUsername, status);
        Assert.AreEqual<long?>(null, profiles.ChangedAccountId);
    }

    [TestMethod]
    public void ChangeUsername_UnavailableUsername_ReturnsExpectedStatus()
    {
        // Arrange
        PlayerProfileRepositoryStub profiles = CreateActiveProfiles();
        profiles.ChangeUsernameStatus = ProfileStatus.UsernameUnavailable;
        PlayerProfileApplicationService applicationService = CreateService(
            CreateSessionStore(CreateSession(42)),
            profiles);

        // Act
        ProfileStatus status = applicationService.ChangeUsername(
            "session-token",
            "OtherPlayer");

        // Assert
        Assert.AreEqual(ProfileStatus.UsernameUnavailable, status);
    }

    [TestMethod]
    public void UpdateImage_InactiveAccount_RemovesStoredImage()
    {
        // Arrange
        PlayerProfileRepositoryStub profiles = CreateActiveProfiles();
        profiles.SetImageStatus = ProfileStatus.InactiveAccount;
        PlayerImageRepositoryStub images = new PlayerImageRepositoryStub();
        PlayerProfileApplicationService applicationService = new PlayerProfileApplicationService(
            CreateSessionStore(CreateSession(42)),
            profiles,
            images);

        // Act
        ProfileStatus status = applicationService.UpdateImage(
            "session-token",
            new ProfileImageData(new byte[] { 1 }, "image/png"));

        // Assert
        Assert.AreEqual(ProfileStatus.InactiveAccount, status);
        Assert.AreEqual("new-image", images.Deleted);
    }

    [TestMethod]
    public void UpdateImage_AssociationFailure_RemovesStoredImageAndPropagates()
    {
        // Arrange
        PlayerProfileRepositoryStub profiles = CreateActiveProfiles();
        profiles.ThrowOnSetImage = true;
        PlayerImageRepositoryStub images = new PlayerImageRepositoryStub();
        PlayerProfileApplicationService applicationService = new PlayerProfileApplicationService(
            CreateSessionStore(CreateSession(42)),
            profiles,
            images);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            applicationService.UpdateImage(
                "session-token",
                new ProfileImageData(new byte[] { 1 }, "image/png")));
        Assert.AreEqual("new-image", images.Deleted);
    }

    [TestMethod]
    public void UpdateImage_Success_DeletesPreviousImageAfterAssociation()
    {
        const long AccountId = 42;
        const string PreviousReference = "profile-images/previous.jpg";
        PlayerProfileRepositoryStub profiles = CreateActiveProfiles();
        profiles.Profiles[AccountId] = new PlayerProfile(
            AccountId, "PlayerOne", PreviousReference, 1, 0, 0);
        PlayerImageRepositoryStub images = new PlayerImageRepositoryStub();
        PlayerProfileApplicationService service = new PlayerProfileApplicationService(
            CreateSessionStore(CreateSession(AccountId)), profiles, images);

        ProfileStatus status = service.UpdateImage("session-token",
            new ProfileImageData(new byte[] { 1 }, "image/png"));

        Assert.AreEqual(ProfileStatus.Success, status);
        Assert.AreEqual("new-image", profiles.ImageReference);
        Assert.AreEqual(PreviousReference, images.Deleted);
        Assert.AreEqual(PlayerImageKind.Profile, images.LastKind);
    }

    [TestMethod]
    public void UpdateImage_AssociationAndCleanupFail_PreservesBothFailures()
    {
        const long AccountId = 42;
        PlayerProfileRepositoryStub profiles = CreateActiveProfiles();
        profiles.ThrowOnSetImage = true;
        IOException cleanupFailure = new IOException("The cleanup failed.");
        PlayerImageRepositoryStub images = new PlayerImageRepositoryStub
        {
            CleanupFailure = cleanupFailure
        };
        PlayerProfileApplicationService service = new PlayerProfileApplicationService(
            CreateSessionStore(CreateSession(AccountId)), profiles, images);

        AggregateException reported = Assert.Throws<AggregateException>(() =>
            service.UpdateImage("session-token",
                new ProfileImageData(new byte[] { 1 }, "image/png")));

        Assert.IsInstanceOfType<InvalidOperationException>(reported.InnerExceptions[0]);
        Assert.AreSame(cleanupFailure, reported.InnerExceptions[1]);
    }

    [TestMethod]
    public void UpdateImage_PreviousDeletionFails_PreservesNewAssociation()
    {
        const long AccountId = 42;
        PlayerProfileRepositoryStub profiles = CreateActiveProfiles();
        profiles.Profiles[AccountId] = new PlayerProfile(
            AccountId, "PlayerOne", "profile-images/previous.jpg", 1, 0, 0);
        IOException cleanupFailure = new IOException("The previous image cannot be removed.");
        PlayerImageRepositoryStub images = new PlayerImageRepositoryStub
        {
            CleanupFailure = cleanupFailure
        };
        PlayerProfileApplicationService service = new PlayerProfileApplicationService(
            CreateSessionStore(CreateSession(AccountId)), profiles, images);

        IOException reported = Assert.Throws<IOException>(() =>
            service.UpdateImage("session-token",
                new ProfileImageData(new byte[] { 1 }, "image/png")));

        Assert.AreSame(cleanupFailure, reported);
        Assert.AreEqual("new-image", profiles.ImageReference);
    }

    private static SessionStore CreateSessionStore(AuthenticatedSession? session = null)
    {
        SessionStore store = new SessionStore(new TestUtcClock());
        if (session != null)
        {
            store.TryAddAuthenticated(session);
        }

        return store;
    }

    private static PlayerProfileApplicationService CreateService(
        SessionStore sessions,
        PlayerProfileRepositoryStub profiles)
    {
        return new PlayerProfileApplicationService(
            sessions,
            profiles,
            new PlayerImageRepositoryStub());
    }

    private static PlayerProfileRepositoryStub CreateActiveProfiles()
    {
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        profiles.Profiles.Add(42, new PlayerProfile(42, "PlayerOne", null, 1, 0, 0));
        return profiles;
    }

    private static AuthenticatedSession CreateSession(long accountId)
    {
        return new AuthenticatedSession(
            "session-token",
            accountId,
            "PlayerOne",
            "player@example.com",
            "en-US",
            DateTime.UtcNow.AddHours(8));
    }
}

internal sealed class PlayerProfileRepositoryStub : IPlayerProfileRepository
{
    internal Dictionary<long, PlayerProfile> Profiles { get; } =
        new Dictionary<long, PlayerProfile>();

    internal List<long> AccountIds { get; } = new List<long>();

    internal bool ThrowOnGet { get; set; }

    internal bool ThrowOnSetImage { get; set; }

    internal ProfileStatus ChangeUsernameStatus { get; set; } =
        ProfileStatus.Success;

    internal ProfileStatus SetImageStatus { get; set; } =
        ProfileStatus.Success;

    internal long? ChangedAccountId { get; private set; }

    internal string? ChangedUsername { get; private set; }

    internal long? ImageAccountId { get; private set; }

    internal string? ImageReference { get; private set; }

    internal int GetProfileCalls { get; private set; }

    internal int GetImageReferenceCalls { get; private set; }

    public ProfileResult<PlayerProfile> GetProfile(long accountId)
    {
        GetProfileCalls++;
        AccountIds.Add(accountId);
        if (ThrowOnGet)
        {
            throw new InvalidOperationException("private-provider-message");
        }

        if (!Profiles.TryGetValue(accountId, out PlayerProfile? profile))
        {
            return new ProfileResult<PlayerProfile>(ProfileStatus.AccountNotFound);
        }

        return new ProfileResult<PlayerProfile>(profile);
    }

    public ProfileResult<string> GetProfileImageReference(long accountId)
    {
        GetImageReferenceCalls++;
        AccountIds.Add(accountId);
        if (ThrowOnGet)
        {
            throw new InvalidOperationException("private-provider-message");
        }

        if (!Profiles.TryGetValue(accountId, out PlayerProfile? profile))
        {
            return new ProfileResult<string>(ProfileStatus.AccountNotFound);
        }

        return new ProfileResult<string>(profile.ProfileImageUri ?? string.Empty);
    }

    public bool IsActive(long accountId)
    {
        AccountIds.Add(accountId);
        return Profiles.ContainsKey(accountId);
    }

    public ProfileStatus ChangeUsername(
        long accountId,
        string username)
    {
        ChangedAccountId = accountId;
        ChangedUsername = username;
        return ChangeUsernameStatus;
    }

    public ProfileStatus SetProfileImage(
        long accountId,
        string imageReference)
    {
        ImageAccountId = accountId;
        ImageReference = imageReference;
        if (ThrowOnSetImage)
        {
            throw new InvalidOperationException("private-provider-message");
        }

        return SetImageStatus;
    }
}
