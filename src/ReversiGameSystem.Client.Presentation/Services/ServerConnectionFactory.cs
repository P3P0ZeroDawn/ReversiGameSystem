using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using ReversiGameSystem.Configuration;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

/// <summary>
/// Creates client WCF service proxies and endpoints using host configuration
/// outside source control.
/// </summary>
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

    /// <summary>
    /// Resolves the configured server host or IP address.
    /// </summary>
    /// <returns>The validated host name or IP address.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configured host is whitespace or an invalid host name or IP address.
    /// </exception>
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

    /// <summary>
    /// Creates a dynamic EndpointAddress replacing the host in the specified URI pattern.
    /// </summary>
    /// <param name="endpointConfigurationName">The client endpoint configuration name.</param>
    /// <returns>The dynamic EndpointAddress pointing to the resolved host.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the endpoint configuration name is null, whitespace, or unknown.
    /// </exception>
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
            EndpointIdentity identity = EndpointIdentity.CreateDnsIdentity("ReversiGameServer");
            return new EndpointAddress(builder.Uri, identity);
        }

        if (KnownEndpoints.TryGetValue(
            endpointConfigurationName,
            out (int Port, string Path) details))
        {
            UriBuilder builder = new UriBuilder("net.tcp", host, details.Port, details.Path);
            EndpointIdentity identity = EndpointIdentity.CreateDnsIdentity("ReversiGameServer");
            return new EndpointAddress(builder.Uri, identity);
        }

        throw new ArgumentException(
            $"Endpoint configuration '{endpointConfigurationName}' is unknown.",
            nameof(endpointConfigurationName));
    }

    /// <summary>
    /// Creates the override endpoints for remote gameplay services.
    /// </summary>
    /// <returns>
    /// A MatchEndpoints instance with dynamic URIs for lobby, gameplay, and chat.
    /// </returns>
    internal static MatchEndpoints CreateMatchEndpoints()
    {
        return new MatchEndpoints
        {
            Lobby = CreateEndpointAddress("MatchLobbyEndpoint").Uri.AbsoluteUri,
            Gameplay = CreateEndpointAddress("GameplayMatchEndpoint").Uri.AbsoluteUri,
            Chat = CreateEndpointAddress("MatchChatEndpoint").Uri.AbsoluteUri
        };
    }

    /// <summary>
    /// Creates an account access service client with dynamic endpoint address.
    /// </summary>
    /// <returns>A new account access client proxy.</returns>
    internal static Proxy.AccountAccessServiceClient CreateAccountClient()
    {
        return new Proxy.AccountAccessServiceClient(
            "AccountAccessEndpoint",
            CreateEndpointAddress("AccountAccessEndpoint"));
    }

    /// <summary>
    /// Creates a session service client with dynamic endpoint address.
    /// </summary>
    /// <returns>A new session client proxy.</returns>
    internal static Proxy.SessionServiceClient CreateSessionClient()
    {
        return new Proxy.SessionServiceClient(
            "SessionEndpoint",
            CreateEndpointAddress("SessionEndpoint"));
    }

    /// <summary>
    /// Creates a player profile service client with dynamic endpoint address.
    /// </summary>
    /// <returns>A new player profile client proxy.</returns>
    internal static Proxy.PlayerProfileServiceClient CreateProfileClient()
    {
        return new Proxy.PlayerProfileServiceClient(
            "PlayerProfileEndpoint",
            CreateEndpointAddress("PlayerProfileEndpoint"));
    }

    /// <summary>
    /// Creates a leaderboard service client with dynamic endpoint address.
    /// </summary>
    /// <returns>A new leaderboard client proxy.</returns>
    internal static Proxy.LeaderboardServiceClient CreateLeaderboardClient()
    {
        return new Proxy.LeaderboardServiceClient(
            "LeaderboardEndpoint",
            CreateEndpointAddress("LeaderboardEndpoint"));
    }

    /// <summary>
    /// Creates a duplex friendship service client with dynamic endpoint address.
    /// </summary>
    /// <param name="callbackContext">The callback instance context.</param>
    /// <returns>A new duplex friendship client proxy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when callbackContext is null.</exception>
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

    /// <summary>
    /// Creates a duplex match lobby service client with dynamic endpoint address.
    /// </summary>
    /// <param name="callbackContext">The callback instance context.</param>
    /// <returns>A new duplex match lobby client proxy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when callbackContext is null.</exception>
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

    /// <summary>
    /// Creates a duplex gameplay match service client with dynamic endpoint address.
    /// </summary>
    /// <param name="callbackContext">The callback instance context.</param>
    /// <returns>A new duplex gameplay match client proxy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when callbackContext is null.</exception>
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

    /// <summary>
    /// Creates a duplex match chat service client with dynamic endpoint address.
    /// </summary>
    /// <param name="callbackContext">The callback instance context.</param>
    /// <returns>A new duplex match chat client proxy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when callbackContext is null.</exception>
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
