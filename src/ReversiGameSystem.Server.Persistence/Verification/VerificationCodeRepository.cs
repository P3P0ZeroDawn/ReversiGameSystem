using System;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Verification;

public sealed class VerificationCodeRepository : IVerificationCodeRepository
{
    private const string SavePendingCodeCommand = @"
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

    private const string SelectLockedCodeCommand = @"
SELECT codeHash AS CodeHashText,
       expiresAtUtc AS ExpiresAtUtc,
       failedAttempts AS FailedAttempts,
       usedAtUtc AS UsedAtUtc,
       invalidatedAtUtc AS InvalidatedAtUtc
FROM dbo.VerificationCode WITH (UPDLOCK, HOLDLOCK)
WHERE identityKey = @identity;";

    private const string MarkCodeUsedCommand = @"
UPDATE dbo.VerificationCode
SET usedAtUtc = @usedAt
WHERE identityKey = @identity;";

    private const string UpdateFailedAttemptsCommand = @"
UPDATE dbo.VerificationCode
SET failedAttempts = @attempts,
    invalidatedAtUtc = @invalidatedAt
WHERE identityKey = @identity;";

    public void SavePendingCode(PendingVerificationCode pendingCode)
    {
        if (pendingCode == null)
        {
            throw new ArgumentNullException(nameof(pendingCode));
        }

        EnsureValidDigest(pendingCode.Digest);
        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        using (DbContextTransaction transaction = context.Database.BeginTransaction(
            IsolationLevel.Serializable))
        {
            context.Database.ExecuteSqlCommand(
                SavePendingCodeCommand,
                IdentityParameter(pendingCode.IdentityKey),
                DigestParameter(pendingCode.Digest),
                new SqlParameter("@expires", SqlDbType.DateTime2) { Value = pendingCode.ExpiresAtUtc },
                new SqlParameter("@created", SqlDbType.DateTime2) { Value = pendingCode.CreatedAtUtc },
                new SqlParameter("@email", SqlDbType.NVarChar, 254)
                {
                    Value = pendingCode.RecipientAddress
                },
                new SqlParameter("@purpose", SqlDbType.VarChar, 30)
                {
                    Value = GetPurposeCode(pendingCode.Purpose)
                });
            transaction.Commit();
        }
    }

    public VerificationStatus ProcessVerificationAttempt(
        string identityKey,
        Func<StoredVerificationCode, VerificationEvaluationResult> evaluateAttempt)
    {
        if (evaluateAttempt == null)
        {
            throw new ArgumentNullException(nameof(evaluateAttempt));
        }

        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        using (DbContextTransaction transaction = context.Database.BeginTransaction(
            IsolationLevel.Serializable))
        {
            VerificationCodeRow? row = context.Database
                .SqlQuery<VerificationCodeRow>(SelectLockedCodeCommand, IdentityParameter(identityKey))
                .SingleOrDefault();
            StoredVerificationCode storedCode = MapStoredCode(row);
            VerificationEvaluationResult evaluationResult = evaluateAttempt(storedCode);
            if (evaluationResult == null)
            {
                throw new InvalidOperationException(
                    "Verification evaluation must return a result.");
            }

            ApplyEvaluationUpdate(context, identityKey, evaluationResult);
            transaction.Commit();
            VerificationStatus status = evaluationResult.Status;
            return status;
        }
    }

    private static void ApplyEvaluationUpdate(
        ReversiGameSystemEntities context,
        string identityKey,
        VerificationEvaluationResult evaluationResult)
    {
        if (evaluationResult.MarkUsedAtUtc.HasValue)
        {
            context.Database.ExecuteSqlCommand(
                MarkCodeUsedCommand,
                IdentityParameter(identityKey),
                new SqlParameter("@usedAt", SqlDbType.DateTime2)
                {
                    Value = evaluationResult.MarkUsedAtUtc.Value
                });
            return;
        }

        if (evaluationResult.UpdatedFailedAttempts.HasValue)
        {
            object invalidatedValue = evaluationResult.InvalidatedAtUtc.HasValue
                ? (object)evaluationResult.InvalidatedAtUtc.Value
                : DBNull.Value;
            context.Database.ExecuteSqlCommand(
                UpdateFailedAttemptsCommand,
                IdentityParameter(identityKey),
                new SqlParameter("@attempts", SqlDbType.Int)
                {
                    Value = evaluationResult.UpdatedFailedAttempts.Value
                },
                new SqlParameter("@invalidatedAt", SqlDbType.DateTime2)
                {
                    Value = invalidatedValue
                });
        }
    }

    private static StoredVerificationCode MapStoredCode(VerificationCodeRow? row)
    {
        if (row == null)
        {
            StoredVerificationCode missingCode = new StoredVerificationCode();
            return missingCode;
        }

        byte[] digest = string.IsNullOrWhiteSpace(row.CodeHashText)
            ? Array.Empty<byte>()
            : Convert.FromBase64String(row.CodeHashText);

        StoredVerificationCode storedCode = new StoredVerificationCode(
            digest,
            row.ExpiresAtUtc,
            row.FailedAttempts,
            row.UsedAtUtc,
            row.InvalidatedAtUtc);
        return storedCode;
    }

    private static string GetPurposeCode(VerificationPurpose purpose) => purpose switch
    {
        VerificationPurpose.Login => "LOGIN",
        VerificationPurpose.Registration => "ACCOUNT_CREATION",
        VerificationPurpose.PasswordReset => "PASSWORD_RESET",
        _ => throw new ArgumentOutOfRangeException(nameof(purpose))
    };

    private static SqlParameter IdentityParameter(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 450)
        {
            throw new ArgumentException("A valid verification identity is required.", nameof(key));
        }

        SqlParameter parameter = new SqlParameter("@identity", SqlDbType.NVarChar, 450)
        {
            Value = key
        };
        return parameter;
    }

    private static SqlParameter DigestParameter(byte[] digest)
    {
        SqlParameter parameter = new SqlParameter("@digest", SqlDbType.VarChar, 44)
        {
            Value = Convert.ToBase64String(digest)
        };
        return parameter;
    }

    private static void EnsureValidDigest(byte[] digest)
    {
        if (digest == null || digest.Length != 32)
        {
            throw new ArgumentException(
                "A protected verification digest is required.", nameof(digest));
        }
    }

    private sealed class VerificationCodeRow
    {
        public string CodeHashText { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public int FailedAttempts { get; set; }

        public DateTime? UsedAtUtc { get; set; }

        public DateTime? InvalidatedAtUtc { get; set; }
    }
}
