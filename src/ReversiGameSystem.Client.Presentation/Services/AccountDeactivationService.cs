namespace ReversiGameSystem.Services;

/// <summary>
/// Represents the account-server response to a deactivation request.
/// </summary>
internal enum AccountDeactivationResult
{
    Success,
    ActiveMatch,
    ServerConnectionFailed,
    PersistenceError
}

internal interface IAccountDeactivationService
{
    public AccountDeactivationResult DeactivateOwnAccount();
}

/// <summary>
/// Holds the account-service boundary until server deactivation is implemented.
/// </summary>
internal sealed class AccountDeactivationService : IAccountDeactivationService
{
    public AccountDeactivationResult DeactivateOwnAccount()
    {
        // TODO: Reject active matches, mark only the authenticated account inactive,
        // and retain its matches, moves, messages, and results on the server.
        return AccountDeactivationResult.ServerConnectionFailed;
    }
}
