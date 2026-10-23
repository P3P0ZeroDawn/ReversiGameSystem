using System;
using System.IO;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Configuration;
using ReversiGameSystem.Server.Persistence.Mail;
using ReversiGameSystem.Server.Persistence.Verification;
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
            try
            {
                AccountAccessService accountAccessService =
                    CreateAccountAccessService(serviceLogger);
                SessionService sessionService =
                    CreateSessionService(loggerFactory.CreateLogger<SessionService>());
                using (ServiceHost serviceHost = new ServiceHost(accountAccessService))
                using (ServiceHost sessionHost = new ServiceHost(sessionService))
                {
                    return Run(serviceHost, sessionHost, hostLogger);
                }
            }
            catch (Exception exception)
            {
                hostLogger.LogCritical("The server could not start. {Diagnostics}",
                    FailureDiagnostics.Describe(exception));
                return 1;
            }
        }
    }

    private static AccountAccessService CreateAccountAccessService(
        ILogger<AccountAccessService> logger)
    {
        SystemUtcClock clock = new SystemUtcClock();
        Pbkdf2PasswordHasher passwordHasher = new Pbkdf2PasswordHasher();
        string imageDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReversiGameSystem.Server",
            "profile-images");

        CreateAccountHandler createAccountHandler = new CreateAccountHandler(
            new EntityFrameworkAccountRegistrationStore(),
            passwordHasher,
            new FileSystemProfileImageStore(imageDirectory),
            clock);
        return new AccountAccessService(createAccountHandler, logger);
    }

    private static SessionService CreateSessionService(ILogger<SessionService> logger)
    {
        SystemUtcClock clock = new SystemUtcClock();
        byte[] secret = Convert.FromBase64String(
            EnvironmentSettings.ReadRequired("REVERSI_VERIFICATION_SECRET"));
        VerificationCodeProtector protector;
        try
        {
            protector = new VerificationCodeProtector(secret);
        }
        finally
        {
            Array.Clear(secret, 0, secret.Length);
        }

        VerificationCodeIssuer issuer = new VerificationCodeIssuer(
            new VerificationEmailSender(MailSettings.Load()), protector, clock);
        VerificationCodeService verification = new VerificationCodeService(
            issuer, new VerificationCodeRepository(), clock);
        SessionRegistry sessions = new SessionRegistry(
            new InMemoryAuthenticatedSessionStore(clock), clock);
        SessionHandler handler = new SessionHandler(new VerifiedLoginService(
            new EntityFrameworkAccountLoginStore(), new Pbkdf2PasswordHasher()),
            verification, sessions);
        return new SessionService(handler, logger);
    }

    private static int Run(ServiceHost serviceHost, ServiceHost sessionHost, ILogger logger)
    {
        try
        {
            serviceHost.Open();
            sessionHost.Open();
            logger.LogInformation(
                "The Reversi server is listening. Press Enter to stop it.");
            Console.ReadLine();
            sessionHost.Close();
            serviceHost.Close();
            return 0;
        }
        catch (Exception exception)
        {
            logger.LogCritical("The Reversi server stopped unexpectedly. {Diagnostics}",
                FailureDiagnostics.Describe(exception));
            sessionHost.Abort();
            serviceHost.Abort();
            return 1;
        }
    }
}
