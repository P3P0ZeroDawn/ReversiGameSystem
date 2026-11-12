using System.Threading.Tasks;
using ReversiGameSystem.Presentation.AccountCreation;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests;

internal sealed class AccountCreationViewModelTests
{
    internal void CreateAccount_MissingInformation_DoesNotCallServer()
    {
        AccountClientStub accounts = new AccountClientStub();
        AccountCreationViewModel viewModel = CreateValidViewModel(accounts);
        viewModel.EmailAddress = string.Empty;

        AccountCreationStatus status = viewModel.CreateAccount().GetAwaiter().GetResult();

        TestAssert.AreEqual(AccountCreationStatus.MissingRequiredInformation, status);
        TestAssert.AreEqual(0, accounts.CallCount);
    }

    internal void CreateAccount_ValidInput_NormalizesAndCallsServer()
    {
        AccountClientStub accounts = new AccountClientStub();
        AccountCreationViewModel viewModel = CreateValidViewModel(accounts);
        viewModel.Username = "  Player123  ";
        viewModel.EmailAddress = "  player@example.com  ";

        AccountCreationStatus status = viewModel.CreateAccount().GetAwaiter().GetResult();

        TestAssert.AreEqual(AccountCreationStatus.Success, status);
        TestAssert.AreEqual(1, accounts.CallCount);
        TestAssert.AreEqual("Player123", accounts.Username);
        TestAssert.AreEqual("player@example.com", accounts.EmailAddress);
    }

    internal void CreateAccount_ConcurrentSubmission_ReturnsSubmissionInProgress()
    {
        AccountClientStub accounts = new AccountClientStub();
        accounts.DelayCompletion = true;
        AccountCreationViewModel viewModel = CreateValidViewModel(accounts);

        Task<AccountCreationStatus> firstSubmission = viewModel.CreateAccount();
        AccountCreationStatus secondStatus = viewModel.CreateAccount().GetAwaiter().GetResult();
        accounts.Complete(AccountCreationStatus.Success);
        AccountCreationStatus firstStatus = firstSubmission.GetAwaiter().GetResult();

        TestAssert.AreEqual(AccountCreationStatus.SubmissionInProgress, secondStatus);
        TestAssert.AreEqual(AccountCreationStatus.Success, firstStatus);
        TestAssert.AreEqual(1, accounts.CallCount);
    }

    private static AccountCreationViewModel CreateValidViewModel(IAccountClient accounts)
    {
        return new AccountCreationViewModel(accounts)
        {
            Username = "Player123",
            EmailAddress = "player@example.com",
            Password = "Aa12345!",
            PasswordConfirmation = "Aa12345!",
            LocaleCode = "es-MX"
        };
    }

    private sealed class AccountClientStub : IAccountClient
    {
        private readonly TaskCompletionSource<AccountCreationStatus> _completion =
            new TaskCompletionSource<AccountCreationStatus>();

        internal int CallCount { get; private set; }

        internal string Username { get; private set; } = string.Empty;

        internal string EmailAddress { get; private set; } = string.Empty;

        internal bool DelayCompletion { get; set; }

        public Task<AccountCreationStatus> CreateAccount(
            string username,
            string emailAddress,
            string password,
            string localeCode,
            byte[]? imageContent,
            string? imageMediaType)
        {
            CallCount++;
            Username = username;
            EmailAddress = emailAddress;
            Task<AccountCreationStatus> result = DelayCompletion
                ? _completion.Task
                : Task.FromResult(AccountCreationStatus.Success);
            return result;
        }

        internal void Complete(AccountCreationStatus status)
        {
            _completion.SetResult(status);
        }
    }
}
