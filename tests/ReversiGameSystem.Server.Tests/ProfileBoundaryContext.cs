using ReversiGameSystem.Server.Domain.Accounts;
using System;
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
            return new ProfileResult<string>(ProfileOperationStatus.InvalidImage);
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
