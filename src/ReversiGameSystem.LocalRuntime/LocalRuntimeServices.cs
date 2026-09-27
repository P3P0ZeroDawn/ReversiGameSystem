using System;
using ReversiGameSystem.Contracts.Services;
using ReversiGameSystem.LocalRuntime.Services;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Application.Players;

namespace ReversiGameSystem.LocalRuntime;

public sealed class LocalRuntimeServices
{
    internal LocalRuntimeServices(IAccountService accountService, IPlayerService playerService)
    {
        AccountService = accountService;
        PlayerService = playerService;
    }

    public IAccountService AccountService { get; }

    public IPlayerService PlayerService { get; }
}

public static class LocalRuntimeFactory
{
    public static LocalRuntimeServices Create(
        ICreateAccountHandler createAccountHandler,
        ILoginHandler loginHandler,
        IGetPlayerProfileHandler getPlayerProfileHandler,
        IGetLeaderboardHandler getLeaderboardHandler)
    {
        if (createAccountHandler == null)
        {
            throw new ArgumentNullException(nameof(createAccountHandler));
        }

        if (loginHandler == null)
        {
            throw new ArgumentNullException(nameof(loginHandler));
        }

        if (getPlayerProfileHandler == null)
        {
            throw new ArgumentNullException(nameof(getPlayerProfileHandler));
        }

        if (getLeaderboardHandler == null)
        {
            throw new ArgumentNullException(nameof(getLeaderboardHandler));
        }

        return new LocalRuntimeServices(
            new LocalAccountService(createAccountHandler, loginHandler),
            new LocalPlayerService(getPlayerProfileHandler, getLeaderboardHandler));
    }
}
