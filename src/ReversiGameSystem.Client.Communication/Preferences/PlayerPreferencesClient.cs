using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class PlayerPreferencesClient : IPlayerPreferencesClient
{
    private const string PreferencesUnavailableMessage = "No confirmed preferences are available.";
    private const string BackgroundNotLoadedMessage = "The board background has not been loaded.";
    private const string BackgroundUnavailableMessage =
        "No confirmed board background is available.";
    private const string MissingPreferencesResponseMessage =
        "The preferences service returned no preferences.";
    private const string MissingSavedBackgroundMessage =
        "The preferences service returned no background after saving it.";
    private readonly ILogger<PlayerPreferencesClient> _logger;
    private readonly Func<Proxy.PlayerPreferencesServiceClient> _createClient;
    private readonly object _preferencesDataLock = new object();
    private string? _sessionToken;
    private bool _discarded;
    private PlayerPreferences? _preferences;
    private bool _backgroundLoaded;
    private PlayerImageContent? _boardBackground;

    internal PlayerPreferencesClient(ILogger<PlayerPreferencesClient> logger,
        Func<Proxy.PlayerPreferencesServiceClient> createClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
    }

    public bool HasBoardBackground
    {
        get
        {
            lock (_preferencesDataLock)
            {
                if (_discarded || !_backgroundLoaded)
                {
                    throw new InvalidOperationException(BackgroundNotLoadedMessage);
                }
                return _boardBackground != null;
            }
        }
    }

    public Task<PreferencesStatus> LoadPreferences(string sessionToken)
    {
        async Task LoadConfirmedPreferences()
        {
            Proxy.PlayerPreferences preferences = await ReadStoredPreferences(sessionToken)
                .ConfigureAwait(false);
            StorePreferences(sessionToken, MapPreferences(preferences));
        }
        return SendPreferencesOperation(sessionToken, LoadConfirmedPreferences);
    }

    public PlayerPreferences ReadPreferences()
    {
        lock (_preferencesDataLock)
        {
            if (_discarded || _preferences == null)
            {
                throw new InvalidOperationException(PreferencesUnavailableMessage);
            }
            return CopyPreferences(_preferences);
        }
    }

    public Task<PreferencesStatus> UpdatePreferences(
        string sessionToken, PlayerPreferences preferences)
    {
        if (preferences == null)
        {
            throw new ArgumentNullException(nameof(preferences));
        }
        PlayerPreferences pendingPreferences = CopyPreferences(preferences);
        async Task UpdateConfirmedPreferences()
        {
            Proxy.PlayerPreferences request = new Proxy.PlayerPreferences
            {
                PreferredLocaleCode = pendingPreferences.PreferredLocaleCode,
                TimeZoneId = pendingPreferences.TimeZoneId,
                PieceStyleCode = pendingPreferences.PieceStyleCode,
                UseCustomBoardBackground = pendingPreferences.UseCustomBoardBackground
            };
            await WriteStoredPreferences(sessionToken, request).ConfigureAwait(false);
            StorePreferences(sessionToken, pendingPreferences);
        }
        return SendPreferencesOperation(sessionToken, UpdateConfirmedPreferences);
    }

    public Task<PreferencesStatus> LoadBoardBackground(string sessionToken)
    {
        async Task LoadConfirmedBackground()
        {
            Proxy.PlayerPreferencesServiceClient reader = _createClient();
            Proxy.ImageContent? image = await RemoteCall.SendServiceCall(reader,
                () => reader.GetBoardBackgroundAsync(sessionToken), _logger).ConfigureAwait(false);
            lock (_preferencesDataLock)
            {
                if (!_discarded && _sessionToken == sessionToken)
                {
                    _boardBackground = image == null ? null :
                        new PlayerImageContent((byte[])image.Content.Clone(), image.MediaType);
                    _backgroundLoaded = true;
                }
            }
        }
        return SendPreferencesOperation(sessionToken, LoadConfirmedBackground);
    }

    public PlayerImageContent ReadBoardBackground()
    {
        lock (_preferencesDataLock)
        {
            if (_discarded || !_backgroundLoaded || _boardBackground == null)
            {
                throw new InvalidOperationException(BackgroundUnavailableMessage);
            }
            return new PlayerImageContent((byte[])_boardBackground.Content.Clone(),
                _boardBackground.MediaType);
        }
    }

    public Task<PreferencesStatus> ChangeBoardBackground(
        string sessionToken, PlayerImageContent image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }
        Proxy.ImageContent request = new Proxy.ImageContent
        {
            Content = (byte[])image.Content.Clone(),
            MediaType = image.MediaType
        };
        async Task UpdateConfirmedBackground()
        {
            Proxy.PlayerPreferencesServiceClient writer = _createClient();
            await RemoteCall.SendServiceCall(writer,
                () => writer.ChangeBoardBackgroundAsync(sessionToken, request), _logger)
                .ConfigureAwait(false);
            if (!IsCurrentSession(sessionToken))
            {
                return;
            }
            Proxy.PlayerPreferencesServiceClient reader = _createClient();
            Proxy.ImageContent? storedImage = await RemoteCall.SendServiceCall(reader,
                () => reader.GetBoardBackgroundAsync(sessionToken), _logger).ConfigureAwait(false);
            if (storedImage == null)
            {
                throw new InvalidOperationException(MissingSavedBackgroundMessage);
            }
            lock (_preferencesDataLock)
            {
                if (!_discarded && _sessionToken == sessionToken)
                {
                    _boardBackground = new PlayerImageContent((byte[])storedImage.Content.Clone(),
                        storedImage.MediaType);
                    _backgroundLoaded = true;
                    if (_preferences != null)
                    {
                        _preferences.UseCustomBoardBackground = true;
                    }
                }
            }
        }
        return SendPreferencesOperation(sessionToken, UpdateConfirmedBackground);
    }

    public Task<PreferencesStatus> ChangeLanguage(string sessionToken, string localeCode)
    {
        async Task UpdateConfirmedLanguage()
        {
            Proxy.PlayerPreferences preferences = await ReadStoredPreferences(sessionToken)
                .ConfigureAwait(false);
            if (!IsCurrentSession(sessionToken))
            {
                return;
            }
            preferences.PreferredLocaleCode = localeCode;
            await WriteStoredPreferences(sessionToken, preferences).ConfigureAwait(false);
            StorePreferences(sessionToken, MapPreferences(preferences));
        }
        return SendPreferencesOperation(sessionToken, UpdateConfirmedLanguage);
    }

    public void DiscardSession()
    {
        lock (_preferencesDataLock)
        {
            _discarded = true;
            _sessionToken = null;
            _preferences = null;
            _boardBackground = null;
            _backgroundLoaded = false;
        }
    }

    private async Task<PreferencesStatus> SendPreferencesOperation(
        string sessionToken, Func<Task> operation)
    {
        if (!BindSession(sessionToken))
        {
            return PreferencesStatus.InvalidSession;
        }
        PreferencesStatus status;
        try
        {
            await operation().ConfigureAwait(false);
            status = PreferencesStatus.Success;
        }
        catch (FaultException<Proxy.PreferencesFault> fault)
            when (Enum.IsDefined(typeof(Proxy.PreferencesFailure), fault.Detail.Reason))
        {
            status = MapFailure(fault.Detail.Reason);
        }
        catch (FaultException<Proxy.SessionFault> fault)
            when (fault.Detail.Reason is Proxy.SessionFailure.InvalidSession
                or Proxy.SessionFailure.MissingSessionToken)
        {
            status = PreferencesStatus.InvalidSession;
        }
        catch (CommunicationException exception) when (exception is not FaultException)
        {
            status = PreferencesStatus.ServiceUnavailable;
        }
        catch (TimeoutException)
        {
            status = PreferencesStatus.ServiceUnavailable;
        }
        if (!IsCurrentSession(sessionToken))
        {
            status = PreferencesStatus.InvalidSession;
        }
        return status;
    }

    private bool BindSession(string sessionToken)
    {
        lock (_preferencesDataLock)
        {
            if (_discarded || string.IsNullOrWhiteSpace(sessionToken))
            {
                return false;
            }
            if (_sessionToken == null)
            {
                _sessionToken = sessionToken;
            }
            return _sessionToken == sessionToken;
        }
    }

    private bool IsCurrentSession(string sessionToken)
    {
        lock (_preferencesDataLock)
        {
            return !_discarded && _sessionToken == sessionToken;
        }
    }

    private void StorePreferences(string sessionToken, PlayerPreferences preferences)
    {
        lock (_preferencesDataLock)
        {
            if (!_discarded && _sessionToken == sessionToken)
            {
                _preferences = CopyPreferences(preferences);
            }
        }
    }

    private Task<Proxy.PlayerPreferences> ReadStoredPreferences(string sessionToken)
    {
        Proxy.PlayerPreferencesServiceClient reader = _createClient();
        return RemoteCall.SendServiceCall(reader,
            () => reader.GetPlayerPreferencesAsync(sessionToken), _logger);
    }

    private Task WriteStoredPreferences(string sessionToken, Proxy.PlayerPreferences preferences)
    {
        Proxy.PlayerPreferencesServiceClient writer = _createClient();
        return RemoteCall.SendServiceCall(writer,
            () => writer.UpdatePlayerPreferencesAsync(sessionToken, preferences), _logger);
    }

    private static PlayerPreferences MapPreferences(Proxy.PlayerPreferences preferences)
    {
        if (preferences == null)
        {
            throw new InvalidOperationException(MissingPreferencesResponseMessage);
        }
        return new PlayerPreferences
        {
            PreferredLocaleCode = preferences.PreferredLocaleCode,
            TimeZoneId = preferences.TimeZoneId,
            PieceStyleCode = preferences.PieceStyleCode,
            UseCustomBoardBackground = preferences.UseCustomBoardBackground
        };
    }

    private static PlayerPreferences CopyPreferences(PlayerPreferences preferences)
    {
        return new PlayerPreferences
        {
            PreferredLocaleCode = preferences.PreferredLocaleCode,
            TimeZoneId = preferences.TimeZoneId,
            PieceStyleCode = preferences.PieceStyleCode,
            UseCustomBoardBackground = preferences.UseCustomBoardBackground
        };
    }

    private static PreferencesStatus MapFailure(Proxy.PreferencesFailure failure)
    {
        PreferencesStatus status = failure switch
        {
            Proxy.PreferencesFailure.MissingPreferences => PreferencesStatus.MissingPreferences,
            Proxy.PreferencesFailure.InvalidLocale => PreferencesStatus.InvalidLocale,
            Proxy.PreferencesFailure.InvalidTimeZone => PreferencesStatus.InvalidTimeZone,
            Proxy.PreferencesFailure.InvalidPieceStyle => PreferencesStatus.InvalidPieceStyle,
            Proxy.PreferencesFailure.InvalidImage => PreferencesStatus.InvalidImage,
            Proxy.PreferencesFailure.InactiveAccount => PreferencesStatus.InactiveAccount,
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
        return status;
    }
}
