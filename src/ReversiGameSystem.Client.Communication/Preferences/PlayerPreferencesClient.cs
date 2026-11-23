using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class PlayerPreferencesClient
{
    private readonly ILogger<PlayerPreferencesClient> _logger;
    private readonly Func<Proxy.PlayerPreferencesServiceClient> _createClient;

    internal PlayerPreferencesClient(ILogger<PlayerPreferencesClient> logger,
        Func<Proxy.PlayerPreferencesServiceClient> createClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
    }

    internal async Task<PreferencesStatus> ChangeLanguage(string token, string localeCode)
    {
        PreferencesStatus status;
        try
        {
            Proxy.PlayerPreferencesServiceClient reader = _createClient();
            Proxy.PlayerPreferences preferences = await RemoteCall.Execute(reader,
                () => reader.GetPlayerPreferencesAsync(token), _logger).ConfigureAwait(false);
            preferences.PreferredLocaleCode = localeCode;
            Proxy.PlayerPreferencesServiceClient writer = _createClient();
            await RemoteCall.Execute(writer,
                () => writer.UpdatePlayerPreferencesAsync(token, preferences), _logger)
                .ConfigureAwait(false);
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
        return status;
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
