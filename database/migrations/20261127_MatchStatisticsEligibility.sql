SET XACT_ABORT ON;
BEGIN TRANSACTION;

UPDATE game
SET countsTowardStatistics = CASE
    WHEN game.statusCode = 'FINISHED'
        AND game.startedAtUtc IS NOT NULL
        AND game.endedAtUtc >= game.startedAtUtc
        AND participants.total = 2
        AND participants.registered = 2
        AND participants.complete = 2
        AND ((participants.wins = 1 AND participants.losses = 1)
            OR participants.draws = 2)
    THEN 1
    ELSE 0
END
FROM dbo.GameMatch AS game
CROSS APPLY (
    SELECT COUNT(*) AS total,
        SUM(CASE WHEN participantTypeCode = 'REGISTERED' AND accountId IS NOT NULL
            THEN 1 ELSE 0 END) AS registered,
        SUM(CASE WHEN resultCode IS NOT NULL AND finalPieceCount IS NOT NULL
            THEN 1 ELSE 0 END) AS complete,
        SUM(CASE WHEN resultCode = 'WIN' THEN 1 ELSE 0 END) AS wins,
        SUM(CASE WHEN resultCode = 'LOSS' THEN 1 ELSE 0 END) AS losses,
        SUM(CASE WHEN resultCode = 'DRAW' THEN 1 ELSE 0 END) AS draws
    FROM dbo.MatchParticipant
    WHERE matchId = game.matchId
) AS participants
WHERE game.statusCode IN ('FINISHED', 'CANCELLED');

COMMIT TRANSACTION;
