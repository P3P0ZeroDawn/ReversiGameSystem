using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.ServiceApi.Wcf;
using ApplicationSession =
    ReversiGameSystem.Server.Application.Accounts.AuthenticatedSession;
using ContractSession = ReversiGameSystem.Server.Contracts.Accounts.AuthenticatedSession;

namespace ReversiGameSystem.Server.Tests;

internal sealed class AccountAccessServiceTests
{
    internal void LoginAsync_ValidCredentials_MapsAuthenticatedSession()
    {
        // Arrange
        DateTime expiration = new DateTime(
            2026,
            10,
            17,
            2,
            0,
            0,
            DateTimeKind.Utc);
        AccountAccessHandlers handlers = new AccountAccessHandlers
        {
            LoginResult = new LoginResult(
                LoginOutcome.Success,
                new ApplicationSession(
                    "session-token",
                    42,
                    "PlayerOne",
                    "player@example.com",
                    "en-US",
                    expiration))
        };
        AccountAccessService service = CreateService(handlers);

        // Act
        AuthenticationOutcome outcome = service.LoginAsync(
            new AuthenticationCredentials
            {
                LoginIdentifier = "PlayerOne",
                Password = "Password1!"
            }).GetAwaiter().GetResult();

        // Assert
        TestAssert.AreEqual("PlayerOne", handlers.LoginCommand?.Login);
        TestAssert.AreEqual(AuthenticationStatus.Success, outcome.Status);
        ContractSession session = TestAssert.IsNotNull(outcome.Session);
        TestAssert.AreEqual("session-token", session.SessionToken);
        TestAssert.AreEqual<long>(42, session.Account.AccountId);
        TestAssert.AreEqual("en-US", session.PreferredLocaleCode);
        TestAssert.AreEqual(expiration, session.ExpiresAtUtc);
    }

    internal void LoginAsync_MissingCredentials_ReturnsInvalidCredentials()
    {
        // Arrange
        AccountAccessHandlers handlers = new AccountAccessHandlers
        {
            LoginResult = new LoginResult(LoginOutcome.InvalidRequest)
        };
        AccountAccessService service = CreateService(handlers);

        // Act
        AuthenticationOutcome outcome = service.LoginAsync(
            new AuthenticationCredentials()).GetAwaiter().GetResult();

        // Assert
        TestAssert.AreEqual(string.Empty, handlers.LoginCommand?.Login);
        TestAssert.AreEqual(AuthenticationStatus.InvalidCredentials, outcome.Status);
        TestAssert.AreEqual<ContractSession?>(null, outcome.Session);
    }

    internal void LoginAsync_ThroughWcfChannel_ReturnsAuthenticatedSession()
    {
        // Arrange
        AccountAccessHandlers handlers = new AccountAccessHandlers
        {
            LoginResult = new LoginResult(
                LoginOutcome.Success,
                new ApplicationSession(
                    "session-token",
                    42,
                    "PlayerOne",
                    "player@example.com",
                    "en-US",
                    DateTime.UtcNow.AddHours(8)))
        };
        AccountAccessService service = CreateService(handlers);
        Uri baseAddress = new Uri(
            "net.pipe://localhost/ReversiGameSystem.Tests/"
                + Guid.NewGuid().ToString("N")
                + "/");
        NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);

        // Act
        AuthenticationOutcome outcome = InvokeThroughWcf(service, baseAddress, binding);

        // Assert
        TestAssert.AreEqual(AuthenticationStatus.Success, outcome.Status);
        TestAssert.AreEqual("PlayerOne", outcome.Session?.Account.Username);
        TestAssert.AreEqual("en-US", outcome.Session?.PreferredLocaleCode);
    }

    private static AccountAccessService CreateService(AccountAccessHandlers handlers)
    {
        return new AccountAccessService(
            handlers,
            handlers,
            NullLogger<AccountAccessService>.Instance);
    }

    private static AuthenticationOutcome InvokeThroughWcf(
        AccountAccessService service,
        Uri baseAddress,
        Binding binding)
    {
        using (ServiceHost host = new ServiceHost(service, baseAddress))
        {
            host.AddServiceEndpoint(typeof(IAccountAccessService), binding, "AccountAccess");
            host.Open();
            ChannelFactory<IAccountAccessService> factory =
                new ChannelFactory<IAccountAccessService>(
                    binding,
                    new EndpointAddress(new Uri(baseAddress, "AccountAccess")));
            IAccountAccessService channel = factory.CreateChannel();
            try
            {
                AuthenticationOutcome outcome = channel.LoginAsync(
                    new AuthenticationCredentials
                    {
                        LoginIdentifier = "PlayerOne",
                        Password = "Password1!"
                    }).GetAwaiter().GetResult();
                ((ICommunicationObject)channel).Close();
                factory.Close();
                return outcome;
            }
            catch
            {
                ((ICommunicationObject)channel).Abort();
                factory.Abort();
                throw;
            }
        }
    }
}
