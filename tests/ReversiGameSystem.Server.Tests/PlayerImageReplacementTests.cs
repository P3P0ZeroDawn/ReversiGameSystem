using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Preferences;
using ReversiGameSystem.Server.Persistence.Players;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class PlayerImageReplacementTests
{
    private const int SingleFileCount = 1;
    private const int ImageDimension = 16;
    private const int ConcurrentReplacementCount = 12;
    private const int SessionCount = 2;

    [TestMethod]
    public void ChangeBoardBackground_SuccessiveReplacements_LeavesOnlyReferencedFile()
    {
        using (ReplacementFixture fixture = new ReplacementFixture())
        {
            fixture.Replace(fixture.FirstSession);

            fixture.Replace(fixture.FirstSession);

            AssertOnlyReferencedFile(fixture);
        }
    }

    [TestMethod]
    public void ChangeBoardBackground_TwoSessionsReplaceConcurrently_LeavesOnlyFinalReferencedFile()
    {
        using (ReplacementFixture fixture = new ReplacementFixture())
        using (ManualResetEventSlim start = new ManualResetEventSlim(false))
        {
            Task[] replacements = new Task[ConcurrentReplacementCount];
            for (int index = 0; index < replacements.Length; index++)
            {
                AuthenticatedSession session = fixture.FirstSession;
                if (index % SessionCount != 0)
                {
                    session = fixture.SecondSession;
                }
                replacements[index] = Task.Run(() =>
                {
                    start.Wait();
                    fixture.Replace(session);
                });
            }

            start.Set();
            Task.WaitAll(replacements);

            AssertOnlyReferencedFile(fixture);
        }
    }

    [TestMethod]
    public void ChangeBoardBackground_AssociationThrows_PreservesPreviousFileAndRemovesNewFile()
    {
        using (ReplacementFixture fixture = new ReplacementFixture())
        {
            fixture.Replace(fixture.FirstSession);
            string previousReference = fixture.Preferences.Reference;
            IOException failure = new IOException("The association failed.");
            fixture.Preferences.AssociationFailure = failure;

            IOException reported = Assert.Throws<IOException>(() =>
                fixture.Service.ChangeBoardBackground(
                    fixture.FirstSession.SessionToken, fixture.Image));

            Assert.AreSame(failure, reported);
            Assert.AreEqual(previousReference, fixture.Preferences.Reference);
            AssertOnlyReferencedFile(fixture);
        }
    }

    [TestMethod]
    public void ChangeBoardBackground_AssociationRejected_PreservesPreviousFileAndRemovesNewFile()
    {
        using (ReplacementFixture fixture = new ReplacementFixture())
        {
            fixture.Replace(fixture.FirstSession);
            string previousReference = fixture.Preferences.Reference;
            fixture.Preferences.AssociationStatus = PreferencesStatus.InactiveAccount;

            PreferencesStatus actualStatus = fixture.Service.ChangeBoardBackground(
                fixture.FirstSession.SessionToken, fixture.Image);

            Assert.AreEqual(PreferencesStatus.InactiveAccount, actualStatus);
            Assert.AreEqual(previousReference, fixture.Preferences.Reference);
            AssertOnlyReferencedFile(fixture);
        }
    }

    private static void AssertOnlyReferencedFile(ReplacementFixture fixture)
    {
        string[] files = Directory.GetFiles(fixture.DirectoryPath);
        Assert.AreEqual(SingleFileCount, files.Length);
        Assert.AreEqual(Path.GetFileName(fixture.Preferences.Reference),
            Path.GetFileName(files[0]));
        Assert.IsTrue(fixture.Images.ReadImage(fixture.Preferences.Reference).Content.Length > 0);
    }

    private sealed class ReplacementFixture : IDisposable
    {
        private const long AccountId = 42;

        private static ProfileImageData CreateImage()
        {
            using (Bitmap bitmap = new Bitmap(ImageDimension, ImageDimension))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (MemoryStream output = new MemoryStream())
            {
                graphics.Clear(Color.CornflowerBlue);
                bitmap.Save(output, ImageFormat.Png);
                return new ProfileImageData(output.ToArray(), "image/png");
            }
        }

        internal ReplacementFixture()
        {
            string temporaryRoot = Path.GetFullPath(Path.GetTempPath());
            DirectoryPath = Path.GetFullPath(Path.Combine(temporaryRoot,
                "ReversiImageReplacement-" + Guid.NewGuid().ToString("N")));
            if (!string.Equals(Path.GetDirectoryName(DirectoryPath),
                temporaryRoot.TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The disposable image directory is invalid.");
            }
            Images = new PlayerImageRepository(DirectoryPath, "board-backgrounds/");
            SessionStore sessions = new SessionStore(new TestUtcClock());
            FirstSession = TestSessionHelper.CreateSession(
                sessions, AccountId, "ReplacementPlayer");
            SecondSession = TestSessionHelper.CreateSession(
                sessions, AccountId, "ReplacementPlayer");
            Service = new PreferencesApplicationService(sessions, Preferences, Images);
        }

        internal string DirectoryPath { get; }
        internal PreferencesRepositoryFake Preferences { get; } = new PreferencesRepositoryFake();
        internal PlayerImageRepository Images { get; }
        internal ProfileImageData Image { get; } = CreateImage();
        internal AuthenticatedSession FirstSession { get; }
        internal AuthenticatedSession SecondSession { get; }
        internal PreferencesApplicationService Service { get; }

        internal void Replace(AuthenticatedSession session)
        {
            PreferencesStatus actualStatus = Service.ChangeBoardBackground(
                session.SessionToken, Image);
            Assert.AreEqual(PreferencesStatus.Success, actualStatus);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }

    private sealed class PreferencesRepositoryFake : IPreferencesRepository
    {
        internal string Reference { get; private set; } = string.Empty;
        internal IOException? AssociationFailure { get; set; }
        internal PreferencesStatus AssociationStatus { get; set; } = PreferencesStatus.Success;

        public PreferencesAccess ReadPreferences(long accountId)
        {
            PlayerPreferences preferences = new PlayerPreferences
            {
                BoardBackgroundReference = Reference
            };
            return new PreferencesAccess(preferences);
        }

        public PreferencesStatus SavePreferences(long accountId, PlayerPreferences preferences)
        {
            throw new NotSupportedException();
        }

        public PreferencesStatus SetBoardBackground(long accountId, string imageReference)
        {
            if (AssociationFailure != null)
            {
                throw AssociationFailure;
            }
            if (AssociationStatus == PreferencesStatus.Success)
            {
                Reference = imageReference;
            }
            return AssociationStatus;
        }
    }
}
