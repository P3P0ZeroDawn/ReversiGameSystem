-- Apply to the configured Reversi database before enabling the session-code flow.
-- Regenerate the EDMX after applying; parameterized EF commands support the interim model.
-- Historical rows retain a null identityKey and are not used by the new verification flow.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.VerificationCode', N'identityKey') IS NULL
    ALTER TABLE dbo.VerificationCode ADD
        identityKey nvarchar(450) COLLATE Latin1_General_100_BIN2 NULL;

IF COL_LENGTH(N'dbo.VerificationCode', N'failedAttempts') IS NULL
    ALTER TABLE dbo.VerificationCode ADD
        failedAttempts int NOT NULL CONSTRAINT DF_VerificationCode_FailedAttempts DEFAULT (0),
        CONSTRAINT CK_VerificationCode_FailedAttempts CHECK (failedAttempts BETWEEN 0 AND 5);

-- Dynamic execution lets the script compile against the pre-migration schema.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.VerificationCode')
        AND name = N'UX_VerificationCode_IdentityKey')
    EXEC(N'CREATE UNIQUE INDEX UX_VerificationCode_IdentityKey
        ON dbo.VerificationCode(identityKey) WHERE identityKey IS NOT NULL;');

COMMIT TRANSACTION;
