using System;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

internal sealed class ProfileBoundaryContext
{
    internal ProfileBoundaryContext()
    {
        Profiles.Profiles.Add(42, new PlayerProfile(
            42,
            "PlayerOne",
            null,
            3,
            2,
            1));
    }

    internal PlayerProfileSessionStoreStub Sessions { get; } = new PlayerProfileSessionStoreStub
    {
        Session = new AuthenticatedSession("session-token", 42, "PlayerOne",
            "player@example.com", "en-US", DateTime.UtcNow.AddHours(1))
    };
    internal PlayerProfileRepositoryStub Profiles { get; } =
        new PlayerProfileRepositoryStub();
    internal ProfileImageRepositoryStub Images { get; } = new ProfileImageRepositoryStub();
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

internal sealed class ProfileImageRepositoryStub : IPlayerImageRepository
{
    internal bool FailRead { get; set; }
    internal bool RejectSave { get; set; }
    internal bool ReadCalled { get; private set; }
    internal string? Deleted { get; private set; }
    public ProfileImageData Read(string imageReference)
    {
        ReadCalled = true;
        if (FailRead)
        {
            throw new InvalidOperationException("private-provider-message");
        }
        return new ProfileImageData(new byte[] { 1, 2 }, "image/png");
    }
    public bool TrySave(ProfileImageData image, out string imageReference)
    {
        imageReference = "new-image";
        return !RejectSave;
    }
    public void Delete(string imageReference)
    {
        Deleted = imageReference;
    }
}

internal sealed class RecordingRegistrationHandler : ICreateAccountHandler
{
    internal CreateAccountCommand? Command { get; private set; }
    public Task<CreateAccountResult> ExecuteAsync(CreateAccountCommand command)
    {
        Command = command;
        return Task.FromResult(new CreateAccountResult(CreateAccountOutcome.Success, 42));
    }
}
