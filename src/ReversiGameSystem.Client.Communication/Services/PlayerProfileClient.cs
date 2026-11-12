using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class PlayerProfileClient
{
    private readonly ILogger<PlayerProfileClient> _logger;
    private readonly Func<Proxy.PlayerProfileServiceClient> _createClient;

    internal PlayerProfileClient(ILogger<PlayerProfileClient> logger)
        : this(logger, ServerConnectionFactory.CreateProfileClient)
    {
    }

    internal PlayerProfileClient(
        ILogger<PlayerProfileClient> logger,
        Func<Proxy.PlayerProfileServiceClient> createClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
    }

    internal async Task<ProfileResult<PlayerProfile>> GetPlayerProfile(
        string token,
        long? targetAccountId = null)
    {
        ProfileResult<PlayerProfile> result;
        try
        {
            Proxy.PlayerProfile profile = await Execute(
                client => client.GetPlayerProfileAsync(token, targetAccountId));
            result = new ProfileResult<PlayerProfile>(ProfileStatus.Success, MapProfile(profile));
        }
        catch (FaultException<Proxy.ProfileFault> fault)
        {
            result = new ProfileResult<PlayerProfile>(MapStatus(fault.Detail.Reason), null);
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            result = new ProfileResult<PlayerProfile>(ProfileStatus.ServiceUnavailable, null);
        }
        return result;
    }

    internal async Task<ProfileResult<ProfileImageContent>> GetProfileImage(
        string token,
        long? targetAccountId = null)
    {
        ProfileResult<ProfileImageContent> result;
        try
        {
            Proxy.ImageContent image = await Execute(
                client => client.GetProfileImageAsync(token, targetAccountId));
            ProfileImageContent content = new ProfileImageContent(image.Content, image.MediaType);
            result = new ProfileResult<ProfileImageContent>(ProfileStatus.Success, content);
        }
        catch (FaultException<Proxy.ProfileFault> fault)
        {
            result = new ProfileResult<ProfileImageContent>(MapStatus(fault.Detail.Reason), null);
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            result = new ProfileResult<ProfileImageContent>(ProfileStatus.ServiceUnavailable, null);
        }
        return result;
    }

    internal Task<ProfileStatus> ChangeUsername(string token, string username)
    {
        return ExecuteStatus(client => client.ChangeUsernameAsync(token, username));
    }

    internal Task<ProfileStatus> UpdateProfileImage(
        string token,
        byte[] content,
        string mediaType)
    {
        Proxy.ImageContent image = new Proxy.ImageContent
        {
            Content = content,
            MediaType = mediaType
        };
        return ExecuteStatus(client => client.UpdateProfileImageAsync(token, image));
    }

    private async Task<ProfileStatus> ExecuteStatus(
        Func<Proxy.PlayerProfileServiceClient, Task> operation)
    {
        ProfileStatus status;
        try
        {
            await Execute(async client =>
            {
                await operation(client);
                return true;
            });
            status = ProfileStatus.Success;
        }
        catch (FaultException<Proxy.ProfileFault> fault)
        {
            status = MapStatus(fault.Detail.Reason);
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            status = ProfileStatus.ServiceUnavailable;
        }
        return status;
    }

    private Task<TResult> Execute<TResult>(
        Func<Proxy.PlayerProfileServiceClient, Task<TResult>> operation)
    {
        Proxy.PlayerProfileServiceClient client = _createClient();
        return RemoteCall.Execute(client, () => operation(client), _logger);
    }

    private static PlayerProfile MapProfile(Proxy.PlayerProfile profile)
    {
        Proxy.PlayerStatistics statistics = profile.Statistics;
        ProfileStatistics mappedStatistics = new ProfileStatistics(
            statistics.MatchesPlayed,
            statistics.Wins,
            statistics.Losses,
            statistics.Draws,
            statistics.WinRate);
        return new PlayerProfile(
            profile.Account.Username,
            profile.HasProfileImage,
            mappedStatistics);
    }

    private static ProfileStatus MapStatus(Proxy.ProfileFailure failure)
    {
        ProfileStatus status;
        switch (failure)
        {
            case Proxy.ProfileFailure.InvalidAccountId:
                status = ProfileStatus.InvalidAccountId;
                break;
            case Proxy.ProfileFailure.AccountNotFound:
                status = ProfileStatus.AccountNotFound;
                break;
            case Proxy.ProfileFailure.InvalidUsername:
                status = ProfileStatus.InvalidUsername;
                break;
            case Proxy.ProfileFailure.UsernameUnavailable:
                status = ProfileStatus.UsernameUnavailable;
                break;
            case Proxy.ProfileFailure.InvalidImage:
                status = ProfileStatus.InvalidImage;
                break;
            default:
                status = ProfileStatus.InactiveAccount;
                break;
        }
        return status;
    }
}
