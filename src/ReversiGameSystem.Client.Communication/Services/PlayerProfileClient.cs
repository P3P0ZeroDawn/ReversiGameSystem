using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class PlayerProfileClient
{
    private readonly string _sessionToken;
    private readonly ILogger<PlayerProfileClient> _logger;
    private readonly Func<Proxy.PlayerProfileServiceClient> _createClient;
    private readonly object _profileDataLock = new object();
    private readonly Dictionary<long, PlayerProfile> _profiles =
        new Dictionary<long, PlayerProfile>();
    private readonly Dictionary<long, PlayerImageContent> _images =
        new Dictionary<long, PlayerImageContent>();
    private PlayerProfile? _ownProfile;
    private PlayerImageContent? _ownImage;
    private bool _discarded;

    internal PlayerProfileClient(string sessionToken, ILogger<PlayerProfileClient> logger)
        : this(sessionToken, logger, ServerConnectionFactory.CreateProfileClient)
    {
    }

    internal PlayerProfileClient(string sessionToken, ILogger<PlayerProfileClient> logger,
        Func<Proxy.PlayerProfileServiceClient> createClient)
    {
        _sessionToken = sessionToken ?? throw new ArgumentNullException(nameof(sessionToken));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
    }

    internal Task<ProfileStatus> LoadProfile(long? targetAccountId = null)
    {
        async Task LoadAccountProfile(Proxy.PlayerProfileServiceClient client)
        {
            Proxy.PlayerProfile profile = await client.GetPlayerProfileAsync(
                _sessionToken, targetAccountId).ConfigureAwait(false);
            PlayerProfile confirmed = MapProfile(profile);
            if (targetAccountId.HasValue && confirmed.Account.AccountId != targetAccountId.Value)
            {
                throw new InvalidDataException("The service returned another account profile.");
            }
            lock (_profileDataLock)
            {
                if (!_discarded)
                {
                    if (targetAccountId.HasValue)
                    {
                        _profiles[targetAccountId.Value] = confirmed;
                    }
                    else
                    {
                        _ownProfile = confirmed;
                    }
                }
            }
        }
        return SendProfileOperation(LoadAccountProfile);
    }

    internal PlayerProfile ReadProfile(long? targetAccountId = null)
    {
        lock (_profileDataLock)
        {
            if (targetAccountId.HasValue && _profiles.ContainsKey(targetAccountId.Value))
            {
                return _profiles[targetAccountId.Value];
            }
            if (!targetAccountId.HasValue && _ownProfile != null)
            {
                return _ownProfile;
            }
            throw new InvalidOperationException("The requested profile has not been loaded.");
        }
    }

    internal Task<ProfileStatus> LoadProfileImage(long? targetAccountId = null)
    {
        async Task LoadAccountImage(Proxy.PlayerProfileServiceClient client)
        {
            Proxy.ImageContent? image = await client.GetProfileImageAsync(
                _sessionToken, targetAccountId).ConfigureAwait(false);
            if (image != null && (image.Content == null || image.Content.Length == 0))
            {
                throw new InvalidDataException("The service returned image content without bytes.");
            }
            PlayerImageContent confirmed = image == null
                ? new PlayerImageContent(Array.Empty<byte>(), string.Empty)
                : new PlayerImageContent(image.Content, image.MediaType);
            lock (_profileDataLock)
            {
                if (!_discarded)
                {
                    if (targetAccountId.HasValue)
                    {
                        _images[targetAccountId.Value] = confirmed;
                    }
                    else
                    {
                        _ownImage = confirmed;
                    }
                }
            }
        }
        return SendProfileOperation(LoadAccountImage);
    }

    internal PlayerImageContent ReadProfileImage(long? targetAccountId = null)
    {
        lock (_profileDataLock)
        {
            if (targetAccountId.HasValue && _images.ContainsKey(targetAccountId.Value))
            {
                return _images[targetAccountId.Value];
            }
            if (!targetAccountId.HasValue && _ownImage != null)
            {
                return _ownImage;
            }
            throw new InvalidOperationException("The requested profile image has not been loaded.");
        }
    }

    internal Task<ProfileStatus> ChangeUsername(string username)
    {
        async Task SaveUsername(Proxy.PlayerProfileServiceClient client)
        {
            await client.ChangeUsernameAsync(_sessionToken, username).ConfigureAwait(false);
            lock (_profileDataLock)
            {
                _ownProfile = null;
                _profiles.Clear();
            }
        }
        return SendProfileOperation(SaveUsername);
    }

    internal Task<ProfileStatus> UpdateProfileImage(PlayerImageContent image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }
        Proxy.ImageContent content = new Proxy.ImageContent
        {
            Content = image.Content,
            MediaType = image.MediaType
        };
        async Task SaveProfileImage(Proxy.PlayerProfileServiceClient client)
        {
            await client.UpdateProfileImageAsync(_sessionToken, content).ConfigureAwait(false);
            lock (_profileDataLock)
            {
                _ownProfile = null;
                _ownImage = null;
                _profiles.Clear();
                _images.Clear();
            }
        }
        return SendProfileOperation(SaveProfileImage);
    }

    internal void DiscardSession()
    {
        lock (_profileDataLock)
        {
            _discarded = true;
            _ownProfile = null;
            _ownImage = null;
            _profiles.Clear();
            _images.Clear();
        }
    }

    private async Task<ProfileStatus> SendProfileOperation(
        Func<Proxy.PlayerProfileServiceClient, Task> operation)
    {
        lock (_profileDataLock)
        {
            if (_discarded)
            {
                return ProfileStatus.InvalidSession;
            }
        }
        ProfileStatus status;
        Proxy.PlayerProfileServiceClient client = _createClient();
        try
        {
            await RemoteCall.SendServiceCall(client, () => operation(client), _logger)
                .ConfigureAwait(false);
            lock (_profileDataLock)
            {
                status = _discarded ? ProfileStatus.InvalidSession : ProfileStatus.Success;
            }
        }
        catch (FaultException<Proxy.ProfileFault> fault)
            when (Enum.IsDefined(typeof(Proxy.ProfileFailure), fault.Detail.Reason))
        {
            status = MapProfileStatus(fault.Detail.Reason);
        }
        catch (FaultException<Proxy.SessionFault> fault)
            when (fault.Detail.Reason is Proxy.SessionFailure.MissingSessionToken
                or Proxy.SessionFailure.InvalidSession)
        {
            status = ProfileStatus.InvalidSession;
        }
        catch (CommunicationException exception) when (RemoteCall.IsConnectionFailure(exception))
        {
            status = ProfileStatus.ServiceUnavailable;
        }
        catch (TimeoutException)
        {
            status = ProfileStatus.ServiceUnavailable;
        }
        return status;
    }

    private static PlayerProfile MapProfile(Proxy.PlayerProfile profile)
    {
        if (profile == null || profile.Account == null || profile.Statistics == null)
        {
            throw new InvalidDataException("The service returned incomplete profile information.");
        }
        Proxy.PlayerStatistics statistics = profile.Statistics;
        ProfileStatistics confirmed = new ProfileStatistics(statistics.MatchesPlayed,
            statistics.Wins, statistics.Losses, statistics.Draws, statistics.WinRate);
        PlayerAccount account = new PlayerAccount(profile.Account.AccountId,
            profile.Account.Username, profile.HasProfileImage);
        return new PlayerProfile(account, confirmed);
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
            Proxy.ProfileFailure.InactiveAccount => ProfileStatus.InactiveAccount,
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
        return status;
    }
}
