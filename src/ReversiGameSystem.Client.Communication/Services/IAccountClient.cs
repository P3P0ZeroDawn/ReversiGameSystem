using System.Threading.Tasks;

namespace ReversiGameSystem.Services;

internal interface IAccountClient
{
    Task<AccountCreationStatus> CreateAccount(
        string username,
        string emailAddress,
        string password,
        string localeCode,
        byte[]? imageContent,
        string? imageMediaType);
}
