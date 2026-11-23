using System;
using System.IO;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
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
            ProfileImageRepository store = new ProfileImageRepository(root);
            byte[] imageContent = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwC" +
                "AAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

            // Act
            ProfileResult<string> result = store.SaveImage(
                new ProfileImageData(imageContent, "image/png"));

            // Assert
            TestAssert.AreEqual(ProfileStatus.Success, result.Status);
            TestAssert.AreEqual(true, result.Value?.StartsWith("profile-images/"));
            TestAssert.AreEqual(1, Directory.GetFiles(root).Length);

            string imageUri = TestAssert.IsNotNull(result.Value);
            store.DeleteImage(imageUri);
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

    internal void SaveImage_ValidAndInvalidImages_ReturnsExpectedOutcome()
    {
        // Arrange
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

            // Act
            ProfileResult<string> saved = repository.SaveImage(
                new ProfileImageData(imageContent, "image/png"));
            ProfileResult<string> rejected = repository.SaveImage(
                new ProfileImageData(new byte[] { 1, 2 }, "image/png"));

            // Assert
            TestAssert.AreEqual(ProfileStatus.Success, saved.Status);
            string savedReference = TestAssert.IsNotNull(saved.Value);
            TestAssert.AreEqual(true, savedReference.StartsWith("profile-images/"));
            TestAssert.AreEqual(ProfileStatus.InvalidImage, rejected.Status);
            TestAssert.AreEqual<string?>(null, rejected.Value);
            TestAssert.AreEqual(1, Directory.GetFiles(root).Length);

            repository.DeleteImage(savedReference);
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
