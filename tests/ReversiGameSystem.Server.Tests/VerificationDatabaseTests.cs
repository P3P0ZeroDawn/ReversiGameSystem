using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Persistence.Verification;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Server.Tests;

internal sealed class VerificationDatabaseTests
{
    private const string DatabasePrefix = "ReversiVerificationTests_";
    private const string ConnectionVariable = "REVERSI_DB_CONNECTION_STRING";
    private readonly Guid _identifier = Guid.NewGuid();
    private readonly DateTime _now = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
    private readonly VerificationCodeProtector _protector =
        new VerificationCodeProtector(new byte[32]);
    private readonly VerificationCodeRepository _repository = new VerificationCodeRepository();
    private string DatabaseName => DatabasePrefix + _identifier.ToString("N");

    internal static void RunDisposableDatabaseChecks()
    {
        VerificationDatabaseTests tests = new VerificationDatabaseTests();
        tests.Run();
    }

    // Creation, environment restoration and deletion share one scope to guarantee cleanup.
    private void Run()
    {
        string? previous = Environment.GetEnvironmentVariable(ConnectionVariable);
        bool created = false;
        try
        {
            ValidateDisposableName();
            Execute("master", "CREATE DATABASE [" + DatabaseName + "];");
            created = true;
            CreateLegacySchema();
            string migration = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "migrations", "20261022_VerificationCodeAttempts.sql"));
            Execute(DatabaseName, migration);
            Execute(DatabaseName, migration);
            Environment.SetEnvironmentVariable(ConnectionVariable, ConnectionString(DatabaseName));
            Migration_OriginalPurposeConstraint_RejectsLogin();
            string purposeMigration = File.ReadAllText(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "migrations", "20261023_VerificationCodeLoginPurpose.sql"));
            Execute(DatabaseName, purposeMigration);
            Execute(DatabaseName, purposeMigration);
            Action[] checks =
            {
                Consume_ValidCode_AcceptsOnlyOnce,
                Consume_FiveFailures_ExhaustsCodeAndReplacementResetsAttempts,
                Consume_FourFailures_AllowsCorrectFifthAttempt,
                Consume_ExactExpiry_RejectsExpiredCode,
                Replace_NewCode_InvalidatesPreviousDigest,
                Consume_DifferentAccountAndPurpose_RejectsCodeTransfer,
                Consume_ConcurrentRequests_AcceptsExactlyOne,
                Migration_RepeatedApplication_PreservesLegacyRows,
                Replace_LoginAndRegistration_UseDatabasePurposeCodes,
                Migration_UnknownPurpose_RemainsRejected
            };
            foreach (Action check in checks) { check(); }
            Console.WriteLine($"Passed {checks.Length + 1} real SQL verification checks "
                + "in a disposable LocalDB database.");
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

    private void Consume_ValidCode_AcceptsOnlyOnce()
    {
        VerificationIdentity identity = Identity(1);
        Replace(identity, "000001");
        Assert.AreEqual(VerificationStatus.Accepted, Consume(identity, "000001"));
        Assert.AreEqual(VerificationStatus.Invalid, Consume(identity, "000001"));
    }

    private void Consume_FiveFailures_ExhaustsCodeAndReplacementResetsAttempts()
    {
        VerificationIdentity identity = Identity(2);
        Replace(identity, "000002");
        for (int attempt = 0; attempt < 5; attempt++)
        {
            Assert.AreEqual(VerificationStatus.Invalid, Consume(identity, "999999"));
        }
        Assert.AreEqual(VerificationStatus.Invalid, Consume(identity, "000002"));
        Assert.AreEqual(5, Convert.ToInt32(Scalar(
            "SELECT failedAttempts FROM dbo.VerificationCode WHERE identityKey = @identity",
            identity.Key)));
        Replace(identity, "000003");
        Assert.AreEqual(0, Convert.ToInt32(Scalar(
            "SELECT failedAttempts FROM dbo.VerificationCode WHERE identityKey = @identity",
            identity.Key)));
        Assert.AreEqual(VerificationStatus.Accepted, Consume(identity, "000003"));
    }

    private void Consume_FourFailures_AllowsCorrectFifthAttempt()
    {
        VerificationIdentity identity = Identity(3);
        Replace(identity, "000004");
        for (int attempt = 0; attempt < 4; attempt++)
        {
            Assert.AreEqual(VerificationStatus.Invalid, Consume(identity, "999999"));
        }
        Assert.AreEqual(VerificationStatus.Accepted, Consume(identity, "000004"));
    }

    private void Consume_ExactExpiry_RejectsExpiredCode()
    {
        VerificationIdentity identity = Identity(4);
        Replace(identity, "000005");
        Assert.AreEqual(VerificationStatus.Expired,
            ConsumeAt(identity, "000005", _now.AddMinutes(5)));
    }

    private void Replace_NewCode_InvalidatesPreviousDigest()
    {
        VerificationIdentity identity = Identity(5);
        Replace(identity, "000006");
        Replace(identity, "000007");
        Assert.AreEqual(VerificationStatus.Invalid, Consume(identity, "000006"));
        Assert.AreEqual(VerificationStatus.Accepted, Consume(identity, "000007"));
        Assert.AreEqual(1, Convert.ToInt32(Scalar(
            "SELECT COUNT(*) FROM dbo.VerificationCode WHERE identityKey = @identity",
            identity.Key)));
    }

    private void Consume_DifferentAccountAndPurpose_RejectsCodeTransfer()
    {
        VerificationIdentity login = Identity(6);
        VerificationIdentity other = Identity(7);
        VerificationIdentity registration = VerificationIdentity.ForRegistration(
            "PlayerSix", login.RecipientAddress);
        Replace(login, "000008");
        Replace(other, "000009");
        Replace(registration, "000010");
        Assert.AreEqual(VerificationStatus.Invalid, Consume(other, "000008"));
        Assert.AreEqual(VerificationStatus.Invalid, Consume(registration, "000008"));
        Assert.AreEqual(VerificationStatus.Accepted, Consume(login, "000008"));
        Assert.AreEqual(VerificationStatus.Accepted, Consume(registration, "000010"));
    }

    private void Consume_ConcurrentRequests_AcceptsExactlyOne()
    {
        VerificationIdentity identity = Identity(8);
        Replace(identity, "000011");
        VerificationStatus[] results = new VerificationStatus[16];
        Parallel.For(0, results.Length, index => results[index] = Consume(identity, "000011"));
        Assert.AreEqual(1, results.Count(result => result == VerificationStatus.Accepted));
        Assert.AreEqual(15, results.Count(result => result == VerificationStatus.Invalid));
    }

    private void Migration_RepeatedApplication_PreservesLegacyRows()
    {
        Assert.AreEqual(2, Convert.ToInt32(Scalar(
            "SELECT COUNT(*) FROM dbo.VerificationCode "
            + "WHERE identityKey IS NULL AND failedAttempts = 0 "
            + "AND codeHash = 'legacy-digest' AND emailAddress = 'legacy@example.invalid' "
            + "AND purposeCode IN ('ACCOUNT_CREATION', 'PASSWORD_RESET')", null)));
    }

    private void Migration_OriginalPurposeConstraint_RejectsLogin()
    {
        SqlException failure = Assert.Throws<SqlException>(() => Replace(Identity(9), "000012"));
        Assert.AreEqual(547, failure.Number);
    }

    private void Replace_LoginAndRegistration_UseDatabasePurposeCodes()
    {
        VerificationIdentity login = Identity(10);
        VerificationIdentity registration = VerificationIdentity.ForRegistration(
            "PlayerTen", login.RecipientAddress);
        Replace(login, "000013");
        Replace(registration, "000014");
        const string Query = "SELECT purposeCode FROM dbo.VerificationCode "
            + "WHERE identityKey = @identity";
        Assert.AreEqual("LOGIN", (string)Scalar(Query, login.Key));
        Assert.AreEqual("ACCOUNT_CREATION", (string)Scalar(Query, registration.Key));
    }

    private void Migration_UnknownPurpose_RemainsRejected()
    {
        SqlException failure = Assert.Throws<SqlException>(() => Execute(DatabaseName, @"
INSERT dbo.VerificationCode(emailAddress,purposeCode,codeHash,createdAtUtc,expiresAtUtc)
VALUES ('invalid@example.invalid','UNKNOWN','test-digest',
    SYSUTCDATETIME(),DATEADD(MINUTE,5,SYSUTCDATETIME()));"));
        Assert.AreEqual(547, failure.Number);
    }

    private void Replace(VerificationIdentity identity, string code)
    {
        _repository.SavePendingCode(new PendingVerificationCode
        {
            IdentityKey = identity.Key, Digest = _protector.ComputeCodeDigest(identity.Key, code),
            RecipientAddress = identity.RecipientAddress, Purpose = identity.Purpose,
            CreatedAtUtc = _now, ExpiresAtUtc = _now.AddMinutes(5)
        });
    }

    private VerificationStatus Consume(VerificationIdentity identity, string code) =>
        ConsumeAt(identity, code, _now);

    private VerificationStatus ConsumeAt(VerificationIdentity identity, string code, DateTime now)
    {
        VerificationCodeService service = new VerificationCodeService(
            _protector,
            _repository,
            new NoOpTemplatedEmailSender(),
            new TestUtcClock(now));
        return service.VerifyCode(identity, code);
    }

    private sealed class NoOpTemplatedEmailSender :
        ReversiGameSystem.Server.Application.Common.ITemplatedEmailSender
    {
        public void SendTemplatedEmail(
            string recipientAddress,
            string templateName,
            System.Collections.Generic.IReadOnlyDictionary<string, string> templateValues)
        {
        }
    }

    private static VerificationIdentity Identity(long id) =>
        VerificationIdentity.ForLogin(id, "player@example.invalid");

    private void CreateLegacySchema()
    {
        Execute(DatabaseName, @"
CREATE TABLE dbo.VerificationCode (
    verificationCodeId bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
    emailAddress nvarchar(254) NOT NULL, purposeCode varchar(30) NOT NULL,
    codeHash varchar(255) NOT NULL, createdAtUtc datetime2(3) NOT NULL,
    expiresAtUtc datetime2(3) NOT NULL, usedAtUtc datetime2(3) NULL,
    invalidatedAtUtc datetime2(3) NULL,
    CONSTRAINT CK_VerificationCode_Purpose
        CHECK (purposeCode IN ('ACCOUNT_CREATION', 'PASSWORD_RESET')),
    CONSTRAINT CK_VerificationCode_Expiration CHECK (expiresAtUtc > createdAtUtc));
INSERT dbo.VerificationCode(emailAddress,purposeCode,codeHash,createdAtUtc,expiresAtUtc)
VALUES ('legacy@example.invalid','ACCOUNT_CREATION','legacy-digest',
        SYSUTCDATETIME(),DATEADD(MINUTE,5,SYSUTCDATETIME())),
       ('legacy@example.invalid','PASSWORD_RESET','legacy-digest',
        SYSUTCDATETIME(),DATEADD(MINUTE,5,SYSUTCDATETIME()));");
    }

    private object Scalar(string sql, string? identity)
    {
        using (SqlConnection connection = new SqlConnection(ConnectionString(DatabaseName)))
        using (SqlCommand command = new SqlCommand(sql, connection))
        {
            if (identity != null) { command.Parameters.AddWithValue("@identity", identity); }
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
        DataSource = @"(localdb)\MSSQLLocalDB", InitialCatalog = database,
        IntegratedSecurity = true, ConnectTimeout = 15, Pooling = false
    }.ConnectionString;

    private void ValidateDisposableName()
    {
        string suffix = DatabaseName.Substring(DatabasePrefix.Length);
        if (!DatabaseName.StartsWith(DatabasePrefix, StringComparison.Ordinal)
            || !Guid.TryParseExact(suffix, "N", out Guid parsed) || parsed != _identifier
            || _identifier == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The database is not the generated disposable test target.");
        }
    }
}
