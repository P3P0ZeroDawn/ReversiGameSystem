SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.MatchParticipant')
        AND name = N'UQ_MatchParticipant_AccountPerMatch'
)
BEGIN
    DROP INDEX UQ_MatchParticipant_AccountPerMatch ON dbo.MatchParticipant;
END;

CREATE UNIQUE INDEX UQ_MatchParticipant_AccountPerMatch
    ON dbo.MatchParticipant (matchId, accountId)
    WHERE accountId IS NOT NULL;

COMMIT TRANSACTION;
