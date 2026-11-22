SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.AccountSettings', N'useCustomBoardBackground') IS NULL
BEGIN
    ALTER TABLE dbo.AccountSettings
        ADD useCustomBoardBackground bit NOT NULL
            CONSTRAINT DF_AccountSettings_UseCustomBoardBackground DEFAULT (0) WITH VALUES;
    EXEC(N'UPDATE dbo.AccountSettings
           SET useCustomBoardBackground = 1
           WHERE boardBackgroundUri IS NOT NULL AND LEN(boardBackgroundUri) > 0');
END;

COMMIT TRANSACTION;
