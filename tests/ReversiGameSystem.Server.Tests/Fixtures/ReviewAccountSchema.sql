CREATE TABLE dbo.Account (
 accountId bigint IDENTITY NOT NULL PRIMARY KEY,
 username nvarchar(50) NOT NULL CONSTRAINT UQ_Account_Username UNIQUE,
 emailAddress nvarchar(254) NOT NULL CONSTRAINT UQ_Account_EmailAddress UNIQUE,
 passwordHash varchar(255) NOT NULL, profileImageUri nvarchar(2048) NULL,
 statusCode varchar(20) NOT NULL, registeredAtUtc datetime2(3) NOT NULL);
CREATE TABLE dbo.SupportedLocale (localeCode varchar(35) NOT NULL PRIMARY KEY, isEnabled bit NOT NULL);
INSERT dbo.SupportedLocale VALUES ('es-MX',1),('en-US',1);
CREATE TABLE dbo.AccountSettings (
 accountId bigint NOT NULL PRIMARY KEY REFERENCES dbo.Account(accountId),
 preferredLocaleCode varchar(35) NOT NULL REFERENCES dbo.SupportedLocale(localeCode),
 timeZoneId varchar(100) NULL, boardBackgroundUri nvarchar(2048) NULL,
 pieceStyleCode varchar(20) NOT NULL);
CREATE TABLE dbo.Friendship (
 accountIdA bigint NOT NULL REFERENCES dbo.Account(accountId),
 accountIdB bigint NOT NULL REFERENCES dbo.Account(accountId),
 requestedByAccountId bigint NOT NULL REFERENCES dbo.Account(accountId),
 statusCode varchar(20) NOT NULL, requestedAtUtc datetime2(3) NOT NULL,
 respondedAtUtc datetime2(3) NULL, endedAtUtc datetime2(3) NULL,
 PRIMARY KEY(accountIdA,accountIdB),
 CONSTRAINT CK_Friendship_Order CHECK(accountIdA < accountIdB),
 CONSTRAINT CK_Friendship_Requester CHECK(requestedByAccountId IN (accountIdA,accountIdB)),
 CONSTRAINT CK_Friendship_Status CHECK(statusCode IN ('PENDING','ACCEPTED','REJECTED','ENDED')));
CREATE TABLE dbo.VerificationCode (
 verificationCodeId bigint IDENTITY NOT NULL PRIMARY KEY,
 emailAddress nvarchar(254) NOT NULL, purposeCode varchar(30) NOT NULL,
 codeHash varchar(255) NOT NULL, createdAtUtc datetime2(3) NOT NULL,
 expiresAtUtc datetime2(3) NOT NULL, usedAtUtc datetime2(3) NULL, invalidatedAtUtc datetime2(3) NULL,
 CONSTRAINT CK_VerificationCode_Purpose CHECK(purposeCode IN ('ACCOUNT_CREATION','PASSWORD_RESET')),
 CONSTRAINT CK_VerificationCode_Expiration CHECK(expiresAtUtc > createdAtUtc));
