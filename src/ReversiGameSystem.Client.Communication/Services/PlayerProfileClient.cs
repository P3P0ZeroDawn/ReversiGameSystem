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
        string sessionToken,
        long? targetAccountId = null)
    {
        ProfileResult<PlayerProfile> result;
        try
        {
            Proxy.PlayerProfile profile = await SendProfileOperation(
                client => client.GetPlayerProfileAsync(sessionToken, targetAccountId));
            result = new ProfileResult<PlayerProfile>(ProfileStatus.Success, MapProfile(profile));
        }
        catch (FaultException<Proxy.ProfileFault> fault)
            when (Enum.IsDefined(typeof(Proxy.ProfileFailure), fault.Detail.Reason))
        {
            result = new ProfileResult<PlayerProfile>(MapProfileStatus(fault.Detail.Reason), null);
        }
        catch (Exception exception) when (RemoteCall.IsConnectionFailure(exception))
        {
            result = new ProfileResult<PlayerProfile>(ProfileStatus.ServiceUnavailable, null);
        }
        return result;
    }

    internal async Task<ProfileResult<PlayerImageContent>> GetProfileImage(
        string sessionToken,
        long? targetAccountId = null)
    {
        ProfileResult<PlayerImageContent> result;
        try
        {
            Proxy.ImageContent image = await SendProfileOperation(
                client => client.GetProfileImageAsync(sessionToken, targetAccountId));
            PlayerImageContent content = new PlayerImageContent(image.Content, image.MediaType);
            result = new ProfileResult<PlayerImageContent>(ProfileStatus.Success, content);
        }
        catch (FaultException<Proxy.ProfileFault> fault)
            when (Enum.IsDefined(typeof(Proxy.ProfileFailure), fault.Detail.Reason))
        {
            result = new ProfileResult<PlayerImageContent>(
                MapProfileStatus(fault.Detail.Reason), null);
        }
        catch (Exception exception) when (RemoteCall.IsConnectionFailure(exception))
        {
            result = new ProfileResult<PlayerImageContent>(ProfileStatus.ServiceUnavailable, null);
        }
        return result;
    }

    internal Task<ProfileStatus> ChangeUsername(string sessionToken, string username)
    {
        return ExecuteStatus(client => client.ChangeUsernameAsync(sessionToken, username));
    }

    internal Task<ProfileStatus> UpdateProfileImage(
        string sessionToken,
        byte[] content,
        string mediaType)
    {
        Proxy.ImageContent image = new Proxy.ImageContent
        {
            Content = content,
            MediaType = mediaType
        };
        return ExecuteStatus(client => client.UpdateProfileImageAsync(sessionToken, image));
    }

    private async Task<ProfileStatus> ExecuteStatus(
        Func<Proxy.PlayerProfileServiceClient, Task> operation)
    {
        ProfileStatus status;
        try
        {
            await SendProfileOperation(async client =>
            {
                await operation(client);
                return true;
            });
            status = ProfileStatus.Success;
        }
        catch (FaultException<Proxy.ProfileFault> fault)
            when (Enum.IsDefined(typeof(Proxy.ProfileFailure), fault.Detail.Reason))
        {
            status = MapProfileStatus(fault.Detail.Reason);
        }
        catch (Exception exception) when (RemoteCall.IsConnectionFailure(exception))
        {
            status = ProfileStatus.ServiceUnavailable;
        }
        return status;
    }

    private Task<TResult> SendProfileOperation<TResult>(
        Func<Proxy.PlayerProfileServiceClient, Task<TResult>> operation)
    {
        Proxy.PlayerProfileServiceClient client = _createClient();
        return RemoteCall.SendServiceCall(client, () => operation(client), _logger);
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

    private static ProfileStatus MapProfileStatus(Proxy.ProfileFailure failure)
    {
        ProfileStatus status = failure switch
        {
            Proxy.ProfileFailure.InvalidAccountId => ProfileStatus.InvalidAccountId,
            Proxy.ProfileFailure.AccountNotFound => ProfileStatus.AccountNotFound,
            Proxy.ProfileFailure.InvalidUsername => ProfileStatus.InvalidUsername,
            Proxy.ProfileFailure.UsernameUnavailable => ProfileStatus.UsernameUnavailable,
            Proxy.ProfileFailure.InvalidImage => ProfileStatus.InvalidImage,
            _ => ProfileStatus.InactiveAccount
        };
        return status;
    }
}
