using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using Proxy = ReversiGameSystem.ServerProxy;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests;

internal sealed class ServerConnectionFactoryTests
{
    internal static Action[] CreateTests()
    {
        ServerConnectionFactoryTests suite = new ServerConnectionFactoryTests();
        return new Action[]
        {
            suite.GetServerHost_VariableAbsent_ReturnsLocalhost,
            suite.GetServerHost_EnvironmentVariableProvided_ReturnsConfiguredHost,
            suite.CreateEndpointAddress_ValidHost_RewritesAddressPreservingPortAndPath,
            suite.CreateMatchClients_CustomHost_PreservesConfiguredPortsAndPaths,
            suite.GetServerHost_WhitespaceOrInvalidHost_ThrowsInvalidOperationException
        };
    }

    internal void GetServerHost_VariableAbsent_ReturnsLocalhost()
    {
        // Arrange
        string? previousValue = Environment.GetEnvironmentVariable(
            ServerConnectionFactory.ServerHostVariable);
        try
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable,
                null);

            // Act
            string host = ServerConnectionFactory.GetServerHost();

            // Assert
            TestAssert.AreEqual(ServerConnectionFactory.DefaultHost, host);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable,
                previousValue);
        }
    }

    internal void GetServerHost_EnvironmentVariableProvided_ReturnsConfiguredHost()
    {
        // Arrange
        string? previousValue = Environment.GetEnvironmentVariable(
            ServerConnectionFactory.ServerHostVariable);
        string configuredHost = "192.168.137.1";
        try
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable,
                configuredHost);

            // Act
            string host = ServerConnectionFactory.GetServerHost();

            // Assert
            TestAssert.AreEqual(configuredHost, host);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable,
                previousValue);
        }
    }

    internal void CreateEndpointAddress_ValidHost_RewritesAddressPreservingPortAndPath()
    {
        // Arrange
        string? previousValue = Environment.GetEnvironmentVariable(
            ServerConnectionFactory.ServerHostVariable);
        string customHost = "192.168.1.50";
        try
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable,
                customHost);

            // Act
            EndpointAddress endpointAddress =
                ServerConnectionFactory.CreateEndpointAddress("SessionEndpoint");

            // Assert
            TestAssert.AreEqual(
                "net.tcp://192.168.1.50:8091/ReversiGameSystem/Session",
                endpointAddress.Uri.AbsoluteUri);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable,
                previousValue);
        }
    }

    internal void CreateMatchClients_CustomHost_PreservesConfiguredPortsAndPaths()
    {
        string? previousValue = Environment.GetEnvironmentVariable(
            ServerConnectionFactory.ServerHostVariable);
        string customHost = "10.0.0.5";
        try
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable,
                customHost);

            using MatchLobbyClient lobbyCallback = new MatchLobbyClient(
                "test-session", NullLogger<MatchLobbyClient>.Instance);
            using GameplayClient gameplayCallback = new GameplayClient(
                "test-session", NullLogger<GameplayClient>.Instance);
            using MatchChatClient chatCallback = new MatchChatClient(
                "test-session", NullLogger<MatchChatClient>.Instance);
            using Proxy.MatchLobbyServiceClient lobby = ServerConnectionFactory.CreateLobbyClient(
                new InstanceContext(lobbyCallback));
            using Proxy.GameplayMatchServiceClient gameplay =
                ServerConnectionFactory.CreateGameplayClient(new InstanceContext(gameplayCallback));
            using Proxy.MatchChatServiceClient chat = ServerConnectionFactory.CreateChatClient(
                new InstanceContext(chatCallback));
            TestAssert.AreEqual(
                "net.tcp://10.0.0.5:8095/ReversiGameSystem/MatchLobby",
                lobby.Endpoint.Address.Uri.AbsoluteUri);
            TestAssert.AreEqual(
                "net.tcp://10.0.0.5:8096/ReversiGameSystem/GameplayMatch",
                gameplay.Endpoint.Address.Uri.AbsoluteUri);
            TestAssert.AreEqual(
                "net.tcp://10.0.0.5:8097/ReversiGameSystem/MatchChat",
                chat.Endpoint.Address.Uri.AbsoluteUri);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable,
                previousValue);
        }
    }

    internal void GetServerHost_WhitespaceOrInvalidHost_ThrowsInvalidOperationException()
    {
        // Arrange
        string? previousValue = Environment.GetEnvironmentVariable(
            ServerConnectionFactory.ServerHostVariable);
        try
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable,
                "   ");

            // Act & Assert
            TestAssert.Throws<InvalidOperationException>(
                () => ServerConnectionFactory.GetServerHost());
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable,
                previousValue);
        }
    }
}
