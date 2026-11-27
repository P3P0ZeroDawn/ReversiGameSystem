using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Players;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.SystemTests;

[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
[TestCategory("System")]
[TestCategory("Database")]
public sealed class AccountOnboardingSystemTests
{
    private const string InitialUsername = "e2ePlayerOne";
    private const string UpdatedUsername = "e2ePlayerUpdated";
    private const string PlayerEmail = "e2e.player@example.com";
    private const string PlayerPassword = "StrongPassword1!";
    private const string SpanishLocaleCode = "es-MX";
    private const string PngMediaType = "image/png";
    private const string JpegMediaType = "image/jpeg";
    private const string StandardPieceStyleCode = "STANDARD";
    private const string SampleTimeZone = "UTC";

    private static SystemTestHost? _host;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _host = new SystemTestHost();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _host?.Dispose();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _host?.ResetState();
    }

    [TestMethod]
    public void OnboardingJourney_RegisterLoginUpdateProfileAndRelogin_PersistsAcrossLayers()
    {
        if (_host == null)
        {
            Assert.Fail("SystemTestHost was not initialized.");
            return;
        }

        byte[] avatarBytes = CreatePngBytes(Color.DarkSlateBlue);
        byte[] backgroundBytes = CreatePngBytes(Color.ForestGreen);

        using SystemTestHost.ClientChannelScope<IAccountService> accountScope =
            _host.OpenChannel<IAccountService>();
        using SystemTestHost.ClientChannelScope<ISessionService> sessionScope =
            _host.OpenChannel<ISessionService>();
        using SystemTestHost.ClientChannelScope<IPlayerProfileService> profileScope =
            _host.OpenChannel<IPlayerProfileService>();
        using SystemTestHost.ClientChannelScope<IPlayerPreferencesService> preferencesScope =
            _host.OpenChannel<IPlayerPreferencesService>();

        accountScope.Channel.RequestRegistrationCode(InitialUsername, PlayerEmail);
        string registrationCode = _host.GetLatestVerificationCode(PlayerEmail);
        AccountReference registered = accountScope.Channel.CompleteRegistration(new NewAccount
        {
            Username = InitialUsername,
            EmailAddress = PlayerEmail,
            Password = PlayerPassword,
            PreferredLocaleCode = SpanishLocaleCode,
            VerificationCode = registrationCode,
            ProfileImage = new ImageContent
            {
                MediaType = PngMediaType,
                Content = avatarBytes
            }
        });

        sessionScope.Channel.RequestLoginCode(InitialUsername, PlayerPassword);
        string firstLoginCode = _host.GetLatestVerificationCode(PlayerEmail);
        AuthenticatedSession firstSession = sessionScope.Channel.CompleteLogin(
            InitialUsername,
            firstLoginCode);

        profileScope.Channel.ChangeUsername(firstSession.SessionToken, UpdatedUsername);
        preferencesScope.Channel.UpdatePlayerPreferences(
            firstSession.SessionToken,
            new PlayerPreferences
            {
                PreferredLocaleCode = SpanishLocaleCode,
                TimeZoneId = SampleTimeZone,
                PieceStyleCode = StandardPieceStyleCode
            });
        preferencesScope.Channel.ChangeBoardBackground(
            firstSession.SessionToken,
            new ImageContent
            {
                MediaType = PngMediaType,
                Content = backgroundBytes
            });
        sessionScope.Channel.CloseSession(firstSession.SessionToken);

        sessionScope.Channel.RequestLoginCode(UpdatedUsername, PlayerPassword);
        string secondLoginCode = _host.GetLatestVerificationCode(PlayerEmail);
        AuthenticatedSession actualSecondSession = sessionScope.Channel.CompleteLogin(
            UpdatedUsername,
            secondLoginCode);
        PlayerProfile actualProfile = profileScope.Channel.GetPlayerProfile(
            actualSecondSession.SessionToken,
            null);
        ImageContent? nullableAvatar = profileScope.Channel.GetProfileImage(
            actualSecondSession.SessionToken,
            null);
        PlayerPreferences actualPreferences = preferencesScope.Channel.GetPlayerPreferences(
            actualSecondSession.SessionToken);
        ImageContent? nullableBackground = preferencesScope.Channel.GetBoardBackground(
            actualSecondSession.SessionToken);

        ImageContent actualAvatar = TestAssert.IsNotNull(nullableAvatar);
        ImageContent actualBackground = TestAssert.IsNotNull(nullableBackground);
        Assert.AreEqual(registered.AccountId, actualSecondSession.Account.AccountId);
        Assert.AreEqual(UpdatedUsername, actualProfile.Account.Username);
        Assert.IsTrue(actualProfile.HasProfileImage);
        Assert.AreEqual(JpegMediaType, actualAvatar.MediaType);
        Assert.IsTrue(actualAvatar.Content.Length > 0);
        Assert.AreEqual(SpanishLocaleCode, actualPreferences.PreferredLocaleCode);
        Assert.AreEqual(SampleTimeZone, actualPreferences.TimeZoneId);
        Assert.IsTrue(actualPreferences.UseCustomBoardBackground);
        Assert.AreEqual(JpegMediaType, actualBackground.MediaType);
        Assert.IsTrue(actualBackground.Content.Length > 0);
    }

    private static byte[] CreatePngBytes(Color fillColor)
    {
        using Bitmap bitmap = new Bitmap(2, 2);
        bitmap.SetPixel(0, 0, fillColor);
        using MemoryStream stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
