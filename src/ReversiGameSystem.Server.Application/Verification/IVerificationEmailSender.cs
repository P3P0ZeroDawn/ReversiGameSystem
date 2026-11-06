namespace ReversiGameSystem.Server.Application.Verification;

/// <summary>Delivers verification mail or throws when delivery is not accepted.</summary>
public interface IVerificationEmailSender
{
    /// <summary>Submits a verification message to the delivery mechanism.</summary>
    /// <param name="email">The destination, code and authorized action.</param>
    public void Send(VerificationEmail email);
}
