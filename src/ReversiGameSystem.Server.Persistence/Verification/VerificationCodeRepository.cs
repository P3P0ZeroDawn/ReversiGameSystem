using System;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Verification;

/// <summary>Persists protected challenges through the existing database connection.</summary>
public sealed class VerificationCodeRepository : IVerificationCodeRepository
{
    private const string ReplaceCommand = @"
UPDATE dbo.VerificationCode WITH (UPDLOCK, HOLDLOCK)
SET codeHash = @digest, expiresAtUtc = @expires, failedAttempts = 0,
    usedAtUtc = NULL, invalidatedAtUtc = NULL, createdAtUtc = @created,
    emailAddress = @email, purposeCode = @purpose
WHERE identityKey = @identity;
IF @@ROWCOUNT = 0
    INSERT dbo.VerificationCode
        (identityKey, codeHash, expiresAtUtc, failedAttempts, createdAtUtc,
         emailAddress, purposeCode, usedAtUtc, invalidatedAtUtc)
    VALUES (@identity, @digest, @expires, 0, @created, @email, @purpose, NULL, NULL);";

    private const string ConsumeCommand = @"
DECLARE @result int = 1;
DECLARE @stored varchar(255), @expires datetime2, @attempts int;
DECLARE @used datetime2, @invalidated datetime2;
SELECT @stored = codeHash, @expires = expiresAtUtc,
       @attempts = failedAttempts, @used = usedAtUtc, @invalidated = invalidatedAtUtc
FROM dbo.VerificationCode WITH (UPDLOCK, HOLDLOCK)
WHERE identityKey = @identity;
IF @stored IS NOT NULL AND @used IS NULL AND @invalidated IS NULL AND @attempts < 5
BEGIN
    IF @expires <= @now
        SET @result = 2;
    ELSE IF @stored COLLATE Latin1_General_100_BIN2 = @digest COLLATE Latin1_General_100_BIN2
    BEGIN
        UPDATE dbo.VerificationCode SET usedAtUtc = @now WHERE identityKey = @identity;
        SET @result = 0;
    END
    ELSE
        UPDATE dbo.VerificationCode
        SET failedAttempts = failedAttempts + 1,
            invalidatedAtUtc = CASE WHEN failedAttempts = 4 THEN @now ELSE NULL END
        WHERE identityKey = @identity;
END;
SELECT @result;";

    /// <inheritdoc />
    public void Replace(PendingVerificationCode code)
    {
        if (code == null)
        {
            throw new ArgumentNullException(nameof(code));
        }
        ValidateDigest(code.Digest);
        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        using (DbContextTransaction transaction = context.Database.BeginTransaction(
            IsolationLevel.Serializable))
        {
            context.Database.ExecuteSqlCommand(ReplaceCommand,
                IdentityParameter(code.IdentityKey), DigestParameter(code.Digest),
                new SqlParameter("@expires", SqlDbType.DateTime2) { Value = code.ExpiresAtUtc },
                new SqlParameter("@created", SqlDbType.DateTime2) { Value = code.CreatedAtUtc },
                new SqlParameter("@email", SqlDbType.NVarChar, 254)
                {
                    Value = code.RecipientAddress
                },
                new SqlParameter("@purpose", SqlDbType.VarChar, 30)
                {
                    Value = GetPurposeCode(code.Purpose)
                });
            transaction.Commit();
        }
    }

    /// <inheritdoc />
    public VerificationStatus Consume(VerificationAttempt attempt)
    {
        if (attempt == null)
        {
            throw new ArgumentNullException(nameof(attempt));
        }
        ValidateDigest(attempt.Digest);
        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        using (DbContextTransaction transaction = context.Database.BeginTransaction(
            IsolationLevel.Serializable))
        {
            int result = context.Database.SqlQuery<int>(ConsumeCommand,
                IdentityParameter(attempt.IdentityKey), DigestParameter(attempt.Digest),
                new SqlParameter("@now", SqlDbType.DateTime2) { Value = attempt.NowUtc }).Single();
            transaction.Commit();
            return (VerificationStatus)result;
        }
    }

    private static string GetPurposeCode(VerificationPurpose purpose) => purpose switch
    {
        VerificationPurpose.Login => "LOGIN",
        VerificationPurpose.Registration => "ACCOUNT_CREATION",
        _ => throw new ArgumentOutOfRangeException(nameof(purpose))
    };

    private static SqlParameter IdentityParameter(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 450)
        {
            throw new ArgumentException("A valid verification identity is required.", nameof(key));
        }
        return new SqlParameter("@identity", SqlDbType.NVarChar, 450) { Value = key };
    }

    private static SqlParameter DigestParameter(byte[] digest)
    {
        return new SqlParameter("@digest", SqlDbType.VarChar, 44)
        {
            Value = Convert.ToBase64String(digest)
        };
    }

    private static void ValidateDigest(byte[] digest)
    {
        if (digest == null || digest.Length != 32)
        {
            throw new ArgumentException(
                "A protected verification digest is required.", nameof(digest));
        }
    }
}
