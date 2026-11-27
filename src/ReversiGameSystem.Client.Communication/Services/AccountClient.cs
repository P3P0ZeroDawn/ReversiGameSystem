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
        return SendAccountOperation(RequestCode);
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
        return SendAccountOperation(RegisterAccount);
    }

    private async Task<AccountStatus> SendAccountOperation(
        Func<Proxy.AccountServiceClient, Task> operation)
    {
        AccountStatus status;
        try
        {
            Proxy.AccountServiceClient client = _createClient();
            await RemoteCall.SendServiceCall(client, () => operation(client), _logger)
                .ConfigureAwait(false);
            status = AccountStatus.Success;
        }
        catch (FaultException<Proxy.AccountFault> fault) when (CanMapFailure(fault.Detail.Reason))
        {
            status = MapFailure(fault.Detail.Reason);
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

    private static bool CanMapFailure(Proxy.AccountFailure failure)
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
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
        return status;
    }
}
