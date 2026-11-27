using System;
using System.Collections.Generic;
using ReversiGameSystem.Server.Domain.Accounts;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

internal sealed class ProfileBoundaryContext
{
    internal ProfileBoundaryContext()
    {
        Sessions.TryAddAuthenticated(new AuthenticatedSession(
            "session-token",
            42,
            "PlayerOne",
            "player@example.com",
            "en-US",
            DateTime.UtcNow.AddHours(1)));
        Profiles.Profiles.Add(42, new PlayerProfile(
            42,
            "PlayerOne",
            null,
            3,
            2,
            1));
    }

    internal SessionStore Sessions { get; } = new SessionStore(new TestUtcClock());
    internal PlayerProfileRepositoryStub Profiles { get; } =
        new PlayerProfileRepositoryStub();
    internal PlayerImageRepositoryStub Images { get; } = new PlayerImageRepositoryStub();
    internal PlayerProfileService Service
    {
        get
        {
            PlayerProfileApplicationService applicationService =
                new PlayerProfileApplicationService(
                Sessions,
                Profiles,
                Images);
            return new PlayerProfileService(applicationService);
        }
    }
}

internal sealed class PlayerImageRepositoryStub : IPlayerImageRepository
{
    internal Exception? CleanupFailure { get; set; }
    internal PlayerImageKind LastKind { get; private set; }
    internal bool FailRead { get; set; }
    internal bool RejectSave { get; set; }
    internal bool ReadCalled { get; private set; }
    internal string? Deleted { get; private set; }
    public ProfileImageData ReadImage(string imageReference)
    {
        ReadCalled = true;
        if (FailRead)
        {
            throw new InvalidOperationException("private-provider-message");
        }
        return new ProfileImageData(new byte[] { 1, 2 }, "image/png");
    }
    public ProfileResult<string> SaveImage(ProfileImageData image, PlayerImageKind kind)
    {
        LastKind = kind;
        if (RejectSave)
        {
            return new ProfileResult<string>(ProfileStatus.InvalidImage);
        }

        return new ProfileResult<string>("new-image");
    }
    public void DeleteImage(string imageReference)
    {
        if (CleanupFailure != null)
        {
            throw CleanupFailure;
        }
        Deleted = imageReference;
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
