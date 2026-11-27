using System;
using ReversiGameSystem.Server.Application.Players;

namespace ReversiGameSystem.Server.Tests.TestDoubles;

internal sealed class PlayerProfileRepositoryStub : IPlayerProfileRepository
{
    internal PlayerProfileRepositoryStub()
    {
        ProfileResult = new ProfileResult<PlayerProfile>(
            new PlayerProfile(10L, "PlayerTen", "profiles/old.png", 3, 1, 1));
        ImageReferenceResult = new ProfileResult<string>("profiles/old.png");
    }

    internal int GetProfileCalls { get; private set; }

    internal int GetImageReferenceCalls { get; private set; }

    internal int IsActiveCalls { get; private set; }

    internal int ChangeUsernameCalls { get; private set; }

    internal int SetProfileImageCalls { get; private set; }

    internal long LastAccountId { get; private set; }

    internal string LastUsername { get; private set; } = string.Empty;

    internal string LastImageReference { get; private set; } = string.Empty;

    internal ProfileResult<PlayerProfile> ProfileResult { get; set; }

    internal ProfileResult<string> ImageReferenceResult { get; set; }

    internal bool IsActiveResult { get; set; } = true;

    internal ProfileStatus ChangeUsernameResult { get; set; } = ProfileStatus.Success;

    internal ProfileStatus SetProfileImageResult { get; set; } = ProfileStatus.Success;

    internal Exception? SetProfileImageFailure { get; set; }

    public ProfileResult<PlayerProfile> GetProfile(long accountId)
    {
        GetProfileCalls++;
        LastAccountId = accountId;
        return ProfileResult;
    }

    public ProfileResult<string> GetProfileImageReference(long accountId)
    {
        GetImageReferenceCalls++;
        LastAccountId = accountId;
        return ImageReferenceResult;
    }

    public bool IsActive(long accountId)
    {
        IsActiveCalls++;
        LastAccountId = accountId;
        return IsActiveResult;
    }

    public ProfileStatus ChangeUsername(long accountId, string username)
    {
        ChangeUsernameCalls++;
        LastAccountId = accountId;
        LastUsername = username;
        return ChangeUsernameResult;
    }

    public ProfileStatus SetProfileImage(long accountId, string imageReference)
    {
        SetProfileImageCalls++;
        LastAccountId = accountId;
        LastImageReference = imageReference;
        if (SetProfileImageFailure != null)
        {
            throw SetProfileImageFailure;
        }

        return SetProfileImageResult;
    }
}
