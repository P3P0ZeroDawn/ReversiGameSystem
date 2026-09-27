using System;
using System.Threading.Tasks;
using ReversiGameSystem.Contracts.Accounts;
using ReversiGameSystem.Contracts.Services;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.LocalRuntime.Services;

/// <summary>
/// Represents local account service.
/// </summary>
public sealed class LocalAccountService : IAccountService
{
    private readonly ICreateAccountHandler _createAccountHandler;
    private readonly ILoginHandler _loginHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalAccountService"/> class.
    /// </summary>
    /// <param name="createAccountHandler">The create account handler.</param>
    /// <param name="loginHandler">The login handler.</param>
    public LocalAccountService(
        ICreateAccountHandler createAccountHandler,
        ILoginHandler loginHandler)
    {
        _createAccountHandler = createAccountHandler
            ?? throw new ArgumentNullException(nameof(createAccountHandler));
        _loginHandler = loginHandler
            ?? throw new ArgumentNullException(nameof(loginHandler));
    }

    /// <summary>
    /// Performs the create account async operation.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The result of the operation.</returns>
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

        CreateAccountResult result = await _createAccountHandler.ExecuteAsync(
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

    /// <summary>
    /// Performs the login async operation.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        LoginResult result = await _loginHandler.ExecuteAsync(
            new LoginCommand(request.Login, request.Password));

        LoginResponse response = new LoginResponse
        {
            ResultCode = Map(result.Outcome)
        };
        if (result.Session != null)
        {
            response.Session = Map(result.Session);
        }

        return response;
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
            CreateAccountOutcome.UsernameAlreadyExists =>
                CreateAccountResultCode.UsernameAlreadyExists,
            CreateAccountOutcome.EmailAddressAlreadyExists =>
                CreateAccountResultCode.EmailAddressAlreadyExists,
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

    private static AuthenticatedSessionDto Map(AuthenticatedSession session)
    {
        return new AuthenticatedSessionDto
        {
            SessionToken = session.SessionToken,
            AccountId = session.AccountId,
            Username = session.Username,
            EmailAddress = session.EmailAddress
        };
    }
}
