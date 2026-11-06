namespace ReversiGameSystem.Server.Application.Verification;

/// <summary>Persists replacements and atomically consumes one valid code.</summary>
public interface IVerificationCodeRepository
{
    /// <summary>Atomically replaces the pending code after successful delivery.</summary>
    /// <param name="code">The protected code and identity binding.</param>
    public void Replace(PendingVerificationCode code);

    /// <summary>Atomically consumes a match or increments failed attempts up to five.</summary>
    /// <param name="attempt">The protected candidate and server time.</param>
    /// <returns>The durable verification status.</returns>
    public VerificationStatus Consume(VerificationAttempt attempt);
}
