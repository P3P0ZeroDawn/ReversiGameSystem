using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.ServiceModel;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Preferences;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Players;
using ReversiGameSystem.Server.Persistence.Preferences;
using ReversiGameSystem.Server.ServiceApi;
using ContractPreferences = ReversiGameSystem.Server.Contracts.Players.PlayerPreferences;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Server.Tests;

internal static class PreferencesDatabaseTests
{
    private const string DatabasePrefix = "ReversiReviewAccountTests_";
    private const string ConnectionVariable = "REVERSI_DB_CONNECTION_STRING";
    private const string MigrationFile = "20261121_CustomBoardBackgroundPreference.sql";
    private const string ExistingReference = "board-backgrounds/existing.png";
    private const string TestAddress = "net.tcp://localhost:18098/ReversiReview/PlayerPreferences";

    internal static void Run()
    {
        ReviewAccountDatabaseTests.RunWithReviewAccounts(VerifyPreferences);
    }

    private static void VerifyPreferences(SessionApplicationService authentication,
        SessionStore sessions)
    {
        AuthenticatedSession alpha = authentication.CompleteLogin(
            "ReviewAlpha", "000001").Value!;
        authentication.CompleteLogin("ReviewBeta", "000002");
        ValidateDisposableConnection();
        const string SeedBackgroundSql =
            "UPDATE dbo.AccountSettings SET boardBackgroundUri = @reference WHERE accountId = @id";
        Execute(SeedBackgroundSql,
            new SqlParameter("@reference", ExistingReference),
            new SqlParameter("@id", alpha.AccountId));
        string migration = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "migrations", MigrationFile));
        Execute(migration);
        Execute(migration);

        PreferencesRepository repository = new PreferencesRepository();
        PlayerPreferences preferences = repository.ReadPreferences(alpha.AccountId).Preferences;
        Assert.AreEqual(true, preferences.UseCustomBoardBackground);
        string directory = Path.Combine(Path.GetTempPath(), "ReversiPreferences-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            ProfileImageRepository images = new ProfileImageRepository(
                directory, "board-backgrounds/");
            PreferencesApplicationService application = new PreferencesApplicationService(
                sessions, repository, images);
            preferences.PreferredLocaleCode = "en-US";
            preferences.TimeZoneId = "UTC";
            preferences.PieceStyleCode = "PROFILE_PICTURES";
            preferences.UseCustomBoardBackground = false;
            Assert.AreEqual(PreferencesStatus.Success,
                application.UpdatePreferences(alpha.SessionToken, preferences));
            PlayerPreferences stored = repository.ReadPreferences(alpha.AccountId).Preferences;
            Assert.AreEqual(ExistingReference, stored.BoardBackgroundReference);
            Assert.AreEqual(false, stored.UseCustomBoardBackground);
            Assert.AreEqual("en-US", new AccountRepository(new TestUtcClock())
                .FindByLogin("ReviewAlpha").Value?.PreferredLocaleCode);

            ProfileImageData image = CreateImage();
            Assert.AreEqual(PreferencesStatus.Success,
                application.ChangeBoardBackground(alpha.SessionToken, image));
            stored = repository.ReadPreferences(alpha.AccountId).Preferences;
            Assert.AreEqual(true, stored.UseCustomBoardBackground);
            Assert.AreEqual(true,
                stored.BoardBackgroundReference.StartsWith(
                    "board-backgrounds/", StringComparison.Ordinal));
            stored.UseCustomBoardBackground = false;
            Assert.AreEqual(PreferencesStatus.Success,
                application.UpdatePreferences(alpha.SessionToken, stored));
            PreferencesAccess background = application.GetBoardBackground(alpha.SessionToken);
            Assert.AreEqual(true, background.BackgroundImage != null);
            Assert.AreEqual(false, background.Preferences.UseCustomBoardBackground);
            Assert.AreEqual(stored.BoardBackgroundReference,
                background.Preferences.BoardBackgroundReference);
            VerifyRemoteBoundary(application, repository, alpha);
            Assert.AreEqual(PreferencesStatus.InactiveAccount,
                repository.ReadPreferences(long.MaxValue).Status);
            Console.WriteLine(
                "Passed preferences migration, SQL persistence, background and WCF checks.");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void VerifyRemoteBoundary(PreferencesApplicationService application,
        PreferencesRepository repository, AuthenticatedSession session)
    {
        using ServiceHost host = new ServiceHost(new PlayerPreferencesService(application));
        host.AddServiceEndpoint(typeof(IPlayerPreferencesService),
            new NetTcpBinding(SecurityMode.None), TestAddress);
        host.Open();
        using ChannelFactory<IPlayerPreferencesService> factory =
            new ChannelFactory<IPlayerPreferencesService>(new NetTcpBinding(SecurityMode.None),
                new EndpointAddress(TestAddress));
        IPlayerPreferencesService client = factory.CreateChannel();
        try
        {
            ProfileImageData uploaded = CreateImage();
            client.ChangeBoardBackground(session.SessionToken, new ImageContent
            {
                Content = uploaded.Content,
                MediaType = uploaded.MediaType
            });
            ContractPreferences preferences = client.GetPlayerPreferences(session.SessionToken);
            preferences.UseCustomBoardBackground = false;
            preferences.PreferredLocaleCode = "es-MX";
            client.UpdatePlayerPreferences(session.SessionToken, preferences);
            PlayerPreferences stored = repository.ReadPreferences(session.AccountId).Preferences;
            Assert.AreEqual("es-MX", stored.PreferredLocaleCode);
            Assert.AreEqual("UTC", stored.TimeZoneId);
            Assert.AreEqual("PROFILE_PICTURES", stored.PieceStyleCode);
            Assert.AreEqual(false, stored.UseCustomBoardBackground);
            Assert.AreEqual(true, client.GetBoardBackground(session.SessionToken) != null);
            ((IClientChannel)client).Close();
            factory.Close();
            host.Close();
        }
        finally
        {
            ((IClientChannel)client).Abort();
            factory.Abort();
            host.Abort();
        }
    }

    private static ProfileImageData CreateImage()
    {
        using Bitmap bitmap = new Bitmap(2, 2);
        using MemoryStream content = new MemoryStream();
        bitmap.Save(content, ImageFormat.Png);
        return new ProfileImageData(content.ToArray(), "image/png");
    }

    private static void ValidateDisposableConnection()
    {
        SqlConnectionStringBuilder connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable(ConnectionVariable));
        string name = connection.InitialCatalog;
        if (!name.StartsWith(DatabasePrefix, StringComparison.Ordinal)
            || !Guid.TryParseExact(name.Substring(DatabasePrefix.Length), "N", out _))
        {
            throw new InvalidOperationException("A disposable preferences database is required.");
        }
    }

    private static void Execute(string sql, params SqlParameter[] parameters)
    {
        ValidateDisposableConnection();
        using SqlConnection connection = new SqlConnection(
            Environment.GetEnvironmentVariable(ConnectionVariable));
        connection.Open();
        using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        command.ExecuteNonQuery();
    }
}
