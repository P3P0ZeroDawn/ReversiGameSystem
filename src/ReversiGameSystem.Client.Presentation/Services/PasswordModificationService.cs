namespace ReversiGameSystem.Services;

/// <summary>
/// Represents the account-server response to a password change request.
/// </summary>
internal enum PasswordModificationResult
{
    Success,
    IncorrectCurrentPassword,
    BlockedNewPassword,
    ServerConnectionFailed,
    PersistenceError
}

internal interface IPasswordModificationService
{
    public PasswordModificationResult ChangePassword(string currentPassword, string newPassword);
}

/// <summary>
/// Holds the service boundary until account-server communication is implemented.
/// </summary>
internal sealed class PasswordModificationService : IPasswordModificationService
{
    public PasswordModificationResult ChangePassword(string currentPassword, string newPassword)
    {
        // TODO: Authenticate the current password and persist the new one on the server.
        return PasswordModificationResult.ServerConnectionFailed;
    }
}
