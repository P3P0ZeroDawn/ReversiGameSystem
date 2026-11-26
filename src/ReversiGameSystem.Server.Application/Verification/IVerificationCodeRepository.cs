using System;

namespace ReversiGameSystem.Server.Application.Verification;

public interface IVerificationCodeRepository
{
    void SavePendingCode(PendingVerificationCode pendingCode);

    VerificationStatus ProcessVerificationAttempt(
        string identityKey,
        Func<StoredVerificationCode, VerificationEvaluationResult> evaluateAttempt);
}
