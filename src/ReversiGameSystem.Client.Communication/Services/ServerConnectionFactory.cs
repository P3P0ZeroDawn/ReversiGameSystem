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

    private static readonly IReadOnlyDictionary<string, (int Port, string Path)> KnownEndpoints =
        new Dictionary<string, (int Port, string Path)>(StringComparer.OrdinalIgnoreCase)
        {
            ["AccountAccessEndpoint"] = (8090, "/ReversiGameSystem/AccountAccess"),
            ["SessionEndpoint"] = (8091, "/ReversiGameSystem/Session"),
            ["PlayerProfileEndpoint"] = (8092, "/ReversiGameSystem/PlayerProfile"),
            ["LeaderboardEndpoint"] = (8093, "/ReversiGameSystem/Leaderboard"),
            ["FriendshipEndpoint"] = (8094, "/ReversiGameSystem/Friendship"),
            ["MatchLobbyEndpoint"] = (8095, "/ReversiGameSystem/MatchLobby"),
            ["GameplayMatchEndpoint"] = (8096, "/ReversiGameSystem/GameplayMatch"),
            ["MatchChatEndpoint"] = (8097, "/ReversiGameSystem/MatchChat")
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
        Uri? baseUri = ResolveConfiguredUri(endpointConfigurationName);
        if (baseUri != null)
        {
            UriBuilder builder = new UriBuilder(baseUri)
            {
                Host = host
            };
            return new EndpointAddress(builder.Uri);
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

    internal static Proxy.AccountAccessServiceClient CreateAccountClient()
    {
        return new Proxy.AccountAccessServiceClient(
            "AccountAccessEndpoint",
            CreateEndpointAddress("AccountAccessEndpoint"));
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

    private static Uri? ResolveConfiguredUri(string endpointConfigurationName)
    {
        try
        {
            ClientSection? clientSection =
                ConfigurationManager.GetSection("system.serviceModel/client") as ClientSection;
            if (clientSection != null)
            {
                foreach (ChannelEndpointElement endpoint in clientSection.Endpoints)
                {
                    if (string.Equals(
                        endpoint.Name,
                        endpointConfigurationName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return endpoint.Address;
                    }
                }
            }
        }
        catch (ConfigurationException)
        {
            // Fall back to known endpoints mapping if configuration section cannot be loaded.
        }

        return null;
    }
}
