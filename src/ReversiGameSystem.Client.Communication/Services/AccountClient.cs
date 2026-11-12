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
            Proxy.AccountRegistrationOutcome outcome = await Execute(
                client => client.RegisterAccountAsync(credentials));
            status = MapStatus(outcome.Status);
        }
        catch (FaultException<Proxy.ServiceFault>)
        {
            status = AccountCreationStatus.PersistenceUnavailable;
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
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

    private static AccountCreationStatus MapStatus(Proxy.AccountRegistrationStatus status)
    {
        AccountCreationStatus mappedStatus;
        switch (status)
        {
            case Proxy.AccountRegistrationStatus.Success:
                mappedStatus = AccountCreationStatus.Success;
                break;
            case Proxy.AccountRegistrationStatus.UsernameUnavailable:
                mappedStatus = AccountCreationStatus.UsernameUnavailable;
                break;
            case Proxy.AccountRegistrationStatus.EmailUnavailable:
                mappedStatus = AccountCreationStatus.EmailUnavailable;
                break;
            case Proxy.AccountRegistrationStatus.InvalidUsername:
                mappedStatus = AccountCreationStatus.InvalidUsername;
                break;
            case Proxy.AccountRegistrationStatus.InvalidEmail:
                mappedStatus = AccountCreationStatus.InvalidEmailFormat;
                break;
            case Proxy.AccountRegistrationStatus.InvalidPassword:
                mappedStatus = AccountCreationStatus.PasswordUnsupportedCharacters;
                break;
            case Proxy.AccountRegistrationStatus.InvalidLocale:
                mappedStatus = AccountCreationStatus.InvalidLocale;
                break;
            case Proxy.AccountRegistrationStatus.InvalidProfileImage:
                mappedStatus = AccountCreationStatus.InvalidProfileImage;
                break;
            default:
                mappedStatus = AccountCreationStatus.PersistenceUnavailable;
                break;
        }
        return mappedStatus;
    }

    private Task<TResult> Execute<TResult>(
        Func<Proxy.AccountAccessServiceClient, Task<TResult>> operation)
    {
        Proxy.AccountAccessServiceClient client = _createClient();
        return RemoteCall.Execute(client, () => operation(client), _logger);
    }
}
