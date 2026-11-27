CREATE TABLE dbo.Account (
    accountId bigint IDENTITY NOT NULL PRIMARY KEY,
    username nvarchar(50) NOT NULL CONSTRAINT UQ_Account_Username UNIQUE,
    emailAddress nvarchar(254) NOT NULL CONSTRAINT UQ_Account_EmailAddress UNIQUE,
    passwordHash varchar(255) NOT NULL,
    profileImageUri nvarchar(2048) NULL,
    statusCode varchar(20) NOT NULL,
    registeredAtUtc datetime2(3) NOT NULL
);

CREATE TABLE dbo.SupportedLocale (
    localeCode varchar(35) NOT NULL PRIMARY KEY,
    isEnabled bit NOT NULL
);

INSERT INTO dbo.SupportedLocale (localeCode, isEnabled)
VALUES ('es-MX', 1), ('en-US', 1);

CREATE TABLE dbo.AccountSettings (
    accountId bigint NOT NULL PRIMARY KEY REFERENCES dbo.Account(accountId),
    preferredLocaleCode varchar(35) NOT NULL REFERENCES dbo.SupportedLocale(localeCode),
    timeZoneId varchar(100) NULL,
    boardBackgroundUri nvarchar(2048) NULL,
    useCustomBoardBackground bit NOT NULL
        CONSTRAINT DF_AccountSettings_UseCustomBoardBackground DEFAULT (0),
    pieceStyleCode varchar(20) NOT NULL
);

CREATE TABLE dbo.Friendship (
    accountIdA bigint NOT NULL REFERENCES dbo.Account(accountId),
    accountIdB bigint NOT NULL REFERENCES dbo.Account(accountId),
    requestedByAccountId bigint NOT NULL REFERENCES dbo.Account(accountId),
    statusCode varchar(20) NOT NULL,
    requestedAtUtc datetime2(3) NOT NULL,
    respondedAtUtc datetime2(3) NULL,
    endedAtUtc datetime2(3) NULL,
    PRIMARY KEY (accountIdA, accountIdB),
    CONSTRAINT CK_Friendship_Order CHECK (accountIdA < accountIdB),
    CONSTRAINT CK_Friendship_Requester CHECK (requestedByAccountId IN (accountIdA, accountIdB)),
    CONSTRAINT CK_Friendship_Status CHECK (
        statusCode IN ('PENDING', 'ACCEPTED', 'REJECTED', 'REMOVED')
    )
);

CREATE TABLE dbo.VerificationCode (
    verificationCodeId bigint IDENTITY NOT NULL PRIMARY KEY,
    emailAddress nvarchar(254) NOT NULL,
    purposeCode varchar(30) NOT NULL,
    codeHash varchar(255) NOT NULL,
    createdAtUtc datetime2(3) NOT NULL,
    expiresAtUtc datetime2(3) NOT NULL,
    usedAtUtc datetime2(3) NULL,
    invalidatedAtUtc datetime2(3) NULL,
    identityKey nvarchar(450) COLLATE Latin1_General_100_BIN2 NULL,
    failedAttempts int NOT NULL
        CONSTRAINT DF_VerificationCode_FailedAttempts DEFAULT (0),
    CONSTRAINT CK_VerificationCode_Purpose CHECK (
        purposeCode IN ('ACCOUNT_CREATION', 'PASSWORD_RESET', 'LOGIN')
    ),
    CONSTRAINT CK_VerificationCode_Expiration CHECK (expiresAtUtc > createdAtUtc),
    CONSTRAINT CK_VerificationCode_FailedAttempts CHECK (failedAttempts BETWEEN 0 AND 5)
);

CREATE UNIQUE INDEX UX_VerificationCode_IdentityKey
    ON dbo.VerificationCode (identityKey)
    WHERE identityKey IS NOT NULL;

CREATE TABLE dbo.GameMatch (
    matchId bigint IDENTITY NOT NULL PRIMARY KEY,
    matchCode varchar(12) NOT NULL UNIQUE,
    statusCode varchar(20) NOT NULL
        CHECK (statusCode IN ('WAITING', 'IN_PROGRESS', 'FINISHED', 'CANCELLED')),
    turnDurationSeconds smallint NOT NULL CHECK (turnDurationSeconds BETWEEN 10 AND 120),
    countsTowardStatistics bit NOT NULL,
    isPrivate bit NOT NULL CONSTRAINT DF_GameMatch_IsPrivate DEFAULT (0),
    invitedAccountId bigint NULL
        CONSTRAINT FK_GameMatch_InvitedAccount REFERENCES dbo.Account(accountId),
    invitationConfirmedAtUtc datetime2(3) NULL,
    createdAtUtc datetime2(3) NOT NULL,
    startedAtUtc datetime2(3) NULL,
    endedAtUtc datetime2(3) NULL,
    endReasonCode varchar(30) NULL CONSTRAINT CK_GameMatch_EndReason CHECK (
        endReasonCode IS NULL OR endReasonCode IN
            ('BOARD_FULL', 'NO_LEGAL_MOVES', 'ABANDONMENT', 'INACTIVITY', 'CREATOR_CANCELLED')
    ),
    currentTurnSeatNumber tinyint NULL,
    turnStartedAtUtc datetime2(3) NULL,
    CONSTRAINT CK_GameMatch_Privacy CHECK (
        (isPrivate = 0 AND invitedAccountId IS NULL AND invitationConfirmedAtUtc IS NULL)
        OR (isPrivate = 1 AND invitedAccountId IS NOT NULL)
    )
);

CREATE TABLE dbo.MatchParticipant (
    matchId bigint NOT NULL REFERENCES dbo.GameMatch(matchId),
    seatNumber tinyint NOT NULL CHECK (seatNumber IN (1, 2)),
    accountId bigint NULL REFERENCES dbo.Account(accountId),
    participantTypeCode varchar(20) NOT NULL,
    displayNameSnapshot nvarchar(50) NULL,
    pieceColorCode varchar(10) NOT NULL CHECK (pieceColorCode IN ('BLACK', 'WHITE')),
    resultCode varchar(10) NULL
        CHECK (resultCode IS NULL OR resultCode IN ('WIN', 'LOSS', 'DRAW')),
    finalPieceCount tinyint NULL,
    consecutiveTimeoutCount tinyint NOT NULL,
    joinedAtUtc datetime2(3) NOT NULL,
    isCreator bit NOT NULL,
    PRIMARY KEY (matchId, seatNumber)
);

ALTER TABLE dbo.GameMatch
    ADD CONSTRAINT FK_GameMatch_CurrentTurnParticipant
    FOREIGN KEY (matchId, currentTurnSeatNumber)
    REFERENCES dbo.MatchParticipant(matchId, seatNumber);

CREATE TABLE dbo.GameMove (
    matchId bigint NOT NULL,
    moveNumber int NOT NULL,
    seatNumber tinyint NOT NULL,
    rowIndex tinyint NOT NULL,
    columnIndex tinyint NOT NULL,
    moveSourceCode varchar(30) NOT NULL
        CHECK (moveSourceCode IN ('MANUAL', 'SYSTEM', 'TIMEOUT_AUTOMATIC')),
    playedAtUtc datetime2(3) NOT NULL,
    PRIMARY KEY (matchId, moveNumber),
    FOREIGN KEY (matchId, seatNumber) REFERENCES dbo.MatchParticipant(matchId, seatNumber)
);

CREATE TABLE dbo.ChatMessage (
    matchId bigint NOT NULL,
    messageNumber int NOT NULL CHECK (messageNumber > 0),
    senderSeatNumber tinyint NOT NULL,
    content nvarchar(1000) NOT NULL,
    sentAtUtc datetime2(3) NOT NULL,
    PRIMARY KEY (matchId, messageNumber),
    FOREIGN KEY (matchId, senderSeatNumber) REFERENCES dbo.MatchParticipant(matchId, seatNumber)
);

CREATE TABLE dbo.ModerationRule (
    ruleCode varchar(50) NOT NULL PRIMARY KEY,
    violationThreshold tinyint NULL,
    muteDurationSeconds int NULL,
    isEnabled bit NOT NULL
);

CREATE TABLE dbo.ModerationPattern (
    moderationPatternId bigint IDENTITY NOT NULL PRIMARY KEY,
    ruleCode varchar(50) NOT NULL REFERENCES dbo.ModerationRule(ruleCode),
    localeCode varchar(35) NOT NULL REFERENCES dbo.SupportedLocale(localeCode),
    patternValue nvarchar(200) NOT NULL,
    patternTypeCode varchar(20) NOT NULL,
    isEnabled bit NOT NULL
);

CREATE TABLE dbo.ChatViolation (
    violationId bigint IDENTITY NOT NULL PRIMARY KEY,
    detectionEventId uniqueidentifier NOT NULL UNIQUE,
    matchId bigint NOT NULL,
    offenderSeatNumber tinyint NOT NULL,
    ruleCode varchar(50) NOT NULL REFERENCES dbo.ModerationRule(ruleCode),
    detectedLocaleCode varchar(35) NULL REFERENCES dbo.SupportedLocale(localeCode),
    detectedAtUtc datetime2(3) NOT NULL,
    FOREIGN KEY (matchId, offenderSeatNumber) REFERENCES dbo.MatchParticipant(matchId, seatNumber)
);

CREATE TABLE dbo.ChatMute (
    muteId bigint IDENTITY NOT NULL PRIMARY KEY,
    triggerViolationId bigint NOT NULL REFERENCES dbo.ChatViolation(violationId),
    reasonCode varchar(50) NOT NULL,
    startsAtUtc datetime2(3) NOT NULL,
    endsAtUtc datetime2(3) NOT NULL,
    CHECK (endsAtUtc > startsAtUtc)
);

INSERT INTO dbo.ModerationRule (ruleCode, violationThreshold, muteDurationSeconds, isEnabled)
VALUES ('REVIEW_BLOCKED_WORD', 3, 600, 1);

INSERT INTO dbo.ModerationPattern (ruleCode, localeCode, patternValue, patternTypeCode, isEnabled)
VALUES ('REVIEW_BLOCKED_WORD', 'en-US', N'blockedword', 'WORD', 1);
