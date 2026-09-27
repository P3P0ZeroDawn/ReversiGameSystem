using System;
using System.IO;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Persistence.Accounts;

namespace ReversiGameSystem.Tests;

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
            ProfileImageStorageResult result = store.SaveAsync(
                new ProfileImageData(imageContent, "image/png"))
                .GetAwaiter()
                .GetResult();

            // Assert
            TestAssert.AreEqual(ProfileImageStorageOutcome.Success, result.Outcome);
            TestAssert.AreEqual(true, result.ImageUri?.StartsWith("profile-images/"));
            TestAssert.AreEqual(1, Directory.GetFiles(root).Length);

            string imageUri = TestAssert.IsNotNull(result.ImageUri);
            store.DeleteAsync(imageUri).GetAwaiter().GetResult();
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
