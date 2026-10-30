-- Disposable integration fixture; never apply this file to the project database.
CREATE TABLE dbo.GameMatch (
 matchId bigint IDENTITY NOT NULL PRIMARY KEY,
 matchCode varchar(12) NOT NULL UNIQUE,
 statusCode varchar(20) NOT NULL CHECK (statusCode IN ('WAITING','IN_PROGRESS','FINISHED','CANCELLED')),
 turnDurationSeconds smallint NOT NULL CHECK (turnDurationSeconds BETWEEN 10 AND 120),
 countsTowardStatistics bit NOT NULL,
 createdAtUtc datetime2(3) NOT NULL, startedAtUtc datetime2(3) NULL,
 endedAtUtc datetime2(3) NULL, endReasonCode varchar(30) NULL,
 currentTurnSeatNumber tinyint NULL, turnStartedAtUtc datetime2(3) NULL);
CREATE TABLE dbo.MatchParticipant (
 matchId bigint NOT NULL REFERENCES dbo.GameMatch(matchId),
 seatNumber tinyint NOT NULL CHECK (seatNumber IN (1,2)),
 accountId bigint NULL REFERENCES dbo.Account(accountId),
 participantTypeCode varchar(20) NOT NULL,
 displayNameSnapshot nvarchar(50) NULL,
 pieceColorCode varchar(10) NOT NULL CHECK (pieceColorCode IN ('BLACK','WHITE')),
 resultCode varchar(10) NULL, finalPieceCount tinyint NULL,
 consecutiveTimeoutCount tinyint NOT NULL,
 joinedAtUtc datetime2(3) NOT NULL, isCreator bit NOT NULL,
 PRIMARY KEY(matchId,seatNumber));
ALTER TABLE dbo.GameMatch ADD FOREIGN KEY(matchId,currentTurnSeatNumber)
 REFERENCES dbo.MatchParticipant(matchId,seatNumber);
CREATE TABLE dbo.GameMove (
 matchId bigint NOT NULL, moveNumber int NOT NULL,
 seatNumber tinyint NOT NULL, rowIndex tinyint NOT NULL, columnIndex tinyint NOT NULL,
 moveSourceCode varchar(30) NOT NULL, playedAtUtc datetime2(3) NOT NULL,
 PRIMARY KEY(matchId,moveNumber),
 FOREIGN KEY(matchId,seatNumber) REFERENCES dbo.MatchParticipant(matchId,seatNumber));
CREATE TABLE dbo.ChatMessage (
 matchId bigint NOT NULL, messageNumber int NOT NULL CHECK(messageNumber > 0),
 senderSeatNumber tinyint NOT NULL, content nvarchar(1000) NOT NULL,
 sentAtUtc datetime2(3) NOT NULL, PRIMARY KEY(matchId,messageNumber),
 FOREIGN KEY(matchId,senderSeatNumber) REFERENCES dbo.MatchParticipant(matchId,seatNumber));
CREATE TABLE dbo.ModerationRule (
 ruleCode varchar(50) NOT NULL PRIMARY KEY, violationThreshold tinyint NULL,
 muteDurationSeconds int NULL, isEnabled bit NOT NULL);
CREATE TABLE dbo.ModerationPattern (
 moderationPatternId bigint IDENTITY NOT NULL PRIMARY KEY,
 ruleCode varchar(50) NOT NULL REFERENCES dbo.ModerationRule(ruleCode),
 localeCode varchar(35) NOT NULL REFERENCES dbo.SupportedLocale(localeCode),
 patternValue nvarchar(200) NOT NULL, patternTypeCode varchar(20) NOT NULL,
 isEnabled bit NOT NULL);
CREATE TABLE dbo.ChatViolation (
 violationId bigint IDENTITY NOT NULL PRIMARY KEY,
 detectionEventId uniqueidentifier NOT NULL UNIQUE,
 matchId bigint NOT NULL, offenderSeatNumber tinyint NOT NULL,
 ruleCode varchar(50) NOT NULL REFERENCES dbo.ModerationRule(ruleCode),
 detectedLocaleCode varchar(35) NULL REFERENCES dbo.SupportedLocale(localeCode),
 detectedAtUtc datetime2(3) NOT NULL,
 FOREIGN KEY(matchId,offenderSeatNumber) REFERENCES dbo.MatchParticipant(matchId,seatNumber));
CREATE TABLE dbo.ChatMute (
 muteId bigint IDENTITY NOT NULL PRIMARY KEY,
 triggerViolationId bigint NOT NULL REFERENCES dbo.ChatViolation(violationId),
 reasonCode varchar(50) NOT NULL, startsAtUtc datetime2(3) NOT NULL,
 endsAtUtc datetime2(3) NOT NULL, CHECK(endsAtUtc > startsAtUtc));
INSERT dbo.ModerationRule VALUES ('REVIEW_BLOCKED_WORD',3,600,1);
INSERT dbo.ModerationPattern VALUES ('REVIEW_BLOCKED_WORD','en-US',N'blockedword','WORD',1);
