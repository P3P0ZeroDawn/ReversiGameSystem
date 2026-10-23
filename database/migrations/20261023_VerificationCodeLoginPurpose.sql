-- Apply after 20261022_VerificationCodeAttempts.sql to the configured Reversi database.
-- Preserve existing account-creation and password-reset codes while enabling login codes.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.VerificationCode', N'U') IS NULL
    THROW 50000, 'The VerificationCode table must exist before this migration.', 1;

IF EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.VerificationCode')
        AND name = N'CK_VerificationCode_Purpose')
    ALTER TABLE dbo.VerificationCode DROP CONSTRAINT CK_VerificationCode_Purpose;

ALTER TABLE dbo.VerificationCode WITH CHECK
    ADD CONSTRAINT CK_VerificationCode_Purpose
    CHECK (purposeCode IN ('ACCOUNT_CREATION', 'PASSWORD_RESET', 'LOGIN'));

COMMIT TRANSACTION;
