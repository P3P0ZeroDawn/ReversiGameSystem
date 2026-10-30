-- Apply to the configured Reversi database to support private match invitations.
-- Regenerate the EDMX after applying; parameterized EF commands support the interim model.
USE ReversiGameSystem;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.GameMatch', N'isPrivate') IS NULL
    ALTER TABLE dbo.GameMatch ADD
        isPrivate bit NOT NULL CONSTRAINT DF_GameMatch_IsPrivate DEFAULT (0);

IF COL_LENGTH(N'dbo.GameMatch', N'invitedAccountId') IS NULL
    ALTER TABLE dbo.GameMatch ADD
        invitedAccountId bigint NULL CONSTRAINT FK_GameMatch_InvitedAccount REFERENCES dbo.Account(accountId);

IF COL_LENGTH(N'dbo.GameMatch', N'invitationConfirmedAtUtc') IS NULL
    ALTER TABLE dbo.GameMatch ADD
        invitationConfirmedAtUtc datetime2(3) NULL;

-- Dynamic execution lets the script compile against the pre-migration schema.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.GameMatch')
        AND name = N'CK_GameMatch_Privacy')
    EXEC(N'ALTER TABLE dbo.GameMatch WITH CHECK
        ADD CONSTRAINT CK_GameMatch_Privacy
        CHECK ((isPrivate = 0 AND invitedAccountId IS NULL AND invitationConfirmedAtUtc IS NULL)
            OR (isPrivate = 1 AND invitedAccountId IS NOT NULL));');

COMMIT TRANSACTION;