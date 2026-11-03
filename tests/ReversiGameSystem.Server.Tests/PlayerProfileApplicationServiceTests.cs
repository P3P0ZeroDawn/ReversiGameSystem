using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;

namespace ReversiGameSystem.Server.Tests;

/// <summary>Verifies the player profile application operations in isolation.</summary>
internal sealed class PlayerProfileApplicationServiceTests
{
    internal void GetAsync_CurrentPlayer_ReturnsCalculatedStatistics()
    {
        PlayerProfileSessionStoreStub sessions = new PlayerProfileSessionStoreStub
        {
            Session = CreateSession(42)
        };
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        profiles.Profiles.Add(42, new PlayerProfile(
            42,
            "PlayerOne",
            "profile-images/player.png",
            7,
            3,
            2));
        PlayerProfileApplicationService applicationService = CreateService(sessions, profiles);

        GetPlayerProfileResult result = applicationService.GetProfileAsync("session-token", null)
            .GetAwaiter().GetResult();

        TestAssert.AreEqual(ProfileOperationStatus.Success, result.Status);
        TestAssert.AreEqual("session-token", sessions.SessionToken);
        TestAssert.AreEqual(1, profiles.AccountIds.Count);
        TestAssert.AreEqual(42L, profiles.AccountIds[0]);
        TestAssert.AreEqual("PlayerOne", result.Profile?.Username);
        TestAssert.AreEqual("profile-images/player.png", result.Profile?.ProfileImageUri);
        TestAssert.AreEqual(12, result.Profile?.MatchesPlayed);
        TestAssert.AreEqual(7, result.Profile?.Wins);
        TestAssert.AreEqual(3, result.Profile?.Losses);
        TestAssert.AreEqual(2, result.Profile?.Draws);
        TestAssert.AreEqual(7d / 12d, result.Profile?.WinRate);
    }

    internal void GetAsync_PublicProfile_VerifiesCallerThenUsesTargetAccount()
    {
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        profiles.Profiles.Add(42, new PlayerProfile(42, "PlayerOne", null, 1, 0, 0));
        profiles.Profiles.Add(99, new PlayerProfile(99, "OtherPlayer", null, 0, 0, 0));
        PlayerProfileApplicationService applicationService = CreateService(
            new PlayerProfileSessionStoreStub { Session = CreateSession(42) },
            profiles);

        GetPlayerProfileResult result = applicationService.GetProfileAsync("session-token", 99)
            .GetAwaiter().GetResult();

        TestAssert.AreEqual(ProfileOperationStatus.Success, result.Status);
        TestAssert.AreEqual(2, profiles.AccountIds.Count);
        TestAssert.AreEqual(42L, profiles.AccountIds[0]);
        TestAssert.AreEqual(99L, profiles.AccountIds[1]);
        TestAssert.AreEqual("OtherPlayer", result.Profile?.Username);
        TestAssert.AreEqual(0d, result.Profile?.WinRate);
    }

    internal void GetAsync_InvalidInput_RejectsBeforeRepositoryAccess()
    {
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        PlayerProfileApplicationService applicationService = CreateService(
            new PlayerProfileSessionStoreStub { Session = CreateSession(42) },
            profiles);

        GetPlayerProfileResult missingToken = applicationService.GetProfileAsync("", null)
            .GetAwaiter().GetResult();
        GetPlayerProfileResult invalidAccount = applicationService.GetProfileAsync(
            "session-token",
            0)
            .GetAwaiter().GetResult();

        TestAssert.AreEqual(ProfileOperationStatus.MissingSessionToken, missingToken.Status);
        TestAssert.AreEqual(ProfileOperationStatus.InvalidAccountId, invalidAccount.Status);
        TestAssert.AreEqual(0, profiles.AccountIds.Count);
    }

    internal void GetAsync_InvalidSession_PrecedesInvalidTargetAccount()
    {
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        PlayerProfileApplicationService applicationService = CreateService(
            new PlayerProfileSessionStoreStub(),
            profiles);

        GetPlayerProfileResult result = applicationService.GetProfileAsync("unknown-token", 0)
            .GetAwaiter().GetResult();

        TestAssert.AreEqual(ProfileOperationStatus.InvalidSession, result.Status);
        TestAssert.AreEqual(0, profiles.AccountIds.Count);
    }

    internal void GetAsync_UnknownSession_RejectsBeforeRepositoryAccess()
    {
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        PlayerProfileApplicationService applicationService = CreateService(
            new PlayerProfileSessionStoreStub(),
            profiles);

        GetPlayerProfileResult result = applicationService.GetProfileAsync(
            "unknown-token",
            null)
            .GetAwaiter().GetResult();

        TestAssert.AreEqual(ProfileOperationStatus.InvalidSession, result.Status);
        TestAssert.AreEqual(0, profiles.AccountIds.Count);
    }

    internal void GetAsync_InactiveCaller_DoesNotReadRequestedTarget()
    {
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        PlayerProfileApplicationService applicationService = CreateService(
            new PlayerProfileSessionStoreStub { Session = CreateSession(42) },
            profiles);

        GetPlayerProfileResult result = applicationService.GetProfileAsync("session-token", 99)
            .GetAwaiter().GetResult();

        TestAssert.AreEqual(ProfileOperationStatus.InactiveAccount, result.Status);
        TestAssert.AreEqual(1, profiles.AccountIds.Count);
        TestAssert.AreEqual(42L, profiles.AccountIds[0]);
    }

    internal void GetAsync_MissingTarget_ReturnsAccountNotFound()
    {
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        profiles.Profiles.Add(42, new PlayerProfile(42, "PlayerOne", null, 1, 0, 0));
        PlayerProfileApplicationService applicationService = CreateService(
            new PlayerProfileSessionStoreStub { Session = CreateSession(42) },
            profiles);

        GetPlayerProfileResult result = applicationService.GetProfileAsync("session-token", 99)
            .GetAwaiter().GetResult();

        TestAssert.AreEqual(ProfileOperationStatus.AccountNotFound, result.Status);
        TestAssert.AreEqual(2, profiles.AccountIds.Count);
    }

    internal void GetAsync_RepositoryFailure_PropagatesException()
    {
        PlayerProfileApplicationService applicationService = CreateService(
            new PlayerProfileSessionStoreStub { Session = CreateSession(42) },
            new PlayerProfileRepositoryStub { ThrowOnGet = true });

        TestAssert.Throws<InvalidOperationException>(() =>
            applicationService.GetProfileAsync("session-token", null)
                .GetAwaiter().GetResult());
    }

    internal void ChangeUsernameAsync_ValidUsername_NormalizesAndUpdatesCaller()
    {
        PlayerProfileRepositoryStub profiles = CreateActiveProfiles();
        PlayerProfileApplicationService applicationService = CreateService(
            new PlayerProfileSessionStoreStub { Session = CreateSession(42) },
            profiles);

        ProfileOperationStatus status = applicationService.ChangeUsernameAsync(
            "session-token",
            "  NewPlayer  ").GetAwaiter().GetResult();

        TestAssert.AreEqual(ProfileOperationStatus.Success, status);
        TestAssert.AreEqual<long?>(42, profiles.ChangedAccountId);
        TestAssert.AreEqual("NewPlayer", profiles.ChangedUsername);
    }

    internal void ChangeUsernameAsync_UnavailableUsername_ReturnsExpectedStatus()
    {
        PlayerProfileRepositoryStub profiles = CreateActiveProfiles();
        profiles.ChangeUsernameStatus = ProfileRepositoryUpdateStatus.UsernameUnavailable;
        PlayerProfileApplicationService applicationService = CreateService(
            new PlayerProfileSessionStoreStub { Session = CreateSession(42) },
            profiles);

        ProfileOperationStatus status = applicationService.ChangeUsernameAsync(
            "session-token",
            "OtherPlayer").GetAwaiter().GetResult();

        TestAssert.AreEqual(ProfileOperationStatus.UsernameUnavailable, status);
    }

    internal void UpdateImageAsync_AssociationFailure_RemovesStoredImageAndPropagates()
    {
        PlayerProfileRepositoryStub profiles = CreateActiveProfiles();
        profiles.ThrowOnSetImage = true;
        ProfileImageRepositoryStub images = new ProfileImageRepositoryStub();
        PlayerProfileApplicationService applicationService = new PlayerProfileApplicationService(
            new PlayerProfileSessionStoreStub { Session = CreateSession(42) },
            profiles,
            images);

        TestAssert.Throws<InvalidOperationException>(() =>
            applicationService.UpdateImageAsync(
                "session-token",
                new ProfileImageData(new byte[] { 1 }, "image/png"))
                .GetAwaiter().GetResult());
        TestAssert.AreEqual("new-image", images.Deleted);
    }

    private static PlayerProfileApplicationService CreateService(
        PlayerProfileSessionStoreStub sessions,
        PlayerProfileRepositoryStub profiles)
    {
        return new PlayerProfileApplicationService(
            sessions,
            profiles,
            new ProfileImageRepositoryStub());
    }

    private static PlayerProfileRepositoryStub CreateActiveProfiles()
    {
        PlayerProfileRepositoryStub profiles = new PlayerProfileRepositoryStub();
        profiles.Profiles.Add(42, new PlayerProfile(42, "PlayerOne", null, 1, 0, 0));
        return profiles;
    }

    private static AuthenticatedSessionRecord CreateSession(long accountId)
    {
        return new AuthenticatedSessionRecord(
            "session-token",
            accountId,
            "PlayerOne",
            "player@example.com",
            "en-US",
            DateTime.UtcNow.AddHours(8));
    }
}

internal sealed class PlayerProfileSessionStoreStub : IAuthenticatedSessionStore
{
    internal AuthenticatedSessionRecord? Session { get; set; }

    internal string? SessionToken { get; private set; }

    public AuthenticatedSessionRecord Create(LoginAccount account)
    {
        throw new NotSupportedException();
    }

    public bool TryGet(string sessionToken, out AuthenticatedSessionRecord? session)
    {
        SessionToken = sessionToken;
        session = Session?.SessionToken == sessionToken ? Session : null;
        return session != null;
    }

    public void Revoke(string sessionToken)
    {
        if (Session?.SessionToken == sessionToken)
        {
            Session = null;
        }
    }
}

internal sealed class PlayerProfileRepositoryStub : IPlayerProfileRepository
{
    internal Dictionary<long, PlayerProfile> Profiles { get; } =
        new Dictionary<long, PlayerProfile>();

    internal List<long> AccountIds { get; } = new List<long>();

    internal bool ThrowOnGet { get; set; }

    internal bool ThrowOnSetImage { get; set; }

    internal ProfileRepositoryUpdateStatus ChangeUsernameStatus { get; set; } =
        ProfileRepositoryUpdateStatus.Success;

    internal ProfileRepositoryUpdateStatus SetImageStatus { get; set; } =
        ProfileRepositoryUpdateStatus.Success;

    internal long? ChangedAccountId { get; private set; }

    internal string? ChangedUsername { get; private set; }

    internal long? ImageAccountId { get; private set; }

    internal string? ImageReference { get; private set; }

    public Task<PlayerProfileLookupResult> GetAsync(long accountId)
    {
        AccountIds.Add(accountId);
        if (ThrowOnGet)
        {
            throw new InvalidOperationException("private-provider-message");
        }

        if (!Profiles.TryGetValue(accountId, out PlayerProfile? profile))
        {
            return Task.FromResult(PlayerProfileLookupResult.CreateNotFound());
        }

        return Task.FromResult(PlayerProfileLookupResult.CreateFound(profile));
    }

    public Task<bool> IsActiveAsync(long accountId)
    {
        return Task.FromResult(Profiles.ContainsKey(accountId));
    }

    public Task<ProfileRepositoryUpdateStatus> ChangeUsernameAsync(
        long accountId,
        string username)
    {
        ChangedAccountId = accountId;
        ChangedUsername = username;
        return Task.FromResult(ChangeUsernameStatus);
    }

    public Task<ProfileRepositoryUpdateStatus> SetProfileImageAsync(
        long accountId,
        string imageReference)
    {
        ImageAccountId = accountId;
        ImageReference = imageReference;
        if (ThrowOnSetImage)
        {
            throw new InvalidOperationException("private-provider-message");
        }

        return Task.FromResult(SetImageStatus);
    }
}
