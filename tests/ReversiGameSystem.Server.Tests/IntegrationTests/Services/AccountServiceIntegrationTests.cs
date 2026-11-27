using System;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.Domain.Accounts;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services;

[TestClass]
[TestCategory("Integration")]
public sealed class AccountServiceIntegrationTests
{
    private const string PrivateProviderMessage = "private-provider-message";

    private AccountServiceBoundaryStub _stub = null!;
    private WcfChannelScope<IAccountService> _scope = null!;
    private IAccountService _channel = null!;

    [TestInitialize]
    public void Initialize()
    {
        _stub = new AccountServiceBoundaryStub();
        VerificationCodeProtector protector = new VerificationCodeProtector(new byte[32]);
        VerificationCodeService verificationCodes = new VerificationCodeService(
            protector,
            _stub,
            _stub,
            _stub);
        SessionStore sessions = new SessionStore(_stub, NullLogger<SessionStore>.Instance);
        AccountApplicationService applicationService = new AccountApplicationService(
            _stub,
            _stub,
            verificationCodes,
            sessions);
        AccountService service = new AccountService(applicationService);
        _scope = WcfTestHostHelper.OpenChannel<IAccountService>(service, "Account");
        _channel = _scope.Channel;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void CompleteRegistration_ThroughWcf_MapsCredentialsAndOutcome()
    {
        string expectedUsername = "PlayerOne";
        string expectedEmail = "player@example.com";
        string expectedLocale = "en-US";
        string expectedMediaType = "image/png";
        byte expectedSecondByte = 80;
        long expectedAccountId = 42L;
        NewAccount newAccount = CreateValidNewAccount(
            expectedUsername,
            expectedEmail,
            expectedLocale,
            expectedMediaType,
            expectedSecondByte);

        AccountReference actualResult = _channel.CompleteRegistration(newAccount);

        Assert.IsNotNull(_stub.LastRegistration);
        Assert.IsNotNull(_stub.LastImage);
        Assert.AreEqual(expectedUsername, _stub.LastRegistration.Username);
        Assert.AreEqual(expectedEmail, _stub.LastRegistration.EmailAddress);
        Assert.AreEqual(expectedLocale, _stub.LastRegistration.PreferredLocaleCode);
        Assert.AreEqual(expectedMediaType, _stub.LastImage.MediaType);
        Assert.AreEqual(expectedSecondByte, _stub.LastImage.Content[1]);
        Assert.AreEqual(expectedAccountId, actualResult.AccountId);
        Assert.AreEqual(expectedUsername, actualResult.Username);
    }

    [TestMethod]
    public void CompleteRegistration_EmailUnavailableThroughWcf_ReturnsTypedFaultAndKeepsOpen()
    {
        _stub.CreateStatus = AccountStatus.EmailUnavailable;
        AccountFailure expectedReason = AccountFailure.EmailUnavailable;
        CommunicationState expectedChannelState = CommunicationState.Opened;
        NewAccount newAccount = CreateValidNewAccount();
        Action action = () => _channel.CompleteRegistration(newAccount);

        FaultException<AccountFault> fault =
            Assert.Throws<FaultException<AccountFault>>(action);
        AccountFailure actualReason = fault.Detail.Reason;
        CommunicationState actualChannelState = _scope.ChannelState;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }

    [TestMethod]
    public void CompleteRegistration_RepositoryFailureThroughWcf_ReturnsSanitizedServiceFault()
    {
        _stub.ThrowOnCreate = true;
        string expectedFailureCode = "ACCOUNT_OPERATION_FAILURE";
        bool expectedHasCorrelation = true;
        bool expectedContainsSecret = false;
        NewAccount newAccount = CreateValidNewAccount();
        Action action = () => _channel.CompleteRegistration(newAccount);

        FaultException<ServiceFault> fault =
            Assert.Throws<FaultException<ServiceFault>>(action);
        string actualFailureCode = fault.Detail.FailureCode;
        bool actualHasCorrelation = !string.IsNullOrWhiteSpace(fault.Detail.CorrelationId);
        bool actualContainsSecret = fault.ToString().Contains(PrivateProviderMessage);

        Assert.AreEqual(expectedFailureCode, actualFailureCode);
        Assert.AreEqual(expectedHasCorrelation, actualHasCorrelation);
        Assert.AreEqual(expectedContainsSecret, actualContainsSecret);
    }

    private static NewAccount CreateValidNewAccount(
        string username = "PlayerOne",
        string email = "player@example.com",
        string locale = "en-US",
        string mediaType = "image/png",
        byte secondByte = 80)
    {
        return new NewAccount
        {
            VerificationCode = "123456",
            Username = username,
            EmailAddress = email,
            Password = "Password1!",
            PreferredLocaleCode = locale,
            ProfileImage = new ImageContent
            {
                MediaType = mediaType,
                Content = new byte[] { 137, secondByte, 78, 71, 13, 10, 26, 10 }
            }
        };
    }

    private sealed class AccountServiceBoundaryStub :
        IAccountRepository,
        IPlayerImageRepository,
        IVerificationCodeRepository,
        ITemplatedEmailSender,
        IUtcClock
    {
        public DateTime UtcNow { get; set; } =
            new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

        internal AccountStatus CreateStatus { get; set; } = AccountStatus.Success;

        internal bool ThrowOnCreate { get; set; }

        internal AccountRegistrationData? LastRegistration { get; private set; }

        internal ProfileImageData? LastImage { get; private set; }

        public AccountResult<LoginAccount> FindByLogin(string loginIdentifier)
        {
            return new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        }

        public AccountResult<LoginAccount> FindByEmail(string emailAddress)
        {
            return new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        }

        public AccountResult<LoginAccount> FindById(long accountId)
        {
            return new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        }

        public AccountStatus CheckRegistrationAvailability(string username, string emailAddress)
        {
            return AccountStatus.Success;
        }

        public AccountResult<long> CreateAccount(AccountRegistrationData registration)
        {
            if (ThrowOnCreate)
            {
                throw new InvalidOperationException(PrivateProviderMessage);
            }

            if (CreateStatus != AccountStatus.Success)
            {
                return new AccountResult<long>(CreateStatus);
            }

            LastRegistration = registration;
            return new AccountResult<long>(42L);
        }

        public AccountStatus UpdatePasswordHash(long accountId, string newPasswordHash)
        {
            return AccountStatus.Success;
        }

        public AccountStatus DeactivateAccount(long accountId)
        {
            return AccountStatus.Success;
        }

        public ProfileImageData ReadImage(string imageReference)
        {
            throw new NotSupportedException();
        }

        public ProfileResult<string> SaveImage(ProfileImageData image, PlayerImageKind kind)
        {
            LastImage = image;
            return new ProfileResult<string>("img/42.png");
        }

        public void DeleteImage(string imageReference)
        {
        }

        public void SavePendingCode(PendingVerificationCode pendingCode)
        {
        }

        public VerificationStatus ProcessVerificationAttempt(
            string identityKey,
            Func<StoredVerificationCode, VerificationEvaluationResult> evaluateAttempt)
        {
            return VerificationStatus.Accepted;
        }

        public void SendTemplatedEmail(
            string recipientAddress,
            string templateName,
            IReadOnlyDictionary<string, string> templateValues)
        {
        }
    }
}
