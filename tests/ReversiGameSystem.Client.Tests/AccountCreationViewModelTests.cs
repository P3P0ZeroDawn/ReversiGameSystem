using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Presentation.AccountCreation;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class AccountCreationViewModelTests
{
    private const string ValidCode = "123456";

    [TestMethod]
    public void RequestRegistrationCode_MissingInformation_DoesNotCallServer()
    {
        AccountClientStub accounts = new AccountClientStub();
        AccountCreationViewModel viewModel = new AccountCreationViewModel(accounts);

        AccountStatus status = viewModel.RequestRegistrationCode().GetAwaiter().GetResult();

        Assert.AreEqual(AccountStatus.MissingRequiredInformation, status);
        Assert.AreEqual(0, accounts.RequestCalls);
    }

    [TestMethod]
    public void RequestRegistrationCode_ValidInput_NormalizesAndCallsServer()
    {
        AccountClientStub accounts = new AccountClientStub();
        AccountCreationViewModel viewModel = CreateValidViewModel(accounts);
        viewModel.Username = "  Player123  ";
        viewModel.EmailAddress = "  player@example.test  ";

        AccountStatus status = viewModel.RequestRegistrationCode().GetAwaiter().GetResult();

        Assert.AreEqual(AccountStatus.Success, status);
        Assert.AreEqual("Player123", accounts.Username);
        Assert.AreEqual("player@example.test", accounts.EmailAddress);
    }

    [TestMethod]
    public void RequestRegistrationCode_ConcurrentSubmission_ReturnsSubmissionInProgress()
    {
        AccountClientStub accounts = new AccountClientStub { DelayRequest = true };
        AccountCreationViewModel viewModel = CreateValidViewModel(accounts);
        Task<AccountStatus> firstRequest = viewModel.RequestRegistrationCode();

        AccountStatus secondStatus = viewModel.RequestRegistrationCode().GetAwaiter().GetResult();
        accounts.CompleteRequest(AccountStatus.Success);
        firstRequest.GetAwaiter().GetResult();

        Assert.AreEqual(AccountStatus.SubmissionInProgress, secondStatus);
        Assert.AreEqual(1, accounts.RequestCalls);
    }

    [TestMethod]
    public async Task CompleteRegistration_WithoutCodeRequest_DoesNotSubmitAccount()
    {
        AccountClientStub accounts = new AccountClientStub();
        AccountCreationViewModel viewModel = CreateValidViewModel(accounts);

        AccountStatus status = await viewModel.CompleteRegistration(ValidCode);

        Assert.AreEqual(AccountStatus.MissingRequiredInformation, status);
        Assert.AreEqual(0, accounts.RegistrationCalls);
    }

    [TestMethod]
    public async Task CompleteRegistration_ChangedForm_UsesRequestedIdentity()
    {
        AccountClientStub accounts = new AccountClientStub();
        AccountCreationViewModel viewModel = CreateValidViewModel(accounts);
        await viewModel.RequestRegistrationCode();
        viewModel.Username = "AnotherPlayer";
        viewModel.EmailAddress = "another@example.test";

        await viewModel.CompleteRegistration(ValidCode);

        Assert.AreEqual("Player123", accounts.RegisteredUsername);
        Assert.AreEqual("player@example.test", accounts.RegisteredEmail);
    }

    [TestMethod]
    public async Task CompleteRegistration_InvalidCode_AllowsCorrectionWithoutNewRequest()
    {
        AccountClientStub accounts = new AccountClientStub
        {
            RegistrationStatus = AccountStatus.InvalidVerificationCode
        };
        AccountCreationViewModel viewModel = CreateValidViewModel(accounts);
        await viewModel.RequestRegistrationCode();
        await viewModel.CompleteRegistration("000000");
        accounts.RegistrationStatus = AccountStatus.Success;

        AccountStatus status = await viewModel.CompleteRegistration(ValidCode);

        Assert.AreEqual(AccountStatus.Success, status);
        Assert.AreEqual(1, accounts.RequestCalls);
        Assert.AreEqual(2, accounts.RegistrationCalls);
    }

    [TestMethod]
    public async Task CompleteRegistration_ExpiredCode_RequiresNewRequest()
    {
        AccountClientStub accounts = new AccountClientStub
        {
            RegistrationStatus = AccountStatus.ExpiredVerificationCode
        };
        AccountCreationViewModel viewModel = CreateValidViewModel(accounts);
        await viewModel.RequestRegistrationCode();
        await viewModel.CompleteRegistration(ValidCode);

        AccountStatus status = await viewModel.CompleteRegistration(ValidCode);

        Assert.AreEqual(AccountStatus.MissingRequiredInformation, status);
        Assert.AreEqual(1, accounts.RegistrationCalls);
    }

    [TestMethod]
    public async Task CompleteRegistration_Success_ClearsSensitiveFormState()
    {
        AccountClientStub accounts = new AccountClientStub();
        AccountCreationViewModel viewModel = CreateValidViewModel(accounts);
        await viewModel.RequestRegistrationCode();

        await viewModel.CompleteRegistration(ValidCode);

        Assert.AreEqual(string.Empty, viewModel.Password);
        Assert.AreEqual(string.Empty, viewModel.PasswordConfirmation);
        Assert.IsFalse(viewModel.IsSubmitting);
    }

    [TestMethod]
    public async Task CompleteRegistration_UnexpectedFailure_PropagatesAndRestoresSubmittingFlag()
    {
        AccountClientStub accounts = new AccountClientStub { ThrowOnRegistration = true };
        AccountCreationViewModel viewModel = CreateValidViewModel(accounts);
        await viewModel.RequestRegistrationCode();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => viewModel.CompleteRegistration(ValidCode));

        Assert.IsFalse(viewModel.IsSubmitting);
    }

    [TestMethod]
    public async Task RequestRegistrationCode_ServerRejection_DoesNotAllowRegistration()
    {
        AccountClientStub accounts = new AccountClientStub
        {
            RequestStatus = AccountStatus.EmailUnavailable
        };
        AccountCreationViewModel viewModel = CreateValidViewModel(accounts);
        await viewModel.RequestRegistrationCode();

        AccountStatus status = await viewModel.CompleteRegistration(ValidCode);

        Assert.AreEqual(AccountStatus.MissingRequiredInformation, status);
        Assert.AreEqual(0, accounts.RegistrationCalls);
    }

    private static AccountCreationViewModel CreateValidViewModel(IAccountClient accounts)
    {
        return new AccountCreationViewModel(accounts)
        {
            Username = "Player123",
            EmailAddress = "player@example.test",
            Password = "Aa12345!",
            PasswordConfirmation = "Aa12345!",
            LocaleCode = "es-MX"
        };
    }

    private sealed class AccountClientStub : IAccountClient
    {
        private readonly TaskCompletionSource<AccountStatus> _request =
            new TaskCompletionSource<AccountStatus>();

        internal int RequestCalls { get; private set; }

        internal int RegistrationCalls { get; private set; }

        internal string Username { get; private set; } = string.Empty;

        internal string EmailAddress { get; private set; } = string.Empty;

        internal string RegisteredUsername { get; private set; } = string.Empty;

        internal string RegisteredEmail { get; private set; } = string.Empty;

        internal bool DelayRequest { get; set; }

        internal bool ThrowOnRegistration { get; set; }

        internal AccountStatus RequestStatus { get; set; } = AccountStatus.Success;

        internal AccountStatus RegistrationStatus { get; set; } = AccountStatus.Success;

        public Task<AccountStatus> RequestRegistrationCode(string username, string emailAddress)
        {
            RequestCalls++;
            Username = username;
            EmailAddress = emailAddress;
            Task<AccountStatus> status = DelayRequest
                ? _request.Task : Task.FromResult(RequestStatus);
            return status;
        }

        public Task<AccountStatus> CompleteRegistration(NewAccount account)
        {
            RegistrationCalls++;
            if (ThrowOnRegistration)
            {
                throw new InvalidOperationException("Unexpected test failure.");
            }
            RegisteredUsername = account.Username;
            RegisteredEmail = account.EmailAddress;
            return Task.FromResult(RegistrationStatus);
        }

        internal void CompleteRequest(AccountStatus status)
        {
            _request.SetResult(status);
        }
    }
}
