using System;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class MatchCallbackSubscriptionsTests
{
    [TestMethod]
    public void Constructor_NullPredicate_ThrowsArgumentNullException()
    {
        Action action = () => _ = new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
            null!,
            NullLogger.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Action action = () => _ = new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
            (_, _) => true,
            null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Publish_NullAction_ThrowsArgumentNullException()
    {
        using MatchCallbackSubscriptions<IMatchLobbyServiceCallback> subscriptions =
            new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
                (_, _) => true,
                NullLogger.Instance);

        Action action = () => subscriptions.Publish(1L, null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void HasSubscription_WithoutRegisteredChannel_ReturnsFalse()
    {
        using MatchCallbackSubscriptions<IMatchLobbyServiceCallback> subscriptions =
            new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
                (_, _) => true,
                NullLogger.Instance);

        bool actualHasSubscription = subscriptions.HasSubscription("token-1", 10L);

        Assert.IsFalse(actualHasSubscription);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("unknown-token")]
    public void RemoveSession_MissingOrUnknownToken_DoesNotThrow(string? sessionToken)
    {
        using MatchCallbackSubscriptions<IMatchLobbyServiceCallback> subscriptions =
            new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
                (_, _) => true,
                NullLogger.Instance);

        subscriptions.RemoveSession(sessionToken!);
        bool actualHasSubscription = subscriptions.HasSubscription("token-1", 10L);

        Assert.IsFalse(actualHasSubscription);
    }

    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        MatchCallbackSubscriptions<IMatchLobbyServiceCallback> subscriptions =
            new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
                (_, _) => true,
                NullLogger.Instance);

        subscriptions.Dispose();
        subscriptions.Dispose();

        Assert.IsNotNull(subscriptions);
    }
}
