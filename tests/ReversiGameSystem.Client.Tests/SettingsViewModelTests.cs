using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Features.Preferences;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
[DoNotParallelize]
public sealed class SettingsViewModelTests
{
    [TestMethod]
    public async Task ChangePieceStyle_FailedSave_PreservesConfirmedPreferences()
    {
        ClientSession session = CreateSession();
        PreferencesClientFake client = new PreferencesClientFake();
        SettingsViewModel model = new SettingsViewModel(client, session);
        await model.LoadPreferences();
        client.WriteStatus = PreferencesStatus.ServiceUnavailable;

        PreferencesStatus status = await model.ChangePieceStyle(true);

        Assert.AreEqual(PreferencesStatus.ServiceUnavailable, status);
        Assert.IsFalse(model.UseProfilePicturesOnPieces);
        Assert.IsFalse(UserPreferencesState.UseProfilePicturesOnPieces);
        Assert.IsFalse(model.IsSaving);
    }

    [TestMethod]
    public async Task LoadPreferences_ConfirmedLoad_DoesNotSaveDefaults()
    {
        ClientSession session = CreateSession();
        PreferencesClientFake client = new PreferencesClientFake();
        client.Preferences.PieceStyleCode = PlayerPreferences.ProfilePicturesPieceStyle;
        SettingsViewModel model = new SettingsViewModel(client, session);

        await model.LoadPreferences();
        await model.ChangePieceStyle(true);

        Assert.IsTrue(model.UseProfilePicturesOnPieces);
        Assert.AreEqual(0, client.WriteCalls);
        Assert.IsTrue(model.CanEdit);
    }

    [TestMethod]
    public async Task ChangePreferences_Guest_ChangesOnlyLocalState()
    {
        ClientSession session = new ClientSession();
        session.StartGuest(new GuestAccountSession("guest", "Visitor", DateTime.UtcNow.AddHours(
            1)));
        PreferencesClientFake client = new PreferencesClientFake();
        SettingsViewModel model = new SettingsViewModel(client, session);

        await model.LoadPreferences();
        await model.ChangePieceStyle(true);
        await model.ChangeCustomBackground(true);
        await model.ChangeLanguage("es-MX");

        Assert.IsTrue(model.UseProfilePicturesOnPieces);
        Assert.IsTrue(model.UseCustomBoardBackground);
        Assert.AreEqual(0, client.Calls);
    }

    [TestMethod]
    public async Task LoadPreferences_SessionDiscardedDuringAwait_RestoresFlagsWithoutReadingCache()
    {
        ClientSession session = CreateSession();
        PreferencesClientFake client = new PreferencesClientFake();
        TaskCompletionSource<PreferencesStatus> pending
            = new TaskCompletionSource<PreferencesStatus>();
        client.PendingLoad = pending.Task;
        SettingsViewModel model = new SettingsViewModel(client, session);

        Task<PreferencesStatus> operation = model.LoadPreferences();
        Assert.IsTrue(model.IsLoading);
        session.Clear();
        client.DiscardSession();
        pending.SetResult(PreferencesStatus.Success);
        PreferencesStatus status = await operation;

        Assert.AreEqual(PreferencesStatus.InvalidSession, status);
        Assert.IsFalse(model.IsLoading);
        Assert.IsFalse(model.IsLoaded);
        Assert.IsFalse(model.CanEdit);
        Assert.AreEqual(0, client.ReadCalls);
    }

    [TestMethod]
    public async Task ChangeCustomBackground_LoadedTimeZone_PreservesTimeZone()
    {
        ClientSession session = CreateSession();
        PreferencesClientFake client = new PreferencesClientFake();
        client.Preferences.TimeZoneId = "Central Standard Time (Mexico)";
        SettingsViewModel model = new SettingsViewModel(client, session);
        await model.LoadPreferences();

        await model.ChangeCustomBackground(true);

        Assert.AreEqual("Central Standard Time (Mexico)", client.Preferences.TimeZoneId);
        Assert.IsTrue(model.UseCustomBoardBackground);
    }

    [TestMethod]
    public async Task ChangeBoardBackground_ServerReadback_DisplaysActualConfirmedImage()
    {
        ClientSession session = CreateSession();
        PreferencesClientFake client = new PreferencesClientFake();
        SettingsViewModel model = new SettingsViewModel(client, session);
        await model.LoadPreferences();
        PlayerImageContent selected = CreateImage(Colors.Red);
        client.UploadReadback = CreateImage(Colors.Blue);

        PreferencesStatus status = await model.ChangeBoardBackground(selected);

        Assert.AreEqual(PreferencesStatus.Success, status);
        BitmapSource? preview = model.BackgroundPreview;
        Assert.IsNotNull(preview);
        byte[] pixels = new byte[16];
        preview.CopyPixels(pixels, 8, 0);
        Assert.AreEqual((byte)255, pixels[0]);
        Assert.AreEqual((byte)0, pixels[2]);
        Assert.IsTrue(model.UseCustomBoardBackground);
    }

    [TestMethod]
    public async Task ChangePieceStyle_UnexpectedFailure_RestoresSavingFlag()
    {
        ClientSession session = CreateSession();
        PreferencesClientFake client = new PreferencesClientFake();
        SettingsViewModel model = new SettingsViewModel(client, session);
        await model.LoadPreferences();
        client.ThrowOnWrite = true;

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => model.ChangePieceStyle(
            true));

        Assert.IsFalse(model.IsSaving);
        Assert.IsFalse(model.UseProfilePicturesOnPieces);
    }

    [TestMethod]
    public async Task ChangePieceStyle_SessionChangedDuringSave_DoesNotApplyPreviousPlayerState()
    {
        ClientSession session = CreateSession();
        PreferencesClientFake client = new PreferencesClientFake();
        SettingsViewModel model = new SettingsViewModel(client, session);
        await model.LoadPreferences();
        TaskCompletionSource<PreferencesStatus> pending
            = new TaskCompletionSource<PreferencesStatus>();
        client.PendingWrite = pending.Task;
        int readsBeforeSave = client.ReadCalls;

        Task<PreferencesStatus> operation = model.ChangePieceStyle(true);
        Assert.IsTrue(model.IsSaving);
        session.StartAuthenticated(new AuthenticatedAccountSession(
            "other-token", 43, "Other", "other@example.test", "en-US", DateTime.UtcNow.AddHours(
                1)));
        client.DiscardSession();
        pending.SetResult(PreferencesStatus.Success);
        PreferencesStatus status = await operation;

        Assert.AreEqual(PreferencesStatus.InvalidSession, status);
        Assert.IsFalse(model.IsSaving);
        Assert.IsFalse(UserPreferencesState.UseProfilePicturesOnPieces);
        Assert.AreEqual(readsBeforeSave, client.ReadCalls);
    }

    [TestMethod]
    public async Task ChangeLanguage_BeforeSettingsLoad_PreservesStoredAppearance()
    {
        ClientSession session = CreateSession();
        PreferencesClientFake client = new PreferencesClientFake();
        client.Preferences.PieceStyleCode = PlayerPreferences.ProfilePicturesPieceStyle;
        client.Preferences.TimeZoneId = "UTC";
        SettingsViewModel model = new SettingsViewModel(client, session);

        PreferencesStatus status = await model.ChangeLanguage("es-MX");

        Assert.AreEqual(PreferencesStatus.Success, status);
        Assert.AreEqual("es-MX", client.Preferences.PreferredLocaleCode);
        Assert.AreEqual("UTC", client.Preferences.TimeZoneId);
        Assert.IsTrue(model.UseProfilePicturesOnPieces);
    }

    [TestMethod]
    public void PreferencesService_ClearedSession_DiscardsPreviousClient()
    {
        ClientRuntime.CurrentSession.Clear();
        PlayerPreferencesClient previousClient = ClientRuntime.PreferencesService;
        try
        {
            ClientRuntime.CurrentSession.Clear();

            Assert.AreNotSame(previousClient, ClientRuntime.PreferencesService);
            Assert.ThrowsExactly<InvalidOperationException>(() => previousClient.ReadPreferences());
        }
        finally
        {
            ClientRuntime.CurrentSession.Clear();
        }
    }

    [TestMethod]
    public void SetBoardBackground_DisabledPreference_DoesNotEnableBackground()
    {
        UserPreferencesState.Reset();
        PlayerImageContent image = CreateImage(Colors.Red);
        BitmapSource preview = PlayerImageService.CreatePreview(image)
            ?? throw new InvalidOperationException("The test image must have a preview.");
        try
        {
            UserPreferencesState.SetBoardBackground(preview);

            Assert.IsFalse(UserPreferencesState.UseCustomBoardBackground);
            Assert.AreSame(preview, UserPreferencesState.BoardBackgroundPreview);
        }
        finally
        {
            UserPreferencesState.Reset();
        }
    }

    [TestMethod]
    public async Task LoadPreferences_PendingLoad_ReturnsOperationInProgress()
    {
        ClientSession session = CreateSession();
        PreferencesClientFake client = new PreferencesClientFake();
        TaskCompletionSource<PreferencesStatus> pending =
            new TaskCompletionSource<PreferencesStatus>();
        client.PendingLoad = pending.Task;
        SettingsViewModel model = new SettingsViewModel(client, session);
        Task<PreferencesStatus> loading = model.LoadPreferences();
        try
        {
            PreferencesStatus status = await model.LoadPreferences();

            Assert.AreEqual(PreferencesStatus.OperationInProgress, status);
            Assert.AreEqual(1, client.Calls);
            Assert.IsFalse(model.CanEdit);
        }
        finally
        {
            pending.SetResult(PreferencesStatus.Success);
            await loading;
        }
    }

    [TestMethod]
    public async Task ChangePieceStyle_PendingSave_DoesNotRepeatWrite()
    {
        ClientSession session = CreateSession();
        PreferencesClientFake client = new PreferencesClientFake();
        SettingsViewModel model = new SettingsViewModel(client, session);
        await model.LoadPreferences();
        TaskCompletionSource<PreferencesStatus> pending =
            new TaskCompletionSource<PreferencesStatus>();
        client.PendingWrite = pending.Task;
        Task<PreferencesStatus> saving = model.ChangePieceStyle(true);
        try
        {
            PreferencesStatus status = await model.ChangePieceStyle(true);

            Assert.AreEqual(PreferencesStatus.OperationInProgress, status);
            Assert.AreEqual(1, client.WriteCalls);
        }
        finally
        {
            pending.SetResult(PreferencesStatus.Success);
            await saving;
        }
    }

    private static ClientSession CreateSession()
    {
        ClientSession session = new ClientSession();
        session.StartAuthenticated(new AuthenticatedAccountSession(
            "settings-token", 42, "Player", "player@example.test", "en-US",
                DateTime.UtcNow.AddHours(1)));
        return session;
    }

    private static PlayerImageContent CreateImage(Color color)
    {
        byte[] pixels = new byte[16];
        for (int index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = color.B;
            pixels[index + 1] = color.G;
            pixels[index + 2] = color.R;
            pixels[index + 3] = 255;
        }
        BitmapSource source = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null,
            pixels, 8);
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using (MemoryStream stream = new MemoryStream())
        {
            encoder.Save(stream);
            return new PlayerImageContent(stream.ToArray(), "image/png");
        }
    }

    private sealed class PreferencesClientFake : IPlayerPreferencesClient
    {
        internal PlayerPreferences Preferences { get; set; } = new PlayerPreferences();

        internal PreferencesStatus WriteStatus { get; set; } = PreferencesStatus.Success;

        internal Task<PreferencesStatus>? PendingLoad { get; set; }

        internal Task<PreferencesStatus>? PendingWrite { get; set; }

        internal PlayerImageContent? UploadReadback { get; set; }

        internal PlayerImageContent? Background { get; set; }

        internal bool ThrowOnWrite { get; set; }

        internal int Calls { get; set; }

        internal int WriteCalls { get; set; }

        internal int ReadCalls { get; set; }

        public bool HasBoardBackground
        {
            get { return Background != null; }
        }

        public Task<PreferencesStatus> LoadPreferences(string sessionToken)
        {
            Calls++;
            return PendingLoad ?? Task.FromResult(PreferencesStatus.Success);
        }

        public PlayerPreferences ReadPreferences()
        {
            ReadCalls++;
            return new PlayerPreferences
            {
                PreferredLocaleCode = Preferences.PreferredLocaleCode,
                TimeZoneId = Preferences.TimeZoneId,
                PieceStyleCode = Preferences.PieceStyleCode,
                UseCustomBoardBackground = Preferences.UseCustomBoardBackground
            };
        }

        public Task<PreferencesStatus> UpdatePreferences(string sessionToken,
            PlayerPreferences preferences)
        {
            Calls++;
            WriteCalls++;
            if (ThrowOnWrite)
            {
                throw new InvalidOperationException("Unexpected write failure.");
            }
            if (WriteStatus == PreferencesStatus.Success)
            {
                Preferences = preferences;
            }
            return PendingWrite ?? Task.FromResult(WriteStatus);
        }

        public Task<PreferencesStatus> LoadBoardBackground(string sessionToken)
        {
            Calls++;
            return Task.FromResult(PreferencesStatus.Success);
        }

        public PlayerImageContent ReadBoardBackground()
        {
            ReadCalls++;
            return Background ?? throw new InvalidOperationException("No confirmed background.");
        }

        public Task<PreferencesStatus> ChangeBoardBackground(string sessionToken,
            PlayerImageContent image)
        {
            Calls++;
            WriteCalls++;
            if (WriteStatus == PreferencesStatus.Success)
            {
                Background = UploadReadback ?? image;
                Preferences.UseCustomBoardBackground = true;
            }
            return Task.FromResult(WriteStatus);
        }

        public Task<PreferencesStatus> ChangeLanguage(string sessionToken, string localeCode)
        {
            Calls++;
            WriteCalls++;
            if (WriteStatus == PreferencesStatus.Success)
            {
                Preferences.PreferredLocaleCode = localeCode;
            }
            return Task.FromResult(WriteStatus);
        }

        public void DiscardSession()
        {
            Background = null;
            Preferences = new PlayerPreferences();
        }
    }
}
