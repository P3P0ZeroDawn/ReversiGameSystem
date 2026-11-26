using System;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests.TestDoubles;

internal sealed class VerificationCodeRepositoryStub : IVerificationCodeRepository
{
    internal VerificationStatus VerificationResult { get; set; } = VerificationStatus.Accepted;

    internal Exception? SaveFailure { get; set; }

    internal Exception? VerificationFailure { get; set; }

    internal PendingVerificationCode? LastSavedCode { get; private set; }

    internal string? LastVerifiedIdentityKey { get; private set; }

    internal int SaveCallCount { get; private set; }

    internal int VerificationCallCount { get; private set; }

    public void SavePendingCode(PendingVerificationCode pendingCode)
    {
        SaveCallCount++;
        LastSavedCode = pendingCode;
        if (SaveFailure != null)
        {
            throw SaveFailure;
        }
    }

    public VerificationStatus ProcessVerificationAttempt(
        string identityKey,
        Func<StoredVerificationCode, VerificationEvaluationResult> evaluateAttempt)
    {
        VerificationCallCount++;
        LastVerifiedIdentityKey = identityKey;
        if (VerificationFailure != null)
        {
            throw VerificationFailure;
        }

        return VerificationResult;
    }
}
