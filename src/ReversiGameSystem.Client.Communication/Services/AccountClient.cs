using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class AccountClient : IAccountClient
{
    private readonly ILogger<AccountClient> _logger;
    private readonly Func<Proxy.AccountServiceClient> _createClient;

    internal AccountClient(ILogger<AccountClient> logger)
        : this(logger, ServerConnectionFactory.CreateAccountClient)
    {
    }

    internal AccountClient(ILogger<AccountClient> logger,
        Func<Proxy.AccountServiceClient> createClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
    }

    public Task<AccountStatus> RequestRegistrationCode(string username, string emailAddress)
    {
        async Task RequestCode(Proxy.AccountServiceClient client)
        {
            await client.RequestRegistrationCodeAsync(username, emailAddress).ConfigureAwait(false);
        }
        return SendAccountOperation(RequestCode, CanMapRegistrationFailure);
    }

    public Task<AccountStatus> CompleteRegistration(NewAccount account)
    {
        if (account == null)
        {
            throw new ArgumentNullException(nameof(account));
        }
        Proxy.NewAccount registration = new Proxy.NewAccount
        {
            VerificationCode = account.VerificationCode,
            Username = account.Username,
            EmailAddress = account.EmailAddress,
            Password = account.Password,
            PreferredLocaleCode = account.PreferredLocaleCode,
            ProfileImage = account.ProfileImage == null ? null : new Proxy.ImageContent
            {
                Content = account.ProfileImage.Content,
                MediaType = account.ProfileImage.MediaType
            }
        };
        async Task RegisterAccount(Proxy.AccountServiceClient client)
        {
            Proxy.AccountReference created = await client.CompleteRegistrationAsync(registration)
                .ConfigureAwait(false);
            if (created == null || created.AccountId <= 0)
            {
                throw new InvalidOperationException("The service returned no registered account.");
            }
        }
        return SendAccountOperation(RegisterAccount, CanMapRegistrationFailure);
    }

    public Task<AccountStatus> ChangePassword(
        string sessionToken, string currentPassword, string newPassword)
    {
        async Task ChangeAccountPassword(Proxy.AccountServiceClient client)
        {
            await client.ChangePasswordAsync(sessionToken, currentPassword, newPassword)
                .ConfigureAwait(false);
        }
        return SendAccountOperation(ChangeAccountPassword,
            failure => failure is Proxy.AccountFailure.InvalidPassword
                or Proxy.AccountFailure.IncorrectCurrentPassword
                or Proxy.AccountFailure.InactiveAccount,
            usesSession: true);
    }

    public Task<AccountStatus> RequestPasswordResetCode(string emailAddress)
    {
        async Task RequestResetCode(Proxy.AccountServiceClient client)
        {
            await client.RequestPasswordResetCodeAsync(emailAddress).ConfigureAwait(false);
        }
        return SendAccountOperation(RequestResetCode,
            failure => failure == Proxy.AccountFailure.InvalidEmailAddress);
    }

    public Task<AccountStatus> ResetPassword(
        string emailAddress, string verificationCode, string newPassword)
    {
        async Task ResetAccountPassword(Proxy.AccountServiceClient client)
        {
            await client.ResetPasswordAsync(emailAddress, verificationCode, newPassword)
                .ConfigureAwait(false);
        }
        return SendAccountOperation(ResetAccountPassword,
            failure => failure is Proxy.AccountFailure.InvalidEmailAddress
                or Proxy.AccountFailure.InvalidVerificationCode
                or Proxy.AccountFailure.ExpiredVerificationCode
                or Proxy.AccountFailure.InvalidPassword);
    }

    public Task<AccountStatus> DeactivateAccount(string sessionToken, string currentPassword)
    {
        async Task DeactivateOwnAccount(Proxy.AccountServiceClient client)
        {
            await client.DeactivateAccountAsync(sessionToken, currentPassword)
                .ConfigureAwait(false);
        }
        return SendAccountOperation(DeactivateOwnAccount,
            failure => failure is Proxy.AccountFailure.IncorrectCurrentPassword
                or Proxy.AccountFailure.InactiveAccount
                or Proxy.AccountFailure.ActiveMatchInProgress,
            usesSession: true);
    }

    private async Task<AccountStatus> SendAccountOperation(
        Func<Proxy.AccountServiceClient, Task> operation,
        Func<Proxy.AccountFailure, bool> canMapFailure,
        bool usesSession = false)
    {
        AccountStatus status;
        try
        {
            Proxy.AccountServiceClient client = _createClient();
            await RemoteCall.SendServiceCall(client, () => operation(client), _logger)
                .ConfigureAwait(false);
            status = AccountStatus.Success;
        }
        catch (FaultException<Proxy.AccountFault> fault) when (canMapFailure(fault.Detail.Reason))
        {
            status = MapFailure(fault.Detail.Reason);
        }
        catch (FaultException<Proxy.SessionFault> fault) when (
            usesSession && fault.Detail.Reason is Proxy.SessionFailure.MissingSessionToken
                or Proxy.SessionFailure.InvalidSession or Proxy.SessionFailure.InactiveAccount)
        {
            status = fault.Detail.Reason switch
            {
                Proxy.SessionFailure.MissingSessionToken => AccountStatus.MissingSessionToken,
                Proxy.SessionFailure.InvalidSession => AccountStatus.InvalidSession,
                Proxy.SessionFailure.InactiveAccount => AccountStatus.InactiveAccount,
                _ => throw new ArgumentOutOfRangeException(nameof(fault))
            };
        }
        catch (CommunicationException exception) when (exception is not FaultException)
        {
            status = AccountStatus.ServiceUnavailable;
        }
        catch (TimeoutException)
        {
            status = AccountStatus.ServiceUnavailable;
        }
        return status;
    }

    private static bool CanMapRegistrationFailure(Proxy.AccountFailure failure)
    {
        return failure is Proxy.AccountFailure.MissingRegistrationData
            or Proxy.AccountFailure.InvalidUsername or Proxy.AccountFailure.InvalidEmailAddress
            or Proxy.AccountFailure.InvalidPassword or Proxy.AccountFailure.InvalidLocale
            or Proxy.AccountFailure.InvalidImage or Proxy.AccountFailure.UsernameUnavailable
            or Proxy.AccountFailure.EmailUnavailable or Proxy.AccountFailure.InvalidVerificationCode
            or Proxy.AccountFailure.ExpiredVerificationCode;
    }

    private static AccountStatus MapFailure(Proxy.AccountFailure failure)
    {
        AccountStatus status = failure switch
        {
            Proxy.AccountFailure.MissingRegistrationData =>
                AccountStatus.MissingRequiredInformation,
            Proxy.AccountFailure.InvalidUsername => AccountStatus.InvalidUsername,
            Proxy.AccountFailure.InvalidEmailAddress => AccountStatus.InvalidEmailFormat,
            Proxy.AccountFailure.InvalidPassword => AccountStatus.InvalidPassword,
            Proxy.AccountFailure.InvalidLocale => AccountStatus.InvalidLocale,
            Proxy.AccountFailure.InvalidImage => AccountStatus.InvalidProfileImage,
            Proxy.AccountFailure.UsernameUnavailable => AccountStatus.UsernameUnavailable,
            Proxy.AccountFailure.EmailUnavailable => AccountStatus.EmailUnavailable,
            Proxy.AccountFailure.InvalidVerificationCode => AccountStatus.InvalidVerificationCode,
            Proxy.AccountFailure.ExpiredVerificationCode => AccountStatus.ExpiredVerificationCode,
            Proxy.AccountFailure.IncorrectCurrentPassword => AccountStatus.IncorrectCurrentPassword,
            Proxy.AccountFailure.InactiveAccount => AccountStatus.InactiveAccount,
            Proxy.AccountFailure.ActiveMatchInProgress => AccountStatus.ActiveMatchInProgress,
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
        return status;
    }
}
