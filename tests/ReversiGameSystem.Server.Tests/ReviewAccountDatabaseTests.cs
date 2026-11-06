using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Verification;

namespace ReversiGameSystem.Server.Tests;

internal sealed class ReviewAccountDatabaseTests : IVerificationEmailSender
{
    private const string DatabasePrefix = "ReversiReviewAccountTests_";
    private const string ConnectionVariable = "REVERSI_DB_CONNECTION_STRING";
    private const string Password = "Disposable1!";
    private readonly Guid _identifier = Guid.NewGuid();
    private readonly Dictionary<string, VerificationEmail> _mail =
        new Dictionary<string, VerificationEmail>(StringComparer.Ordinal);
    private readonly FixedUtcClock _clock = new FixedUtcClock(DateTime.UtcNow);
    private readonly Pbkdf2PasswordHasher _hasher = new Pbkdf2PasswordHasher();
    private readonly RecordingSessionRepository _sessions;
    private readonly SessionApplicationService _applicationService;
    private string DatabaseName => DatabasePrefix + _identifier.ToString("N");

    private ReviewAccountDatabaseTests()
    {
        _sessions = new RecordingSessionRepository(_clock);
        VerificationCodeIssuer issuer = new VerificationCodeIssuer(this,
            new VerificationCodeProtector(new byte[32]), _clock);
        _applicationService = new SessionApplicationService(
            new AccountLoginRepository(),
            _hasher,
            new VerificationCodeService(issuer, new VerificationCodeRepository(), _clock),
            _sessions);
    }

    public void Send(VerificationEmail email) => _mail[email.RecipientAddress] = email;

    internal static void RunDisposableDatabaseChecks() => new ReviewAccountDatabaseTests().Run();

    private void Run() => WithDatabase(RunChecks);

    internal static void RunWithReviewAccounts(
        Action<SessionApplicationService, ISessionRepository> action)
    {
        ReviewAccountDatabaseTests fixture = new ReviewAccountDatabaseTests();
        fixture.WithDatabase(() =>
        {
            fixture.Register_ValidAccount_PersistsSaltedCredentialsAndSettings("ReviewAlpha");
            fixture.Register_ValidAccount_PersistsSaltedCredentialsAndSettings("ReviewBeta");
            Execute(fixture.DatabaseName, FriendshipScript(true));
            fixture.SeedLoginCode("ReviewAlpha", "000001");
            fixture.SeedLoginCode("ReviewBeta", "000002");
            action(fixture._applicationService, fixture._sessions);
            TestAssert.AreEqual(2, Convert.ToInt32(fixture.Scalar(
                "SELECT COUNT(*) FROM dbo.VerificationCode WHERE usedAtUtc IS NOT NULL")));
        });
    }

    private void SeedLoginCode(string username, string code)
    {
        LoginAccount account = new AccountLoginRepository()
            .FindByLoginAsync(username).GetAwaiter().GetResult().GetFoundAccount();
        VerificationIdentity identity = VerificationIdentity.ForLogin(
            account.AccountId, account.EmailAddress);
        new VerificationCodeRepository().Replace(new PendingVerificationCode
        {
            IdentityKey = identity.Key,
            RecipientAddress = identity.RecipientAddress,
            Purpose = identity.Purpose,
            CreatedAtUtc = _clock.UtcNow,
            ExpiresAtUtc = _clock.UtcNow.AddMinutes(5),
            Digest = new VerificationCodeProtector(new byte[32]).Protect(identity.Key, code)
        });
    }

    private void WithDatabase(Action action)
    {
        string? previous = Environment.GetEnvironmentVariable(ConnectionVariable);
        bool created = false;
        try
        {
            ValidateDisposableName();
            Execute("master", "CREATE DATABASE [" + DatabaseName + "];");
            created = true;
            CreateSchema();
            foreach (string migration in new[] { "20261022_VerificationCodeAttempts.sql",
                "20261023_VerificationCodeLoginPurpose.sql" })
            {
                Execute(DatabaseName, File.ReadAllText(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "migrations", migration)));
            }
            Environment.SetEnvironmentVariable(ConnectionVariable, ConnectionString(DatabaseName));
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionVariable, previous);
            if (created)
            {
                ValidateDisposableName();
                SqlConnection.ClearAllPools();
                Execute("master", "ALTER DATABASE [" + DatabaseName
                    + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["
                    + DatabaseName + "];");
            }
        }
    }

    private void RunChecks()
    {
        Register_ValidAccount_PersistsSaltedCredentialsAndSettings("ReviewAlpha");
        Register_ValidAccount_PersistsSaltedCredentialsAndSettings("ReviewBeta");
        RequestLoginCode_WrongPassword_DoesNotCreateSessionOrCode();
        CompleteLogin_WrongCode_DoesNotCreateSession();
        CompleteLogin_CapturedCodes_CreatesDistinctSessions();
        PrepareFriendship_DryRun_DoesNotWrite();
        PrepareFriendship_Apply_AcceptsOnlySelectedPair();
        PrepareFriendship_Reapply_PreservesAllRows();
        PrepareFriendship_MissingAccount_RejectsWithoutWrites();
        PrepareFriendship_SameAccount_RejectsWithoutWrites();
        Console.WriteLine("Passed 10 real review-account SQL checks "
            + "in a disposable LocalDB database.");
    }

    private void Register_ValidAccount_PersistsSaltedCredentialsAndSettings(string username)
    {
        // Arrange
        CreateAccountHandler handler = new CreateAccountHandler(
            new EntityFrameworkAccountRegistrationStore(), _hasher,
            new ProfileImageStoreStub(), _clock);
        CreateAccountCommand command = new CreateAccountCommand(username,
            username + "@example.invalid", Password, "es-MX", null);

        // Act
        CreateAccountResult result = handler.ExecuteAsync(command).GetAwaiter().GetResult();

        // Assert
        TestAssert.AreEqual(CreateAccountOutcome.Success, result.Outcome);
        TestAssert.IsTrue(result.AccountId > 0, "Registration must return the stored account ID.");
        LoginAccount account = new AccountLoginRepository()
            .FindByLoginAsync(username).GetAwaiter().GetResult().GetFoundAccount();
        TestAssert.AreEqual(result.AccountId, (long?)account.AccountId);
        TestAssert.AreEqual("ACTIVE", account.StatusCode);
        TestAssert.AreEqual("es-MX", account.PreferredLocaleCode);
        TestAssert.IsTrue(account.PasswordHash != Password
            && _hasher.Verify(Password, account.PasswordHash),
            "Stored PBKDF2 must verify the password.");
        TestAssert.AreEqual(1, Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.AccountSettings "
            + "WHERE accountId = " + account.AccountId + " AND pieceStyleCode = 'STANDARD'")));
    }

    private void RequestLoginCode_WrongPassword_DoesNotCreateSessionOrCode()
    {
        // Arrange
        int created = _sessions.CreatedCount;

        // Act
        SessionOperationResult<DateTime> result = _applicationService.RequestLoginCode(
            "ReviewAlpha",
            "WrongPassword1!");

        // Assert
        TestAssert.AreEqual(SessionOperationStatus.InvalidCredentials, result.Status);
        TestAssert.AreEqual(created, _sessions.CreatedCount);
        TestAssert.AreEqual(0, _mail.Count);
        TestAssert.AreEqual(0, Convert.ToInt32(Scalar(
            "SELECT COUNT(*) FROM dbo.VerificationCode")));
    }

    private void CompleteLogin_WrongCode_DoesNotCreateSession()
    {
        // Arrange
        _applicationService.RequestLoginCode("ReviewAlpha", Password);
        VerificationEmail email = _mail["ReviewAlpha@example.invalid"];
        string wrongCode = email.Code == "000000" ? "000001" : "000000";
        int created = _sessions.CreatedCount;

        // Act
        SessionOperationResult<AuthenticatedSession> result =
            _applicationService.CompleteLogin("ReviewAlpha", wrongCode);

        // Assert
        TestAssert.AreEqual(SessionOperationStatus.InvalidVerificationCode, result.Status);
        TestAssert.AreEqual(created, _sessions.CreatedCount);
        TestAssert.AreEqual(1, Convert.ToInt32(Scalar(
            "SELECT failedAttempts FROM dbo.VerificationCode")));
    }

    private void CompleteLogin_CapturedCodes_CreatesDistinctSessions()
    {
        // Arrange
        _applicationService.RequestLoginCode("ReviewBeta@example.invalid", Password);

        // Act
        AuthenticatedSession first = _applicationService.CompleteLogin(
            "ReviewAlpha",
            _mail["ReviewAlpha@example.invalid"].Code).GetValue();
        AuthenticatedSession second = _applicationService.CompleteLogin(
            "ReviewBeta@example.invalid",
            _mail["ReviewBeta@example.invalid"].Code).GetValue();

        // Assert
        TestAssert.AreEqual(2, _sessions.CreatedCount);
        TestAssert.IsTrue(first.AccountId != second.AccountId
            && first.SessionToken != second.SessionToken,
            "Both accounts require distinct sessions.");
        TestAssert.AreEqual("ReviewAlpha", first.Username);
        TestAssert.AreEqual("ReviewBeta", second.Username);
        TestAssert.IsTrue(_sessions.TryGet(first.SessionToken, out _)
            && _sessions.TryGet(second.SessionToken, out _),
            "Both real in-memory sessions must exist.");
        TestAssert.AreEqual(2, Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.VerificationCode "
            + "WHERE usedAtUtc IS NOT NULL")));
        TestAssert.AreEqual(2, Convert.ToInt32(Scalar(
            "SELECT COUNT(DISTINCT passwordHash) FROM dbo.Account")));
    }

    private void PrepareFriendship_DryRun_DoesNotWrite()
    {
        // Arrange
        string script = FriendshipScript(false);

        // Act
        Execute(DatabaseName, script);

        // Assert
        TestAssert.AreEqual(0, Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.Friendship")));
    }

    private void PrepareFriendship_Apply_AcceptsOnlySelectedPair()
    {
        // Arrange
        Execute(DatabaseName, @"INSERT dbo.Account
    (username,emailAddress,passwordHash,statusCode,registeredAtUtc)
VALUES ('ReviewOther','other@example.invalid','sentinel','ACTIVE',SYSUTCDATETIME());
INSERT dbo.Friendship VALUES (1,3,3,'PENDING','2026-01-01',NULL,NULL);");
        string before = UnrelatedRowsSnapshot();

        // Act
        Execute(DatabaseName, FriendshipScript(true));

        // Assert
        TestAssert.AreEqual(1, Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.Friendship "
            + "WHERE accountIdA = 1 AND accountIdB = 2 AND requestedByAccountId = 1 "
            + "AND statusCode = 'ACCEPTED' AND respondedAtUtc IS NOT NULL "
            + "AND endedAtUtc IS NULL")));
        TestAssert.IsTrue(before == UnrelatedRowsSnapshot(),
            "Unrelated rows must remain unchanged.");
        TestAssert.AreEqual(2, Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.Friendship")));
    }

    private void PrepareFriendship_Reapply_PreservesAllRows()
    {
        // Arrange
        string before = DatabaseRowsSnapshot();

        // Act
        Execute(DatabaseName, FriendshipScript(true));

        // Assert
        TestAssert.IsTrue(before == DatabaseRowsSnapshot(), "Database rows must remain unchanged.");
    }

    private void PrepareFriendship_MissingAccount_RejectsWithoutWrites()
    {
        // Arrange
        string before = DatabaseRowsSnapshot();
        string script = FriendshipScript(true).Replace("N'ReviewBeta'", "N'MissingReview'");

        // Act
        SqlException failure = TestAssert.Throws<SqlException>(() => Execute(DatabaseName, script));

        // Assert
        TestAssert.AreEqual(50001, failure.Number);
        TestAssert.IsTrue(before == DatabaseRowsSnapshot(), "Database rows must remain unchanged.");
    }

    private void PrepareFriendship_SameAccount_RejectsWithoutWrites()
    {
        // Arrange
        string before = DatabaseRowsSnapshot();
        string script = FriendshipScript(true).Replace("N'ReviewBeta'", "N'ReviewAlpha'");

        // Act
        SqlException failure = TestAssert.Throws<SqlException>(() => Execute(DatabaseName, script));

        // Assert
        TestAssert.AreEqual(50002, failure.Number);
        TestAssert.IsTrue(before == DatabaseRowsSnapshot(), "Database rows must remain unchanged.");
    }

    private static string FriendshipScript(bool apply)
    {
        string script = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "test-data", "PrepareReviewFriendship.sql"));
        TestAssert.IsTrue(script.Contains("DECLARE @FirstUsername nvarchar(50) = N'p3p0';")
            && script.Contains("DECLARE @SecondUsername nvarchar(50) = N'cesardoc';")
            && (script.Contains("DECLARE @Apply bit = 0;")
                || script.Contains("DECLARE @Apply bit = 1;")),
            "The fixture parameter interface has changed.");
        script = System.Text.RegularExpressions.Regex.Replace(script,
            @"(?im)^[\t ]*USE[\t ]+(?:\[ReversiGameSystem\]|ReversiGameSystem);[\t ]*\r?$", "");
        TestAssert.IsTrue(!System.Text.RegularExpressions.Regex.IsMatch(script, @"(?im)^\s*USE\s"),
            "Review fixtures must never change the disposable database connection context.");
        return script.Replace("N'p3p0'", "N'ReviewAlpha'")
            .Replace("N'cesardoc'", "N'ReviewBeta'")
            .Replace("DECLARE @Apply bit = 1;", "DECLARE @Apply bit = 0;")
            .Replace("DECLARE @Apply bit = 0;", "DECLARE @Apply bit = " + (apply ? "1;" : "0;"));
    }

    private string UnrelatedRowsSnapshot() => Convert.ToString(Scalar(@"
SELECT (SELECT * FROM dbo.Account ORDER BY accountId FOR XML PATH('account'))
    + (SELECT * FROM dbo.AccountSettings ORDER BY accountId FOR XML PATH('settings'))
    + (SELECT * FROM dbo.VerificationCode ORDER BY verificationCodeId FOR XML PATH('code'))
    + (SELECT * FROM dbo.SupportedLocale ORDER BY localeCode FOR XML PATH('locale'))
    + (SELECT * FROM dbo.Friendship WHERE accountIdB = 3 FOR XML PATH('friendship'))"))!;

    private string DatabaseRowsSnapshot() => UnrelatedRowsSnapshot() + Convert.ToString(Scalar(
        "SELECT * FROM dbo.Friendship ORDER BY accountIdA,accountIdB FOR XML PATH('friendship')"));

    private object Scalar(string sql)
    {
        using (SqlConnection connection = new SqlConnection(ConnectionString(DatabaseName)))
        using (SqlCommand command = new SqlCommand(sql, connection))
        {
            connection.Open();
            return command.ExecuteScalar();
        }
    }

    private static void Execute(string database, string sql)
    {
        using (SqlConnection connection = new SqlConnection(ConnectionString(database)))
        using (SqlCommand command = new SqlCommand(sql, connection))
        {
            command.CommandTimeout = 30;
            connection.Open();
            command.ExecuteNonQuery();
        }
    }

    private static string ConnectionString(string database) => new SqlConnectionStringBuilder
    {
        DataSource = @"(localdb)\MSSQLLocalDB",
        InitialCatalog = database,
        IntegratedSecurity = true,
        ConnectTimeout = 15,
        Pooling = false
    }.ConnectionString;

    private void ValidateDisposableName()
    {
        if (!DatabaseName.StartsWith(DatabasePrefix, StringComparison.Ordinal)
            || !Guid.TryParseExact(DatabaseName.Substring(DatabasePrefix.Length),
                "N", out Guid parsed)
            || parsed != _identifier || parsed == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The target is not the generated disposable test database.");
        }
    }

    private void CreateSchema() => Execute(DatabaseName, File.ReadAllText(Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "ReviewAccountSchema.sql")));
}
