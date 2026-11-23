namespace ReversiGameSystem.Server.Application.Verification;

public interface IVerificationCodeRepository
{
    public void Replace(PendingVerificationCode code);

    public VerificationStatus Consume(VerificationAttempt attempt);
}
