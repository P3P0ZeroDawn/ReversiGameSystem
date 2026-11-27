using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using ReversiGameSystem.Configuration;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal static class ServerConnectionFactory
{
    internal const string ServerHostVariable = "REVERSI_SERVER_HOST";
    internal const string DefaultHost = "localhost";

    private const int PreferencesPort = 8098;
    private const int PreferencesRequestTimeoutSeconds = 5;
    private const long MaximumPreferencesMessageBytes = 6291456;
    private const string PreferencesEndpoint = "PlayerPreferencesEndpoint";
    private const string PreferencesPath = "/ReversiGameSystem/PlayerPreferences";

    private static readonly IReadOnlyDictionary<string, (int Port, string Path)> KnownEndpoints =
        new Dictionary<string, (int Port, string Path)>(StringComparer.OrdinalIgnoreCase)
        {
            ["AccountEndpoint"] = (8090, "/ReversiGameSystem/Account"),
            ["SessionEndpoint"] = (8091, "/ReversiGameSystem/Session"),
            ["PlayerProfileEndpoint"] = (8092, "/ReversiGameSystem/PlayerProfile"),
            ["LeaderboardEndpoint"] = (8093, "/ReversiGameSystem/Leaderboard"),
            ["FriendshipEndpoint"] = (8094, "/ReversiGameSystem/Friendship"),
            ["MatchLobbyEndpoint"] = (8095, "/ReversiGameSystem/MatchLobby"),
            ["GameplayMatchEndpoint"] = (8096, "/ReversiGameSystem/GameplayMatch"),
            ["MatchChatEndpoint"] = (8097, "/ReversiGameSystem/MatchChat"),
            [PreferencesEndpoint] = (PreferencesPort, PreferencesPath)
        };

    internal static string GetServerHost()
    {
        string host = ClientEnvironmentSettings.ReadOptional(ServerHostVariable, DefaultHost);
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException(
                $"The {ServerHostVariable} variable cannot be empty or whitespace.");
        }

        string trimmedHost = host.Trim();
        UriHostNameType hostType = Uri.CheckHostName(trimmedHost);
        if (hostType == UriHostNameType.Unknown || hostType == UriHostNameType.Basic)
        {
            throw new InvalidOperationException(
                $"The configured {ServerHostVariable} value '{host}' "
                + "is not a valid host name or IP address.");
        }

        return trimmedHost;
    }

    internal static EndpointAddress CreateEndpointAddress(string endpointConfigurationName)
    {
        if (string.IsNullOrWhiteSpace(endpointConfigurationName))
        {
            throw new ArgumentException(
                "Endpoint configuration name cannot be null or whitespace.",
                nameof(endpointConfigurationName));
        }

        string host = GetServerHost();
        ClientSection? clientSection =
            ConfigurationManager.GetSection("system.serviceModel/client") as ClientSection;
        if (clientSection != null)
        {
            foreach (ChannelEndpointElement endpoint in clientSection.Endpoints)
            {
                if (string.Equals(endpoint.Name, endpointConfigurationName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    UriBuilder builder = new UriBuilder(endpoint.Address)
                    {
                        Host = host
                    };
                    return new EndpointAddress(builder.Uri);
                }
            }
        }

        if (KnownEndpoints.TryGetValue(
            endpointConfigurationName,
            out (int Port, string Path) details))
        {
            UriBuilder builder = new UriBuilder("net.tcp", host, details.Port, details.Path);
            return new EndpointAddress(builder.Uri);
        }

        throw new ArgumentException(
            $"Endpoint configuration '{endpointConfigurationName}' is unknown.",
            nameof(endpointConfigurationName));
    }

    internal static Proxy.AccountServiceClient CreateAccountClient()
    {
        return new Proxy.AccountServiceClient(
            "AccountEndpoint",
            CreateEndpointAddress("AccountEndpoint"));
    }

    internal static Proxy.SessionServiceClient CreateSessionClient()
    {
        return new Proxy.SessionServiceClient(
            "SessionEndpoint",
            CreateEndpointAddress("SessionEndpoint"));
    }

    internal static Proxy.PlayerProfileServiceClient CreateProfileClient()
    {
        return new Proxy.PlayerProfileServiceClient(
            "PlayerProfileEndpoint",
            CreateEndpointAddress("PlayerProfileEndpoint"));
    }

    internal static Proxy.PlayerPreferencesServiceClient CreatePreferencesClient()
    {
        NetTcpBinding binding = new NetTcpBinding(SecurityMode.None)
        {
            OpenTimeout = TimeSpan.FromSeconds(PreferencesRequestTimeoutSeconds),
            SendTimeout = TimeSpan.FromSeconds(PreferencesRequestTimeoutSeconds),
            MaxReceivedMessageSize = MaximumPreferencesMessageBytes
        };
        binding.ReaderQuotas.MaxArrayLength = (int)MaximumPreferencesMessageBytes;
        binding.ReaderQuotas.MaxStringContentLength = (int)MaximumPreferencesMessageBytes;
        return new Proxy.PlayerPreferencesServiceClient(binding,
            CreateEndpointAddress(PreferencesEndpoint));
    }

    internal static Proxy.LeaderboardServiceClient CreateLeaderboardClient()
    {
        return new Proxy.LeaderboardServiceClient(
            "LeaderboardEndpoint",
            CreateEndpointAddress("LeaderboardEndpoint"));
    }

    internal static Proxy.FriendshipServiceClient CreateFriendshipClient(
        InstanceContext callbackContext)
    {
        if (callbackContext == null)
        {
            throw new ArgumentNullException(nameof(callbackContext));
        }

        return new Proxy.FriendshipServiceClient(
            callbackContext,
            "FriendshipEndpoint",
            CreateEndpointAddress("FriendshipEndpoint"));
    }

    internal static Proxy.MatchLobbyServiceClient CreateLobbyClient(
        InstanceContext callbackContext)
    {
        if (callbackContext == null)
        {
            throw new ArgumentNullException(nameof(callbackContext));
        }

        return new Proxy.MatchLobbyServiceClient(
            callbackContext,
            "MatchLobbyEndpoint",
            CreateEndpointAddress("MatchLobbyEndpoint"));
    }

    internal static Proxy.GameplayMatchServiceClient CreateGameplayClient(
        InstanceContext callbackContext)
    {
        if (callbackContext == null)
        {
            throw new ArgumentNullException(nameof(callbackContext));
        }

        return new Proxy.GameplayMatchServiceClient(
            callbackContext,
            "GameplayMatchEndpoint",
            CreateEndpointAddress("GameplayMatchEndpoint"));
    }

    internal static Proxy.MatchChatServiceClient CreateChatClient(
        InstanceContext callbackContext)
    {
        if (callbackContext == null)
        {
            throw new ArgumentNullException(nameof(callbackContext));
        }

        return new Proxy.MatchChatServiceClient(
            callbackContext,
            "MatchChatEndpoint",
            CreateEndpointAddress("MatchChatEndpoint"));
    }

}
