using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests;

internal sealed class AccountAccessHandlers : ICreateAccountHandler, ILoginHandler
{
    internal LoginCommand? LoginCommand { get; private set; }

    internal LoginResult LoginResult { get; set; } =
        new LoginResult(LoginOutcome.InvalidCredentials);

    public Task<CreateAccountResult> ExecuteAsync(CreateAccountCommand command)
    {
        return Task.FromResult(
            new CreateAccountResult(CreateAccountOutcome.ServiceUnavailable));
    }

    public Task<LoginResult> ExecuteAsync(LoginCommand command)
    {
        LoginCommand = command;
        return Task.FromResult(LoginResult);
    }
}
