using System;
using System.ServiceModel;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests;

/// <summary>
/// Unit tests for dynamic server connection and host resolution.
/// </summary>
internal sealed class ServerConnectionFactoryTests
{
    /// <summary>
    /// Creates the individual test delegates for server connection factory tests.
    /// </summary>
    /// <returns>An array of test actions.</returns>
    internal static Action[] CreateTests()
    {
        ServerConnectionFactoryTests suite = new ServerConnectionFactoryTests();
        return new Action[]
        {
            suite.GetServerHost_VariableAbsent_ReturnsLocalhost,
            suite.GetServerHost_EnvironmentVariableProvided_ReturnsConfiguredHost,
            suite.CreateEndpointAddress_ValidHost_RewritesAddressPreservingPortAndPath,
            suite.CreateMatchEndpoints_CustomHost_OverridesAllThreeEndpoints,
            suite.GetServerHost_WhitespaceOrInvalidHost_ThrowsInvalidOperationException
        };
    }

    /// <summary>
    /// Verifies that GetServerHost falls back to localhost when the variable is absent.
    /// </summary>
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

    /// <summary>
    /// Verifies that GetServerHost returns the configured host when provided via environment.
    /// </summary>
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

    /// <summary>
    /// Verifies that CreateEndpointAddress rewrites the host while preserving port and path.
    /// </summary>
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

    /// <summary>
    /// Verifies that CreateMatchEndpoints returns overridden URIs for all three match endpoints.
    /// </summary>
    internal void CreateMatchEndpoints_CustomHost_OverridesAllThreeEndpoints()
    {
        // Arrange
        string? previousValue = Environment.GetEnvironmentVariable(
            ServerConnectionFactory.ServerHostVariable);
        string customHost = "10.0.0.5";
        try
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable,
                customHost);

            // Act
            MatchEndpoints endpoints =
                ServerConnectionFactory.CreateMatchEndpoints();

            // Assert
            TestAssert.AreEqual(
                "net.tcp://10.0.0.5:8095/ReversiGameSystem/MatchLobby",
                endpoints.Lobby);
            TestAssert.AreEqual(
                "net.tcp://10.0.0.5:8096/ReversiGameSystem/GameplayMatch",
                endpoints.Gameplay);
            TestAssert.AreEqual(
                "net.tcp://10.0.0.5:8097/ReversiGameSystem/MatchChat",
                endpoints.Chat);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable,
                previousValue);
        }
    }

    /// <summary>
    /// Verifies that GetServerHost throws InvalidOperationException when
    /// the configured host is invalid or whitespace.
    /// </summary>
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
