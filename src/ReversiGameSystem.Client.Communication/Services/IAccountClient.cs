using System.Threading.Tasks;

namespace ReversiGameSystem.Services;

internal interface IAccountClient
{
    Task<AccountStatus> RequestRegistrationCode(string username, string emailAddress);

    Task<AccountStatus> CompleteRegistration(NewAccount account);

    Task<AccountStatus> ChangePassword(
        string sessionToken, string currentPassword, string newPassword);

    Task<AccountStatus> RequestPasswordResetCode(string emailAddress);

    Task<AccountStatus> ResetPassword(
        string emailAddress, string verificationCode, string newPassword);

    Task<AccountStatus> DeactivateAccount(string sessionToken, string currentPassword);
}
