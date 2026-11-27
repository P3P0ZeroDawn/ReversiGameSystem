using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Features.Accounts;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
[TestCategory("Unit")]
public sealed class AccountViewModelTests
{
    private const string CurrentPassword = "Current-Password-47!";
    private const string NewPassword = "Replacement-Password-82!";
    private const string VerificationCode = "123456";
    private const string EmailAddress = "player@example.test";

    [TestMethod]
    public async Task ChangePassword_MismatchedConfirmation_DoesNotCallServer()
    {
        AccountClientStub accounts = new AccountClientStub();
        AccountViewModel viewModel = new AccountViewModel(accounts, CreateSession());

        await viewModel.ChangePassword(CurrentPassword, NewPassword, CurrentPassword);

        Assert.AreEqual(0, accounts.OperationCalls);
    }

    [TestMethod]
    public async Task ChangePassword_InvalidNewPassword_ReturnsInvalidPassword()
    {
        AccountViewModel viewModel = new AccountViewModel(new AccountClientStub(), CreateSession());

        AccountStatus status = await viewModel.ChangePassword(CurrentPassword, "weak", "weak");

        Assert.AreEqual(AccountStatus.InvalidPassword, status);
    }

    [TestMethod]
    public async Task ChangePassword_IncorrectCurrentPassword_PreservesRejection()
    {
        AccountClientStub accounts = new AccountClientStub
        {
            Operation = Task.FromResult(AccountStatus.IncorrectCurrentPassword)
        };
        AccountViewModel viewModel = new AccountViewModel(accounts, CreateSession());

        AccountStatus status = await viewModel.ChangePassword(
            CurrentPassword, NewPassword, NewPassword);

        Assert.AreEqual(AccountStatus.IncorrectCurrentPassword, status);
    }

    [TestMethod]
    public async Task ChangePassword_ConcurrentSubmission_DoesNotSendDuplicate()
    {
        TaskCompletionSource<AccountStatus> completion = new TaskCompletionSource<AccountStatus>();
        AccountClientStub accounts = new AccountClientStub { Operation = completion.Task };
        AccountViewModel viewModel = new AccountViewModel(accounts, CreateSession());

        Task<AccountStatus> first = viewModel.ChangePassword(
            CurrentPassword, NewPassword, NewPassword);
        await viewModel.ChangePassword(CurrentPassword, NewPassword, NewPassword);
        completion.SetResult(AccountStatus.Success);
        await first;

        Assert.AreEqual(1, accounts.OperationCalls);
    }

    [TestMethod]
    public async Task ChangePassword_UnexpectedFailure_ReleasesSubmissionState()
    {
        AccountClientStub accounts = new AccountClientStub
        {
            Operation = Task.FromException<AccountStatus>(new InvalidOperationException())
        };
        AccountViewModel viewModel = new AccountViewModel(accounts, CreateSession());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => viewModel.ChangePassword(CurrentPassword, NewPassword, NewPassword));

        Assert.IsFalse(viewModel.IsSubmitting);
    }

    [TestMethod]
    public async Task RequestPasswordResetCode_InvalidEmail_DoesNotCallServer()
    {
        AccountClientStub accounts = new AccountClientStub();
        AccountViewModel viewModel = new AccountViewModel(accounts, new ClientSession());

        await viewModel.RequestPasswordResetCode("not-an-email");

        Assert.AreEqual(0, accounts.OperationCalls);
    }

    [TestMethod]
    public async Task ResetPassword_UnconfirmedRequest_DoesNotCallServer()
    {
        AccountClientStub accounts = new AccountClientStub
        {
            Operation = Task.FromResult(AccountStatus.ServiceUnavailable)
        };
        AccountViewModel viewModel = new AccountViewModel(accounts, new ClientSession());

        await viewModel.RequestPasswordResetCode(EmailAddress);
        await viewModel.ResetPassword(VerificationCode, NewPassword);

        Assert.AreEqual(1, accounts.OperationCalls);
    }

    [TestMethod]
    public async Task ResetPassword_NormalizedEmail_UsesConfirmedRequestIdentity()
    {
        AccountClientStub accounts = new AccountClientStub();
        AccountViewModel viewModel = new AccountViewModel(accounts, new ClientSession());

        await viewModel.RequestPasswordResetCode(" " + EmailAddress + " ");
        await viewModel.ResetPassword(VerificationCode, NewPassword);

        Assert.AreEqual(EmailAddress, accounts.ResetEmailAddress);
    }

    [TestMethod]
    public async Task ResetPassword_InvalidCodeShape_DoesNotCallServer()
    {
        AccountClientStub accounts = new AccountClientStub();
        AccountViewModel viewModel = new AccountViewModel(accounts, new ClientSession());

        await viewModel.RequestPasswordResetCode(EmailAddress);
        await viewModel.ResetPassword("invalid", NewPassword);

        Assert.AreEqual(1, accounts.OperationCalls);
    }

    [TestMethod]
    public async Task ResetPassword_RejectedCode_AllowsCorrectedCode()
    {
        AccountClientStub accounts = new AccountClientStub();
        AccountViewModel viewModel = new AccountViewModel(accounts, new ClientSession());

        await viewModel.RequestPasswordResetCode(EmailAddress);
        accounts.Operation = Task.FromResult(AccountStatus.InvalidVerificationCode);
        await viewModel.ResetPassword(VerificationCode, NewPassword);
        accounts.Operation = Task.FromResult(AccountStatus.Success);
        AccountStatus status = await viewModel.ResetPassword(VerificationCode, NewPassword);

        Assert.AreEqual(AccountStatus.Success, status);
    }

    [TestMethod]
    [DataRow((int)AccountStatus.Success)]
    [DataRow((int)AccountStatus.ExpiredVerificationCode)]
    public async Task ResetPassword_CompletedOrExpiredRequest_RequiresNewRequest(
        int completionValue)
    {
        AccountStatus completionStatus = (AccountStatus)completionValue;
        AccountClientStub accounts = new AccountClientStub();
        AccountViewModel viewModel = new AccountViewModel(accounts, new ClientSession());

        await viewModel.RequestPasswordResetCode(EmailAddress);
        accounts.Operation = Task.FromResult(completionStatus);
        await viewModel.ResetPassword(VerificationCode, NewPassword);
        AccountStatus status = await viewModel.ResetPassword(VerificationCode, NewPassword);

        Assert.AreEqual(AccountStatus.MissingRequiredInformation, status);
    }

    [TestMethod]
    public async Task DeactivateAccount_ConfirmedSuccess_ClearsSession()
    {
        ClientSession session = CreateSession();
        AccountViewModel viewModel = new AccountViewModel(new AccountClientStub(), session);

        await viewModel.DeactivateAccount(CurrentPassword);

        Assert.IsNull(session.SessionToken);
    }

    [TestMethod]
    [DataRow((int)AccountStatus.ActiveMatchInProgress)]
    [DataRow((int)AccountStatus.IncorrectCurrentPassword)]
    [DataRow((int)AccountStatus.ServiceUnavailable)]
    public async Task DeactivateAccount_RejectedOrUnconfirmedOperation_PreservesSession(
        int rejectionValue)
    {
        AccountStatus rejection = (AccountStatus)rejectionValue;
        ClientSession session = CreateSession();
        AccountClientStub accounts = new AccountClientStub
        {
            Operation = Task.FromResult(rejection)
        };
        AccountViewModel viewModel = new AccountViewModel(accounts, session);
        string? originalToken = session.SessionToken;

        await viewModel.DeactivateAccount(CurrentPassword);

        Assert.AreEqual(originalToken, session.SessionToken);
    }

    [TestMethod]
    public async Task DeactivateAccount_SessionChangesDuringRequest_PreservesNewSession()
    {
        ClientSession session = CreateSession();
        TaskCompletionSource<AccountStatus> completion = new TaskCompletionSource<AccountStatus>();
        AccountClientStub accounts = new AccountClientStub { Operation = completion.Task };
        AccountViewModel viewModel = new AccountViewModel(accounts, session);

        Task<AccountStatus> deactivation = viewModel.DeactivateAccount(CurrentPassword);
        session.StartGuest(new GuestAccountSession(
            "different-session", "Guest", DateTime.MaxValue));
        completion.SetResult(AccountStatus.Success);
        await deactivation;

        Assert.AreEqual("different-session", session.SessionToken);
    }

    [TestMethod]
    public async Task DeactivateAccount_Guest_DoesNotCallServer()
    {
        ClientSession session = new ClientSession();
        session.StartGuest(new GuestAccountSession("guest-session", "Guest", DateTime.MaxValue));
        AccountClientStub accounts = new AccountClientStub();
        AccountViewModel viewModel = new AccountViewModel(accounts, session);

        await viewModel.DeactivateAccount(CurrentPassword);

        Assert.AreEqual(0, accounts.OperationCalls);
    }

    private static ClientSession CreateSession()
    {
        ClientSession session = new ClientSession();
        session.StartAuthenticated(new AuthenticatedAccountSession(
            "account-session", 41, "player", EmailAddress, "es-MX", DateTime.MaxValue));
        return session;
    }

    private sealed class AccountClientStub : IAccountClient
    {
        internal Task<AccountStatus> Operation { get; set; }
            = Task.FromResult(AccountStatus.Success);

        internal int OperationCalls { get; private set; }

        internal string ResetEmailAddress { get; private set; } = string.Empty;

        public Task<AccountStatus> RequestRegistrationCode(string username, string emailAddress)
        {
            throw new NotSupportedException();
        }

        public Task<AccountStatus> CompleteRegistration(NewAccount account)
        {
            throw new NotSupportedException();
        }

        public Task<AccountStatus> ChangePassword(
            string sessionToken, string currentPassword, string newPassword)
        {
            OperationCalls++;
            return Operation;
        }

        public Task<AccountStatus> RequestPasswordResetCode(string emailAddress)
        {
            OperationCalls++;
            return Operation;
        }

        public Task<AccountStatus> ResetPassword(
            string emailAddress, string verificationCode, string newPassword)
        {
            OperationCalls++;
            ResetEmailAddress = emailAddress;
            return Operation;
        }

        public Task<AccountStatus> DeactivateAccount(string sessionToken, string currentPassword)
        {
            OperationCalls++;
            return Operation;
        }
    }
}
