using System;
using System.IO;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Players;

namespace ReversiGameSystem.Server.Tests;

internal sealed class ProfileImageStoreIntegrationTests
{
    internal void SaveAndDelete_ValidImage_UsesRelativeUri()
    {
        // Arrange
        string root = Path.Combine(
            Path.GetTempPath(),
            "ReversiGameSystem.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            FileSystemProfileImageStore store = new FileSystemProfileImageStore(root);
            byte[] imageContent = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwC" +
                "AAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

            // Act
            ProfileImageStorageResult result = store.Save(
                new ProfileImageData(imageContent, "image/png"));

            // Assert
            TestAssert.AreEqual(ProfileImageStorageOutcome.Success, result.Outcome);
            TestAssert.AreEqual(true, result.ImageUri?.StartsWith("profile-images/"));
            TestAssert.AreEqual(1, Directory.GetFiles(root).Length);

            string imageUri = TestAssert.IsNotNull(result.ImageUri);
            store.Delete(imageUri);
            TestAssert.AreEqual(0, Directory.GetFiles(root).Length);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    internal void TrySave_ValidAndInvalidImages_ReturnsExpectedOutcome()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ReversiGameSystem.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            ProfileImageRepository repository = new ProfileImageRepository(root);
            byte[] imageContent = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwC"
                + "AAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

            bool saved = repository.TrySave(
                new ProfileImageData(imageContent, "image/png"),
                out string imageReference);
            bool rejected = repository.TrySave(
                new ProfileImageData(new byte[] { 1, 2 }, "image/png"),
                out string rejectedReference);

            TestAssert.AreEqual(true, saved);
            TestAssert.AreEqual(true, imageReference.StartsWith("profile-images/"));
            TestAssert.AreEqual(false, rejected);
            TestAssert.AreEqual(string.Empty, rejectedReference);
            TestAssert.AreEqual(1, Directory.GetFiles(root).Length);

            repository.Delete(imageReference);
            TestAssert.AreEqual(0, Directory.GetFiles(root).Length);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
