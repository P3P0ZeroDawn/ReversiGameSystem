using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests.AccountClient;

[TestClass]
[TestCategory("Integration")]
[TestCategory("IntegrationWcf")]
public sealed class RegistrationTransportTests
{
    [TestMethod]
    public async Task RequestRegistrationCode_Identity_PreservesRemoteIdentity()
    {
        using RegistrationHost host = new RegistrationHost();
        Services.AccountClient client = host.CreateClient();

        await client.RequestRegistrationCode("remote-player", "player@example.test");

        Assert.AreEqual("remote-player|player@example.test", host.Service.RequestedIdentity);
    }

    [TestMethod]
    public async Task RequestRegistrationCode_Success_DoesNotCompleteRegistration()
    {
        using RegistrationHost host = new RegistrationHost();
        Services.AccountClient client = host.CreateClient();

        await client.RequestRegistrationCode("remote-player", "player@example.test");

        Assert.AreEqual(0, host.Service.CompletionCalls);
    }

    [TestMethod]
    public async Task CompleteRegistration_AccountFields_PreservesRegistrationPayload()
    {
        using RegistrationHost host = new RegistrationHost();
        Services.AccountClient client = host.CreateClient();
        NewAccount account = CreateAccount();

        await client.CompleteRegistration(account);

        Proxy.NewAccount received = host.Service.Registration!;
        CollectionAssert.AreEqual(
            new[] { account.VerificationCode, account.Username, account.EmailAddress,
                account.Password, account.PreferredLocaleCode },
            new[] { received.VerificationCode, received.Username, received.EmailAddress,
                received.Password, received.PreferredLocaleCode });
    }

    [TestMethod]
    public async Task CompleteRegistration_ProfileImage_PreservesImageContent()
    {
        using RegistrationHost host = new RegistrationHost();
        Services.AccountClient client = host.CreateClient();
        NewAccount account = CreateAccount();
        account.ProfileImage = new PlayerImageContent(new byte[] { 3, 5, 8 }, "image/png");

        await client.CompleteRegistration(account);

        CollectionAssert.AreEqual(account.ProfileImage.Content, host.Service.Registration!.ProfileImage.Content);
        Assert.AreEqual(account.ProfileImage.MediaType, host.Service.Registration.ProfileImage.MediaType);
    }

    [TestMethod]
    public async Task CompleteRegistration_NoProfileImage_PreservesAbsentImage()
    {
        using RegistrationHost host = new RegistrationHost();
        Services.AccountClient client = host.CreateClient();

        await client.CompleteRegistration(CreateAccount());

        Assert.IsNull(host.Service.Registration!.ProfileImage);
    }

    [TestMethod]
    [DataRow(Proxy.AccountFailure.InvalidVerificationCode, (int)AccountStatus.InvalidVerificationCode)]
    [DataRow(Proxy.AccountFailure.ExpiredVerificationCode, (int)AccountStatus.ExpiredVerificationCode)]
    public async Task CompleteRegistration_RejectedCode_ReturnsMappedStatus(
        Proxy.AccountFailure reason, int expectedStatus)
    {
        using RegistrationHost host = new RegistrationHost();
        host.Service.Failure = new FaultException<Proxy.AccountFault>(new Proxy.AccountFault { Reason = reason });
        Services.AccountClient client = host.CreateClient();

        AccountStatus actualStatus = await client.CompleteRegistration(CreateAccount());

        Assert.AreEqual((AccountStatus)expectedStatus, actualStatus);
    }

    [TestMethod]
    public async Task CompleteRegistration_RejectedCode_DoesNotReplaySubmission()
    {
        using RegistrationHost host = new RegistrationHost();
        host.Service.Failure = new FaultException<Proxy.AccountFault>(new Proxy.AccountFault
        {
            Reason = Proxy.AccountFailure.InvalidVerificationCode
        });
        Services.AccountClient client = host.CreateClient();

        await client.CompleteRegistration(CreateAccount());

        Assert.AreEqual(1, host.Service.CompletionCalls);
    }

    [TestMethod]
    public async Task CompleteRegistration_ServiceFault_PreservesCorrelationId()
    {
        using RegistrationHost host = new RegistrationHost();
        host.Service.Failure = CreateServiceFault();
        Services.AccountClient client = host.CreateClient();

        FaultException<Proxy.ServiceFault> failure = await Assert.ThrowsExactlyAsync<FaultException<Proxy.ServiceFault>>(
            () => client.CompleteRegistration(CreateAccount()));

        Assert.AreEqual("registration-correlation", failure.Detail.CorrelationId);
    }

    [TestMethod]
    public async Task CompleteRegistration_ServiceFault_DoesNotReplaySubmission()
    {
        using RegistrationHost host = new RegistrationHost();
        host.Service.Failure = CreateServiceFault();
        Services.AccountClient client = host.CreateClient();

        await Assert.ThrowsExactlyAsync<FaultException<Proxy.ServiceFault>>(
            () => client.CompleteRegistration(CreateAccount()));

        Assert.AreEqual(1, host.Service.CompletionCalls);
    }

    [TestMethod]
    public async Task CompleteRegistration_UnrelatedAccountFault_PropagatesTypedFault()
    {
        using RegistrationHost host = new RegistrationHost();
        host.Service.Failure = new FaultException<Proxy.AccountFault>(new Proxy.AccountFault
        {
            Reason = Proxy.AccountFailure.InactiveAccount
        });
        Services.AccountClient client = host.CreateClient();

        FaultException<Proxy.AccountFault> failure = await Assert.ThrowsExactlyAsync<FaultException<Proxy.AccountFault>>(
            () => client.CompleteRegistration(CreateAccount()));

        Assert.AreEqual(Proxy.AccountFailure.InactiveAccount, failure.Detail.Reason);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public async Task CompleteRegistration_InvalidAccountReference_ThrowsInvalidOperation(int accountId)
    {
        using RegistrationHost host = new RegistrationHost();
        host.Service.AccountReference = new Proxy.AccountReference { AccountId = accountId };
        Services.AccountClient client = host.CreateClient();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => client.CompleteRegistration(CreateAccount()));
    }

    [TestMethod]
    public async Task CompleteRegistration_MissingAccountReference_ThrowsInvalidOperation()
    {
        using RegistrationHost host = new RegistrationHost();
        host.Service.AccountReference = null;
        Services.AccountClient client = host.CreateClient();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => client.CompleteRegistration(CreateAccount()));
    }

    [TestMethod]
    public async Task RequestRegistrationCode_ClientFactoryFailure_PropagatesProgrammingException()
    {
        Services.AccountClient client = new Services.AccountClient(NullLogger<Services.AccountClient>.Instance,
            () => throw new InvalidOperationException("Test client creation failure."));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => client.RequestRegistrationCode("remote-player", "player@example.test"));
    }

    [TestMethod]
    public async Task CompleteRegistration_ConfirmedAccount_ReturnsSuccess()
    {
        using RegistrationHost host = new RegistrationHost();
        Services.AccountClient client = host.CreateClient();

        AccountStatus actualStatus = await client.CompleteRegistration(CreateAccount());

        Assert.AreEqual(AccountStatus.Success, actualStatus);
    }

    [TestMethod]
    public async Task RequestRegistrationCode_ConfirmedDelivery_ReturnsSuccess()
    {
        using RegistrationHost host = new RegistrationHost();
        Services.AccountClient client = host.CreateClient();

        AccountStatus actualStatus = await client.RequestRegistrationCode("remote-player", "player@example.test");

        Assert.AreEqual(AccountStatus.Success, actualStatus);
    }

    [TestMethod]
    public async Task RequestRegistrationCode_Timeout_ReturnsServiceUnavailable()
    {
        Services.AccountClient client = new Services.AccountClient(NullLogger<Services.AccountClient>.Instance,
            () => throw new TimeoutException("Test timeout."));

        AccountStatus actualStatus = await client.RequestRegistrationCode("remote-player", "player@example.test");

        Assert.AreEqual(AccountStatus.ServiceUnavailable, actualStatus);
    }
    private static NewAccount CreateAccount()
    {
        return new NewAccount
        {
            VerificationCode = "123456",
            Username = "remote-player",
            EmailAddress = "player@example.test",
            Password = "Disposable-Registration-47!",
            PreferredLocaleCode = "es-MX"
        };
    }

    private static FaultException<Proxy.ServiceFault> CreateServiceFault()
    {
        return new FaultException<Proxy.ServiceFault>(new Proxy.ServiceFault
        {
            FailureCode = "test-unavailable",
            CorrelationId = "registration-correlation",
            IsTransient = true
        });
    }

    [ServiceContract(Name = "IAccountService", Namespace = "urn:reversi:services:v1")]
    public interface IRegistrationTransportService
    {
        [OperationContract]
        [FaultContract(typeof(Proxy.AccountFault), Namespace = "urn:reversi:contracts:v1")]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = "urn:reversi:diagnostics:v1")]
        DateTime RequestRegistrationCode(string username, string emailAddress);

        [OperationContract]
        [FaultContract(typeof(Proxy.AccountFault), Namespace = "urn:reversi:contracts:v1")]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = "urn:reversi:diagnostics:v1")]
        Proxy.AccountReference? CompleteRegistration(Proxy.NewAccount account);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, UseSynchronizationContext = false)]
    public sealed class RegistrationServiceStub : IRegistrationTransportService
    {
        internal string RequestedIdentity { get; private set; } = string.Empty;
        internal Proxy.NewAccount? Registration { get; private set; }
        internal int CompletionCalls { get; private set; }
        internal Exception? Failure { get; set; }
        internal Proxy.AccountReference? AccountReference { get; set; } = new Proxy.AccountReference { AccountId = 41 };

        public DateTime RequestRegistrationCode(string username, string emailAddress)
        {
            RequestedIdentity = username + "|" + emailAddress;
            if (Failure != null)
            {
                throw Failure;
            }
            return new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public Proxy.AccountReference? CompleteRegistration(Proxy.NewAccount account)
        {
            CompletionCalls++;
            Registration = account;
            if (Failure != null)
            {
                throw Failure;
            }
            return AccountReference;
        }
    }

    private sealed class RegistrationHost : IDisposable
    {
        private readonly ServiceHost _host;
        private readonly string _address;
        internal RegistrationServiceStub Service { get; } = new RegistrationServiceStub();

        internal RegistrationHost()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            _address = "net.tcp://localhost:" + port + "/RegistrationTransportTests";
            _host = new ServiceHost(Service);
            _host.AddServiceEndpoint(typeof(IRegistrationTransportService), CreateBinding(), _address);
            _host.Open();
        }

        internal Services.AccountClient CreateClient()
        {
            return new Services.AccountClient(NullLogger<Services.AccountClient>.Instance,
                () => new Proxy.AccountServiceClient(CreateBinding(), new EndpointAddress(_address)));
        }

        private static NetTcpBinding CreateBinding()
        {
            return new NetTcpBinding(SecurityMode.None)
            {
                OpenTimeout = TimeSpan.FromSeconds(5),
                SendTimeout = TimeSpan.FromSeconds(5),
                CloseTimeout = TimeSpan.FromSeconds(5)
            };
        }

        public void Dispose()
        {
            _host.Abort();
        }
    }
}
