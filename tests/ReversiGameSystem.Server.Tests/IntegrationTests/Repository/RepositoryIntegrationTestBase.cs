using System;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

public abstract class RepositoryIntegrationTestBase
{
    private const string DefaultPasswordHash = "HASH_FOR_TEST_ACCOUNT";
    private const string DefaultLocaleCode = "en-US";
    private const string DefaultPieceStyleCode = "STANDARD";

    internal static LocalDbTestDatabase InitializeDatabase()
    {
        return new LocalDbTestDatabase();
    }

    internal static void CleanupDatabase(LocalDbTestDatabase? database)
    {
        database?.Dispose();
    }

    internal static ReversiGameSystemEntities CreateFreshContext()
    {
        return new ReversiGameSystemEntities();
    }

    internal static long SeedAccount(
        string username,
        string emailAddress,
        string statusCode = "ACTIVE")
    {
        return InsertAccount(new Account
        {
            username = username,
            emailAddress = emailAddress,
            passwordHash = DefaultPasswordHash,
            statusCode = statusCode,
            registeredAtUtc = DateTime.UtcNow
        });
    }

    internal static long SeedAccountWithImage(
        string username,
        string emailAddress,
        string profileImageUri)
    {
        return InsertAccount(new Account
        {
            username = username,
            emailAddress = emailAddress,
            passwordHash = DefaultPasswordHash,
            profileImageUri = profileImageUri,
            statusCode = "ACTIVE",
            registeredAtUtc = DateTime.UtcNow
        });
    }

    internal static void SeedAccountSettings(
        long accountId,
        string preferredLocaleCode = DefaultLocaleCode)
    {
        using ReversiGameSystemEntities context = CreateFreshContext();
        AccountSettings settings = new AccountSettings
        {
            accountId = accountId,
            preferredLocaleCode = preferredLocaleCode,
            timeZoneId = "UTC",
            pieceStyleCode = DefaultPieceStyleCode
        };
        context.AccountSettings.Add(settings);
        context.SaveChanges();
    }

    private static long InsertAccount(Account account)
    {
        using ReversiGameSystemEntities context = CreateFreshContext();
        context.Account.Add(account);
        context.SaveChanges();
        return account.accountId;
    }
}
