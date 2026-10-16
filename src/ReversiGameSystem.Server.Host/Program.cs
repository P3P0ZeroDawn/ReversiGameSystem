using System;
using System.IO;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Host;

internal static class Program
{
    private static int Main()
    {
        using (ILoggerFactory loggerFactory = LoggerFactory.Create(
            builder => builder
                .SetMinimumLevel(LogLevel.Information)
                .AddSimpleConsole(options => options.SingleLine = true)))
        {
            ILogger<AccountAccessService> serviceLogger =
                loggerFactory.CreateLogger<AccountAccessService>();
            ILogger<ServerHostLogCategory> hostLogger =
                loggerFactory.CreateLogger<ServerHostLogCategory>();
            AccountAccessService accountAccessService = CreateAccountAccessService(
                serviceLogger);
            using (ServiceHost serviceHost = new ServiceHost(accountAccessService))
            {
                return Run(serviceHost, hostLogger);
            }
        }
    }

    private static AccountAccessService CreateAccountAccessService(
        ILogger<AccountAccessService> logger)
    {
        SystemUtcClock clock = new SystemUtcClock();
        Pbkdf2PasswordHasher passwordHasher = new Pbkdf2PasswordHasher();
        InMemoryAuthenticatedSessionStore sessionStore =
            new InMemoryAuthenticatedSessionStore(clock);
        string imageDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReversiGameSystem.Server",
            "profile-images");

        CreateAccountHandler createAccountHandler = new CreateAccountHandler(
            new EntityFrameworkAccountRegistrationStore(),
            passwordHasher,
            new FileSystemProfileImageStore(imageDirectory),
            clock);
        LoginHandler loginHandler = new LoginHandler(
            new EntityFrameworkAccountLoginStore(),
            passwordHasher,
            sessionStore);

        return new AccountAccessService(createAccountHandler, loginHandler, logger);
    }

    private static int Run(ServiceHost serviceHost, ILogger logger)
    {
        try
        {
            serviceHost.Open();
            logger.LogInformation(
                "The Reversi server is listening. Press Enter to stop it.");
            Console.ReadLine();
            serviceHost.Close();
            return 0;
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "The Reversi server stopped unexpectedly.");
            serviceHost.Abort();
            return 1;
        }
    }
}
