namespace ReversiGameSystem.Services;

internal sealed class AuthenticationOutcome
{
    internal AuthenticationOutcome(
        AuthenticationStatus status,
        AuthenticatedAccountSession? session = null)
    {
        Status = status;
        Session = session;
    }

    internal AuthenticationStatus Status { get; }

    internal AuthenticatedAccountSession? Session { get; }
}
