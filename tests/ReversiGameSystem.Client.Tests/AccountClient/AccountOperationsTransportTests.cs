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
[TestCategory("IntegrationWcf")]
public sealed class AccountOperationsTransportTests
{
    private const string SessionToken = "account-operation-session";
    private const string CurrentPassword = "Current-Password-47!";
    private const string NewPassword = "Replacement-Password-82!";
    private const string EmailAddress = "player@example.test";
    private const string VerificationCode = "123456";
    private const string CorrelationId = "account-operation-correlation";

    [TestMethod]
    public async Task ChangePassword_ValidInput_PreservesPasswordAndSessionArguments()
    {
        using AccountHost host = new AccountHost();

        await host.Client.ChangePassword(SessionToken, CurrentPassword, NewPassword);

        Assert.AreEqual((SessionToken, CurrentPassword, NewPassword),
            (host.Service.SessionToken, host.Service.CurrentPassword, host.Service.NewPassword));
    }

    [TestMethod]
    public async Task RequestPasswordResetCode_ValidEmail_PreservesAddress()
    {
        using AccountHost host = new AccountHost();

        await host.Client.RequestPasswordResetCode(EmailAddress);

        Assert.AreEqual(EmailAddress, host.Service.EmailAddress);
    }

    [TestMethod]
    public async Task RequestPasswordResetCode_ConfirmedRequest_ReturnsSuccess()
    {
        using AccountHost host = new AccountHost();

        AccountStatus status = await host.Client.RequestPasswordResetCode(EmailAddress);

        Assert.AreEqual(AccountStatus.Success, status);
    }

    [TestMethod]
    public async Task ResetPassword_ValidInput_PreservesRecoveryArguments()
    {
        using AccountHost host = new AccountHost();

        await host.Client.ResetPassword(EmailAddress, VerificationCode, NewPassword);

        Assert.AreEqual((EmailAddress, VerificationCode, NewPassword),
            (host.Service.EmailAddress, host.Service.VerificationCode, host.Service.NewPassword));
    }

    [TestMethod]
    public async Task DeactivateAccount_ValidInput_PreservesSessionAndPassword()
    {
        using AccountHost host = new AccountHost();

        await host.Client.DeactivateAccount(SessionToken, CurrentPassword);

        Assert.AreEqual((SessionToken, CurrentPassword),
            (host.Service.SessionToken, host.Service.CurrentPassword));
    }

    [TestMethod]
    [DataRow(Proxy.AccountFailure.IncorrectCurrentPassword,
        (int)AccountStatus.IncorrectCurrentPassword)]
    [DataRow(Proxy.AccountFailure.InactiveAccount, (int)AccountStatus.InactiveAccount)]
    [DataRow(Proxy.AccountFailure.ActiveMatchInProgress, (int)AccountStatus.ActiveMatchInProgress)]
    public async Task DeactivateAccount_DeclaredAccountFault_ReturnsExpectedStatus(
        Proxy.AccountFailure failure, int expectedStatus)
    {
        using AccountHost host = new AccountHost();
        host.Service.Failure = new FaultException<Proxy.AccountFault>(
            new Proxy.AccountFault { Reason = failure });

        AccountStatus status = await host.Client.DeactivateAccount(SessionToken, CurrentPassword);

        Assert.AreEqual((AccountStatus)expectedStatus, status);
    }

    [TestMethod]
    [DataRow(Proxy.SessionFailure.MissingSessionToken, (int)AccountStatus.MissingSessionToken)]
    [DataRow(Proxy.SessionFailure.InvalidSession, (int)AccountStatus.InvalidSession)]
    [DataRow(Proxy.SessionFailure.InactiveAccount, (int)AccountStatus.InactiveAccount)]
    public async Task ChangePassword_DeclaredSessionFault_ReturnsExpectedStatus(
        Proxy.SessionFailure failure, int expectedStatus)
    {
        using AccountHost host = new AccountHost();
        host.Service.Failure = new FaultException<Proxy.SessionFault>(
            new Proxy.SessionFault { Reason = failure });

        AccountStatus status = await host.Client.ChangePassword(
            SessionToken, CurrentPassword, NewPassword);

        Assert.AreEqual((AccountStatus)expectedStatus, status);
    }

    [TestMethod]
    [DataRow(Proxy.AccountFailure.InvalidVerificationCode,
        (int)AccountStatus.InvalidVerificationCode)]
    [DataRow(Proxy.AccountFailure.ExpiredVerificationCode,
        (int)AccountStatus.ExpiredVerificationCode)]
    [DataRow(Proxy.AccountFailure.InvalidPassword, (int)AccountStatus.InvalidPassword)]
    public async Task ResetPassword_DeclaredAccountFault_ReturnsExpectedStatus(
        Proxy.AccountFailure failure, int expectedStatus)
    {
        using AccountHost host = new AccountHost();
        host.Service.Failure = new FaultException<Proxy.AccountFault>(
            new Proxy.AccountFault { Reason = failure });

        AccountStatus status = await host.Client.ResetPassword(
            EmailAddress, VerificationCode, NewPassword);

        Assert.AreEqual((AccountStatus)expectedStatus, status);
    }

    [TestMethod]
    public async Task ChangePassword_UnexpectedSessionFault_PropagatesTypedFault()
    {
        using AccountHost host = new AccountHost();
        host.Service.Failure = new FaultException<Proxy.SessionFault>(
            new Proxy.SessionFault { Reason = Proxy.SessionFailure.InvalidCredentials });

        await Assert.ThrowsExactlyAsync<FaultException<Proxy.SessionFault>>(
            () => host.Client.ChangePassword(SessionToken, CurrentPassword, NewPassword));
    }

    [TestMethod]
    public async Task DeactivateAccount_Timeout_ReturnsUnconfirmedOperation()
    {
        Services.AccountClient client = new Services.AccountClient(
            NullLogger<Services.AccountClient>.Instance, () => throw new TimeoutException());

        AccountStatus status = await client.DeactivateAccount(SessionToken, CurrentPassword);

        Assert.AreEqual(AccountStatus.ServiceUnavailable, status);
    }

    [TestMethod]
    public async Task ResetPassword_TechnicalFault_PreservesCorrelationForErrorBoundary()
    {
        using AccountHost host = new AccountHost();
        host.Service.Failure = new FaultException<Proxy.ServiceFault>(new Proxy.ServiceFault
        {
            FailureCode = "test-failure",
            CorrelationId = CorrelationId
        });

        FaultException<Proxy.ServiceFault> failure =
            await Assert.ThrowsExactlyAsync<FaultException<Proxy.ServiceFault>>(
                () => host.Client.ResetPassword(EmailAddress, VerificationCode, NewPassword));

        Assert.AreEqual(CorrelationId, failure.Detail.CorrelationId);
    }

    [TestMethod]
    public async Task DeactivateAccount_TechnicalFault_DoesNotRetry()
    {
        using AccountHost host = new AccountHost();
        host.Service.Failure = new FaultException<Proxy.ServiceFault>(new Proxy.ServiceFault
        {
            FailureCode = "test-failure",
            CorrelationId = CorrelationId
        });

        await Assert.ThrowsExactlyAsync<FaultException<Proxy.ServiceFault>>(
            () => host.Client.DeactivateAccount(SessionToken, CurrentPassword));

        Assert.AreEqual(1, host.Service.DeactivationCalls);
    }

    [ServiceContract(Name = "IAccountService", Namespace = "urn:reversi:services:v1")]
    public interface IAccountOperations
    {
        [OperationContract]
        [FaultContract(typeof(Proxy.AccountFault), Namespace = "urn:reversi:contracts:v1")]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = "urn:reversi:contracts:v1")]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = "urn:reversi:diagnostics:v1")]
        void ChangePassword(string sessionToken, string currentPassword, string newPassword);

        [OperationContract]
        [FaultContract(typeof(Proxy.AccountFault), Namespace = "urn:reversi:contracts:v1")]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = "urn:reversi:diagnostics:v1")]
        DateTime RequestPasswordResetCode(string emailAddress);

        [OperationContract]
        [FaultContract(typeof(Proxy.AccountFault), Namespace = "urn:reversi:contracts:v1")]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = "urn:reversi:diagnostics:v1")]
        void ResetPassword(string emailAddress, string verificationCode, string newPassword);

        [OperationContract]
        [FaultContract(typeof(Proxy.AccountFault), Namespace = "urn:reversi:contracts:v1")]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = "urn:reversi:contracts:v1")]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = "urn:reversi:diagnostics:v1")]
        void DeactivateAccount(string sessionToken, string currentPassword);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        UseSynchronizationContext = false)]
    public sealed class AccountOperationsStub : IAccountOperations
    {
        internal string SessionToken { get; private set; } = string.Empty;
        internal string CurrentPassword { get; private set; } = string.Empty;
        internal string NewPassword { get; private set; } = string.Empty;
        internal string EmailAddress { get; private set; } = string.Empty;
        internal string VerificationCode { get; private set; } = string.Empty;
        internal int DeactivationCalls { get; private set; }
        internal FaultException? Failure { get; set; }

        public void ChangePassword(string sessionToken, string currentPassword, string newPassword)
        {
            SessionToken = sessionToken;
            CurrentPassword = currentPassword;
            NewPassword = newPassword;
            if (Failure != null)
            {
                throw Failure;
            }
        }

        public DateTime RequestPasswordResetCode(string emailAddress)
        {
            EmailAddress = emailAddress;
            if (Failure != null)
            {
                throw Failure;
            }
            return DateTime.MaxValue;
        }

        public void ResetPassword(string emailAddress, string verificationCode, string newPassword)
        {
            EmailAddress = emailAddress;
            VerificationCode = verificationCode;
            NewPassword = newPassword;
            if (Failure != null)
            {
                throw Failure;
            }
        }

        public void DeactivateAccount(string sessionToken, string currentPassword)
        {
            DeactivationCalls++;
            SessionToken = sessionToken;
            CurrentPassword = currentPassword;
            if (Failure != null)
            {
                throw Failure;
            }
        }
    }

    private sealed class AccountHost : IDisposable
    {
        private const int DynamicPort = 0;
        private const int OperationTimeoutSeconds = 5;
        private readonly ServiceHost _host;

        internal AccountOperationsStub Service { get; } = new AccountOperationsStub();

        internal Services.AccountClient Client { get; }

        internal AccountHost()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, DynamicPort);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            string address = "net.tcp://localhost:" + port + "/AccountOperationsTests";
            _host = new ServiceHost(Service);
            _host.AddServiceEndpoint(typeof(IAccountOperations), CreateBinding(), address);
            _host.Open();
            Client = new Services.AccountClient(NullLogger<Services.AccountClient>.Instance,
                () => new Proxy.AccountServiceClient(
                    CreateBinding(), new EndpointAddress(address)));
        }

        private static NetTcpBinding CreateBinding()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(OperationTimeoutSeconds);
            return new NetTcpBinding(SecurityMode.None)
            {
                OpenTimeout = timeout,
                SendTimeout = timeout,
                CloseTimeout = timeout
            };
        }

        public void Dispose()
        {
            _host.Abort();
        }
    }
}
