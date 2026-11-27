using System.Threading.Tasks;

namespace ReversiGameSystem.Services;

internal interface IAccountClient
{
    Task<AccountStatus> RequestRegistrationCode(string username, string emailAddress);

    Task<AccountStatus> CompleteRegistration(NewAccount account);
}
