using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Application.Preferences;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Sessions;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class PreferencesApplicationServiceTests
{
    private const string ImageReference = "board-backgrounds/test.png";

    [TestMethod]
    public void GetPreferences_MissingToken_ReturnsMissingSessionToken()
    {
        PreferencesFixture fixture = new PreferencesFixture();

        PreferencesAccess access = fixture.Service.GetPreferences(string.Empty);

        Assert.AreEqual(PreferencesStatus.MissingSessionToken, access.Status);
    }

    [TestMethod]
    public void GetPreferences_RevokedSession_ReturnsInvalidSession()
    {
        PreferencesFixture fixture = new PreferencesFixture();
        fixture.Sessions.Revoke(fixture.Session.SessionToken);

        PreferencesAccess access = fixture.Service.GetPreferences(fixture.Session.SessionToken);

        Assert.AreEqual(PreferencesStatus.InvalidSession, access.Status);
    }

    [TestMethod]
    public void PreferencesAccess_RejectedRequest_CannotReadBackgroundAsAbsent()
    {
        PreferencesAccess access = new PreferencesAccess(PreferencesStatus.InvalidSession);

        Assert.Throws<InvalidOperationException>(() => access.BackgroundImage);
    }

    [TestMethod]
    public void UpdatePreferences_InactiveAccount_DoesNotWrite()
    {
        PreferencesFixture fixture = new PreferencesFixture();
        fixture.Repository.IsActiveAccount = false;

        fixture.Service.UpdatePreferences(fixture.Session.SessionToken, CreatePreferences());

        Assert.AreEqual(0, fixture.Repository.WriteCount);
    }

    [TestMethod]
    public void UpdatePreferences_UnsupportedLocale_DoesNotWrite()
    {
        PreferencesFixture fixture = new PreferencesFixture();
        PlayerPreferences preferences = CreatePreferences();
        preferences.PreferredLocaleCode = "fr-FR";

        fixture.Service.UpdatePreferences(fixture.Session.SessionToken, preferences);

        Assert.AreEqual(0, fixture.Repository.WriteCount);
    }

    [TestMethod]
    public void UpdatePreferences_InvalidTimeZone_ReturnsInvalidTimeZone()
    {
        PreferencesFixture fixture = new PreferencesFixture();
        PlayerPreferences preferences = CreatePreferences();
        preferences.TimeZoneId = "NotAnExistingTimeZone";

        PreferencesStatus status = fixture.Service.UpdatePreferences(
            fixture.Session.SessionToken, preferences);

        Assert.AreEqual(PreferencesStatus.InvalidTimeZone, status);
    }

    [TestMethod]
    public void UpdatePreferences_UnsupportedPieceStyle_ReturnsInvalidPieceStyle()
    {
        PreferencesFixture fixture = new PreferencesFixture();
        PlayerPreferences preferences = CreatePreferences();
        preferences.PieceStyleCode = "UNKNOWN";

        PreferencesStatus status = fixture.Service.UpdatePreferences(
            fixture.Session.SessionToken, preferences);

        Assert.AreEqual(PreferencesStatus.InvalidPieceStyle, status);
    }

    [TestMethod]
    public void UpdatePreferences_ValidInput_PersistsLanguage()
    {
        PreferencesFixture fixture = new PreferencesFixture();
        PlayerPreferences preferences = CreatePreferences();
        preferences.PreferredLocaleCode = "en-US";

        fixture.Service.UpdatePreferences(fixture.Session.SessionToken, preferences);

        Assert.AreEqual("en-US", fixture.Repository.Preferences.PreferredLocaleCode);
    }

    [TestMethod]
    public void GetBoardBackground_DisabledBackground_ReturnsStoredImage()
    {
        PreferencesFixture fixture = new PreferencesFixture();
        fixture.Repository.Preferences.UseCustomBoardBackground = false;
        fixture.Repository.Preferences.BoardBackgroundReference = ImageReference;

        PreferencesAccess access = fixture.Service.GetBoardBackground(fixture.Session.SessionToken);

        Assert.AreSame(fixture.Images.Image, access.BackgroundImage);
    }

    [TestMethod]
    public void ChangeBoardBackground_InvalidImage_DoesNotWrite()
    {
        PreferencesFixture fixture = new PreferencesFixture();
        ProfileImageData image = new ProfileImageData(Array.Empty<byte>(), "image/png");

        fixture.Service.ChangeBoardBackground(fixture.Session.SessionToken, image);

        Assert.AreEqual(0, fixture.Repository.WriteCount);
    }

    [TestMethod]
    public void ChangeBoardBackground_AssociationFails_PreservesOriginalException()
    {
        PreferencesFixture fixture = new PreferencesFixture();
        IOException failure = new IOException("The association failed.");
        fixture.Repository.AssociationFailure = failure;

        IOException reported = Assert.Throws<IOException>(() =>
            fixture.Service.ChangeBoardBackground(
                fixture.Session.SessionToken, fixture.Images.Image));

        Assert.AreSame(failure, reported);
        Assert.AreEqual(ImageReference, fixture.Images.DeletedReference);
    }

    [TestMethod]
    public void ChangeBoardBackground_CleanupAlsoFails_PreservesBothExceptions()
    {
        PreferencesFixture fixture = new PreferencesFixture();
        IOException associationFailure = new IOException("The association failed.");
        UnauthorizedAccessException cleanupFailure =
            new UnauthorizedAccessException("The cleanup failed.");
        fixture.Repository.AssociationFailure = associationFailure;
        fixture.Images.CleanupFailure = cleanupFailure;

        AggregateException reported = Assert.Throws<AggregateException>(() =>
            fixture.Service.ChangeBoardBackground(
                fixture.Session.SessionToken, fixture.Images.Image));

        Assert.AreSame(associationFailure, reported.InnerExceptions[0]);
        Assert.AreSame(cleanupFailure, reported.InnerExceptions[1]);
    }

    private static PlayerPreferences CreatePreferences()
    {
        return new PlayerPreferences
        {
            PreferredLocaleCode = "es-MX",
            PieceStyleCode = "STANDARD",
            TimeZoneId = null
        };
    }

    private sealed class PreferencesFixture
    {
        internal SessionRepository Sessions { get; } = new SessionRepository(new SystemUtcClock());
        internal PreferencesRepositoryStub Repository { get; } = new PreferencesRepositoryStub();
        internal ImageRepositoryStub Images { get; } = new ImageRepositoryStub();
        internal AuthenticatedSession Session { get; }
        internal PreferencesApplicationService Service { get; }

        internal PreferencesFixture()
        {
            Session = Sessions.Create(new LoginAccount(
                1, "PreferencesPlayer", "player@example.invalid", "unused", "ACTIVE", "es-MX"));
            Service = new PreferencesApplicationService(Sessions, Repository, Images);
        }
    }

    private sealed class PreferencesRepositoryStub : IPreferencesRepository
    {
        internal PlayerPreferences Preferences { get; private set; } = CreatePreferences();
        internal int WriteCount { get; private set; }
        internal bool IsActiveAccount { get; set; } = true;
        internal Exception? AssociationFailure { get; set; }

        public PreferencesAccess ReadPreferences(long accountId)
        {
            PreferencesAccess access = IsActiveAccount
                ? new PreferencesAccess(Preferences)
                : new PreferencesAccess(PreferencesStatus.InactiveAccount);
            return access;
        }

        public PreferencesStatus SavePreferences(long accountId, PlayerPreferences preferences)
        {
            Preferences = preferences;
            WriteCount++;
            return PreferencesStatus.Success;
        }

        public PreferencesStatus SetBoardBackground(long accountId, string imageReference)
        {
            if (AssociationFailure != null)
            {
                throw AssociationFailure;
            }
            WriteCount++;
            return PreferencesStatus.Success;
        }
    }

    private sealed class ImageRepositoryStub : IPlayerImageRepository
    {
        internal ProfileImageData Image { get; } = new ProfileImageData(
            new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, "image/png");
        internal string DeletedReference { get; private set; } = string.Empty;
        internal Exception? CleanupFailure { get; set; }

        public ProfileImageData ReadImage(string imageReference)
        {
            return Image;
        }

        public ProfileResult<string> SaveImage(ProfileImageData image)
        {
            return new ProfileResult<string>(ImageReference);
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
}
