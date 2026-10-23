using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

internal sealed class ProfileBoundaryContext
{
    internal PlayerProfileSessionStoreStub Sessions { get; } = new PlayerProfileSessionStoreStub
    {
        Session = new AuthenticatedSessionRecord("session-token", 42, "PlayerOne",
            "player@example.com", "en-US", DateTime.UtcNow.AddHours(1))
    };
    internal PlayerProfileStoreStub Profiles { get; } = new PlayerProfileStoreStub
    {
        Result = new PlayerProfileStoreResult(PlayerProfileStoreOutcome.Found,
            new PlayerProfileData(42, "PlayerOne", null, 3, 2, 1))
    };
    internal ProfileEditorStub Editor { get; } = new ProfileEditorStub();
    internal ProfileImageRepositoryStub Images { get; } = new ProfileImageRepositoryStub();
    internal PlayerProfileService Service => new PlayerProfileService(
        new PlayerProfileReader(Sessions, Profiles, Images),
        new PlayerProfileEditor(Sessions, Editor, Images), NullLogger<PlayerProfileService>.Instance);
}

internal sealed class ProfileEditorStub : IPlayerProfileEditorStore
{
    internal long? AccountId { get; private set; }
    internal string? Username { get; private set; }
    internal bool FailAssociation { get; set; }
    public bool IsActive(long accountId) => accountId == 42;
    public void ChangeUsername(long accountId, string username)
    {
        AccountId = accountId;
        Username = username;
    }
    public void SetProfileImage(long accountId, string imageReference)
    {
        AccountId = accountId;
        if (FailAssociation)
        {
            throw new InvalidOperationException("private-provider-message");
        }
    }
}

internal sealed class ProfileImageRepositoryStub : IPlayerImageRepository
{
    internal bool FailRead { get; set; }
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
    public string Save(ProfileImageData image) => "new-image";
    public void Delete(string imageReference) => Deleted = imageReference;
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
