using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class AccountClient : IAccountClient
{
    private readonly ILogger<AccountClient> _logger;
    private readonly Func<Proxy.AccountAccessServiceClient> _createClient;

    internal AccountClient(ILogger<AccountClient> logger)
        : this(logger, ServerConnectionFactory.CreateAccountClient)
    {
    }

    internal AccountClient(
        ILogger<AccountClient> logger,
        Func<Proxy.AccountAccessServiceClient> createClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
    }

    public async Task<AccountCreationStatus> CreateAccount(
        string username,
        string emailAddress,
        string password,
        string localeCode,
        byte[]? imageContent,
        string? imageMediaType)
    {
        Proxy.AccountRegistrationCredentials credentials = new Proxy.AccountRegistrationCredentials
        {
            Username = username,
            EmailAddress = emailAddress,
            Password = password,
            PreferredLocaleCode = localeCode,
            ProfileImage = CreateProfileImage(imageContent, imageMediaType)
        };
        AccountCreationStatus status;
        try
        {
            Proxy.AccountRegistrationOutcome outcome = await SendAccountOperation(
                client => client.RegisterAccountAsync(credentials));
            status = MapRegistrationStatus(outcome.Status);
        }
        catch (Exception exception) when (RemoteCall.IsConnectionFailure(exception))
        {
            status = AccountCreationStatus.ServiceUnavailable;
        }
        return status;
    }

    private static Proxy.ProfileImageContent? CreateProfileImage(
        byte[]? content,
        string? mediaType)
    {
        Proxy.ProfileImageContent? image = null;
        if (content != null && mediaType != null)
        {
            image = new Proxy.ProfileImageContent
            {
                Content = content,
                MediaType = mediaType
            };
        }
        return image;
    }

    private static AccountCreationStatus MapRegistrationStatus(
        Proxy.AccountRegistrationStatus status)
    {
        AccountCreationStatus mappedStatus = status switch
        {
            Proxy.AccountRegistrationStatus.Success => AccountCreationStatus.Success,
            Proxy.AccountRegistrationStatus.UsernameUnavailable =>
                AccountCreationStatus.UsernameUnavailable,
            Proxy.AccountRegistrationStatus.EmailUnavailable =>
                AccountCreationStatus.EmailUnavailable,
            Proxy.AccountRegistrationStatus.InvalidUsername =>
                AccountCreationStatus.InvalidUsername,
            Proxy.AccountRegistrationStatus.InvalidEmail =>
                AccountCreationStatus.InvalidEmailFormat,
            Proxy.AccountRegistrationStatus.InvalidPassword =>
                AccountCreationStatus.PasswordUnsupportedCharacters,
            Proxy.AccountRegistrationStatus.InvalidLocale => AccountCreationStatus.InvalidLocale,
            Proxy.AccountRegistrationStatus.InvalidProfileImage =>
                AccountCreationStatus.InvalidProfileImage,
            _ => AccountCreationStatus.PersistenceUnavailable
        };
        return mappedStatus;
    }

    private Task<TResult> SendAccountOperation<TResult>(
        Func<Proxy.AccountAccessServiceClient, Task<TResult>> operation)
    {
        Proxy.AccountAccessServiceClient client = _createClient();
        return RemoteCall.SendServiceCall(client, () => operation(client), _logger);
    }
}
