using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Players;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class ProfileImageStoreIntegrationTests
{
    [TestMethod]
    public void SaveAndDelete_ValidImage_UsesRelativeUri()
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
            Assert.AreEqual(ProfileStatus.Success, result.Status);
            Assert.AreEqual(true, result.Value?.StartsWith("profile-images/"));
            Assert.AreEqual(1, Directory.GetFiles(root).Length);

            string imageUri = TestAssert.IsNotNull(result.Value);
            store.DeleteImage(imageUri);
            Assert.AreEqual(0, Directory.GetFiles(root).Length);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [TestMethod]
    public void SaveImage_ValidAndInvalidImages_ReturnsExpectedOutcome()
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
            Assert.AreEqual(ProfileStatus.Success, saved.Status);
            string savedReference = TestAssert.IsNotNull(saved.Value);
            Assert.AreEqual(true, savedReference.StartsWith("profile-images/"));
            Assert.AreEqual(ProfileStatus.InvalidImage, rejected.Status);
            Assert.AreEqual<string?>(null, rejected.Value);
            Assert.AreEqual(1, Directory.GetFiles(root).Length);

            repository.DeleteImage(savedReference);
            Assert.AreEqual(0, Directory.GetFiles(root).Length);
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
