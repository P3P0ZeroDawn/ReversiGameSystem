using System;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using ReversiGameSystem.Server.Application.Preferences;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Preferences;

public sealed class PreferencesRepository : IPreferencesRepository
{
    private const string ActiveAccountStatus = "ACTIVE";
    private const string DefaultLocale = "en-US";
    private const string DefaultPieceStyle = "STANDARD";
    private const string ReadSql = @"
SELECT COALESCE(settings.preferredLocaleCode, @locale) AS PreferredLocaleCode,
       settings.timeZoneId AS TimeZoneId,
       COALESCE(settings.pieceStyleCode, @style) AS PieceStyleCode,
       ISNULL(settings.useCustomBoardBackground, 0) AS UseCustomBoardBackground,
       COALESCE(settings.boardBackgroundUri, '') AS BoardBackgroundReference
FROM dbo.Account AS account
LEFT JOIN dbo.AccountSettings AS settings ON settings.accountId = account.accountId
WHERE account.accountId = @accountId AND account.statusCode = @active";
    private const string EnsureSettingsSql = @"
IF NOT EXISTS (SELECT 1 FROM dbo.AccountSettings WHERE accountId = @accountId)
INSERT INTO dbo.AccountSettings
(accountId, preferredLocaleCode, pieceStyleCode, useCustomBoardBackground)
VALUES (@accountId, @locale, @style, 0)";
    private const string ActiveAccountSql = @"
SELECT COUNT(*) FROM dbo.Account WHERE accountId = @accountId AND statusCode = @active";
    private const string UpdateSql = @"
UPDATE dbo.AccountSettings
SET preferredLocaleCode = @locale, timeZoneId = @timeZone,
    pieceStyleCode = @style, useCustomBoardBackground = @custom
WHERE accountId = @accountId";
    private const string UpdateBackgroundSql = @"
UPDATE dbo.AccountSettings
SET boardBackgroundUri = @reference, useCustomBoardBackground = 1
WHERE accountId = @accountId";

    public PreferencesAccess ReadPreferences(long accountId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        PlayerPreferences? preferences = context.Database.SqlQuery<PlayerPreferences>(ReadSql,
            new SqlParameter("@locale", DefaultLocale),
            new SqlParameter("@style", DefaultPieceStyle),
            new SqlParameter("@accountId", accountId),
            new SqlParameter("@active", ActiveAccountStatus)).SingleOrDefault();
        PreferencesAccess access;
        if (preferences == null)
        {
            access = new PreferencesAccess(PreferencesStatus.InactiveAccount);
        }
        else
        {
            access = new PreferencesAccess(preferences);
        }
        return access;
    }

    public PreferencesStatus SavePreferences(long accountId, PlayerPreferences preferences)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(
            IsolationLevel.Serializable);
        PreferencesStatus status = PreferencesStatus.InactiveAccount;
        if (IsActive(context, accountId))
        {
            EnsureSettings(context, accountId);
            context.Database.ExecuteSqlCommand(UpdateSql,
                new SqlParameter("@accountId", accountId),
                new SqlParameter("@locale", preferences.PreferredLocaleCode),
                new SqlParameter("@timeZone", (object?)preferences.TimeZoneId ?? DBNull.Value),
                new SqlParameter("@style", preferences.PieceStyleCode),
                new SqlParameter("@custom", preferences.UseCustomBoardBackground));
            transaction.Commit();
            status = PreferencesStatus.Success;
        }
        return status;
    }

    public PreferencesStatus SetBoardBackground(long accountId, string imageReference)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(
            IsolationLevel.Serializable);
        PreferencesStatus status = PreferencesStatus.InactiveAccount;
        if (IsActive(context, accountId))
        {
            EnsureSettings(context, accountId);
            context.Database.ExecuteSqlCommand(UpdateBackgroundSql,
                new SqlParameter("@accountId", accountId),
                new SqlParameter("@reference", imageReference));
            transaction.Commit();
            status = PreferencesStatus.Success;
        }
        return status;
    }

    private static bool IsActive(ReversiGameSystemEntities context, long accountId)
    {
        int count = context.Database.SqlQuery<int>(ActiveAccountSql,
            new SqlParameter("@accountId", accountId),
            new SqlParameter("@active", ActiveAccountStatus)).Single();
        return count > 0;
    }

    private static void EnsureSettings(ReversiGameSystemEntities context, long accountId)
    {
        context.Database.ExecuteSqlCommand(EnsureSettingsSql,
            new SqlParameter("@accountId", accountId),
            new SqlParameter("@locale", DefaultLocale),
            new SqlParameter("@style", DefaultPieceStyle));
    }
}
