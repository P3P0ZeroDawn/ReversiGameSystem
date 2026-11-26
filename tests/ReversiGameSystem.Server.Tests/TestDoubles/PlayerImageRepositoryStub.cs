using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests.TestDoubles;

internal sealed class PlayerImageRepositoryStub : IPlayerImageRepository
{
    internal const string DefaultImageReference = "board-backgrounds/test.png";

    internal ProfileImageData StoredImage { get; set; } = new ProfileImageData(
        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 },
        PlayerImagePolicy.PngMediaType);

    internal ProfileResult<string> SaveResult { get; set; } =
        new ProfileResult<string>(DefaultImageReference);

    internal Exception? CleanupFailure { get; set; }

    internal int ReadCount { get; private set; }

    internal string LastReadReference { get; private set; } = string.Empty;

    internal int SaveCount { get; private set; }

    internal PlayerImageKind? LastSavedKind { get; private set; }

    internal string DeletedReference { get; private set; } = string.Empty;

    public ProfileImageData ReadImage(string imageReference)
    {
        ReadCount++;
        LastReadReference = imageReference;
        return StoredImage;
    }

    public ProfileResult<string> SaveImage(ProfileImageData image, PlayerImageKind kind)
    {
        SaveCount++;
        LastSavedKind = kind;
        return SaveResult;
    }

    public void DeleteImage(string imageReference)
    {
        if (CleanupFailure != null)
        {
            throw CleanupFailure;
        }

        DeletedReference = imageReference;
    }
}
