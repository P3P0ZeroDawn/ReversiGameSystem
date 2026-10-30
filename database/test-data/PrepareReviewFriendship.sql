-- Review fixture only. Create both accounts through the client before running this script.
-- Run first with @Apply = 0 to inspect the selected pair, then use 1 to accept their friendship.
-- This does not create accounts, read credentials, or modify any other friendship.
DECLARE @FirstUsername nvarchar(50) = N'p3p0';
DECLARE @SecondUsername nvarchar(50) = N'cesardoc';
DECLARE @Apply bit = 0;

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NULLIF(LTRIM(RTRIM(@FirstUsername)), N'') IS NULL
    OR NULLIF(LTRIM(RTRIM(@SecondUsername)), N'') IS NULL
    THROW 50000, 'Both review usernames are required.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @firstId bigint, @secondId bigint;
    SELECT @firstId = accountId
    FROM dbo.Account WITH (UPDLOCK, HOLDLOCK)
    WHERE username = @FirstUsername AND statusCode = 'ACTIVE';

    SELECT @secondId = accountId
    FROM dbo.Account WITH (UPDLOCK, HOLDLOCK)
    WHERE username = @SecondUsername AND statusCode = 'ACTIVE';

    IF @firstId IS NULL OR @secondId IS NULL
        THROW 50001, 'Both review accounts must exist and be active.', 1;
    IF @firstId = @secondId
        THROW 50002, 'The review accounts must be different.', 1;

    DECLARE @accountA bigint = CASE WHEN @firstId < @secondId THEN @firstId ELSE @secondId END;
    DECLARE @accountB bigint = CASE WHEN @firstId < @secondId THEN @secondId ELSE @firstId END;
    DECLARE @now datetime2(3) = SYSUTCDATETIME();

    IF @Apply = 1
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.Friendship WITH (UPDLOCK, HOLDLOCK)
            WHERE accountIdA = @accountA AND accountIdB = @accountB)
        BEGIN
            UPDATE dbo.Friendship
            SET statusCode = 'ACCEPTED', respondedAtUtc = @now, endedAtUtc = NULL
            WHERE accountIdA = @accountA AND accountIdB = @accountB
                AND (statusCode <> 'ACCEPTED' OR endedAtUtc IS NOT NULL
                    OR respondedAtUtc IS NULL);
        END
        ELSE
        BEGIN
            INSERT dbo.Friendship
                (accountIdA, accountIdB, requestedByAccountId, statusCode,
                 requestedAtUtc, respondedAtUtc, endedAtUtc)
            VALUES (@accountA, @accountB, @firstId, 'ACCEPTED', @now, @now, NULL);
        END
    END;

    SELECT @FirstUsername AS firstUsername, @SecondUsername AS secondUsername,
        @accountA AS accountIdA, @accountB AS accountIdB,
        @Apply AS applied, f.statusCode AS friendshipStatus
    FROM (SELECT 1 AS selectedPair) selected
    LEFT JOIN dbo.Friendship f ON f.accountIdA = @accountA AND f.accountIdB = @accountB;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
