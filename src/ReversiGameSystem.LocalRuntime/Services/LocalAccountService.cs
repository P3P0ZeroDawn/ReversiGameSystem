using System;
using System.Threading.Tasks;
using ReversiGameSystem.Contracts.Accounts;
using ReversiGameSystem.Contracts.Services;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.LocalRuntime.Services;

public sealed class LocalAccountService : IAccountService
{
    private readonly ICreateAccountHandler createAccountHandler;
    private readonly ILoginHandler loginHandler;

    public LocalAccountService(
        ICreateAccountHandler createAccountHandler,
        ILoginHandler loginHandler)
    {
        this.createAccountHandler = createAccountHandler
            ?? throw new ArgumentNullException(nameof(createAccountHandler));
        this.loginHandler = loginHandler
            ?? throw new ArgumentNullException(nameof(loginHandler));
    }

    public async Task<CreateAccountResponse> CreateAccountAsync(CreateAccountRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ProfileImageData? profileImage = request.ProfileImage == null
            ? null
            : new ProfileImageData(
                (byte[])request.ProfileImage.Content.Clone(),
                request.ProfileImage.MediaType);

        CreateAccountResult result = await createAccountHandler.ExecuteAsync(
            new CreateAccountCommand(
                request.Username,
                request.EmailAddress,
                request.Password,
                request.PreferredLocaleCode,
                profileImage));

        return new CreateAccountResponse
        {
            ResultCode = Map(result.Outcome),
            AccountId = result.AccountId
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        LoginResult result = await loginHandler.ExecuteAsync(
            new LoginCommand(request.Login, request.Password));

        return new LoginResponse
        {
            ResultCode = Map(result.Outcome),
            Session = Map(result.Session)
        };
    }

    private static CreateAccountResultCode Map(CreateAccountOutcome outcome)
    {
        return outcome switch
        {
            CreateAccountOutcome.Success => CreateAccountResultCode.Success,
            CreateAccountOutcome.InvalidRequest => CreateAccountResultCode.InvalidRequest,
            CreateAccountOutcome.InvalidUsername => CreateAccountResultCode.InvalidUsername,
            CreateAccountOutcome.InvalidEmailAddress => CreateAccountResultCode.InvalidEmailAddress,
            CreateAccountOutcome.InvalidPassword => CreateAccountResultCode.InvalidPassword,
            CreateAccountOutcome.UsernameAlreadyExists => CreateAccountResultCode.UsernameAlreadyExists,
            CreateAccountOutcome.EmailAddressAlreadyExists => CreateAccountResultCode.EmailAddressAlreadyExists,
            CreateAccountOutcome.ServiceUnavailable => CreateAccountResultCode.ServiceUnavailable,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        };
    }

    private static LoginResultCode Map(LoginOutcome outcome)
    {
        return outcome switch
        {
            LoginOutcome.Success => LoginResultCode.Success,
            LoginOutcome.InvalidRequest => LoginResultCode.InvalidRequest,
            LoginOutcome.InvalidCredentials => LoginResultCode.InvalidCredentials,
            LoginOutcome.AccountInactive => LoginResultCode.AccountInactive,
            LoginOutcome.ServiceUnavailable => LoginResultCode.ServiceUnavailable,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        };
    }

    private static AuthenticatedSessionDto? Map(AuthenticatedSession? session)
    {
        return session == null
            ? null
            : new AuthenticatedSessionDto
            {
                SessionToken = session.SessionToken,
                AccountId = session.AccountId,
                Username = session.Username,
                EmailAddress = session.EmailAddress
            };
    }
}
