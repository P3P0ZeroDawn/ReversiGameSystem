using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class FriendshipSubscriptionsTests
{
    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Action action = () => _ = new FriendshipSubscriptions(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void PublishToAccounts_NullAccountIds_ThrowsArgumentNullException()
    {
        using FriendshipSubscriptions subscriptions =
            new FriendshipSubscriptions(NullLogger<FriendshipSubscriptions>.Instance);
        Action<IFriendshipServiceCallback> send = _ => { };

        Action action = () => subscriptions.PublishToAccounts(null!, send);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void PublishToAccounts_NullSendDelegate_ThrowsArgumentNullException()
    {
        using FriendshipSubscriptions subscriptions =
            new FriendshipSubscriptions(NullLogger<FriendshipSubscriptions>.Instance);
        HashSet<long> accountIds = new HashSet<long> { 1L };

        Action action = () => subscriptions.PublishToAccounts(accountIds, null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-3)]
    public void SweepChannels_NonPositiveMaximumChannels_ThrowsArgumentOutOfRangeException(
        int maximumChannels)
    {
        using FriendshipSubscriptions subscriptions =
            new FriendshipSubscriptions(NullLogger<FriendshipSubscriptions>.Instance);

        Action action = () => subscriptions.SweepChannels(maximumChannels, _ => true);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [TestMethod]
    public void SweepChannels_NullPredicate_ThrowsArgumentNullException()
    {
        using FriendshipSubscriptions subscriptions =
            new FriendshipSubscriptions(NullLogger<FriendshipSubscriptions>.Instance);

        Action action = () => subscriptions.SweepChannels(10, null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void SweepChannels_EmptySubscriptions_DoesNotInvokePredicate()
    {
        using FriendshipSubscriptions subscriptions =
            new FriendshipSubscriptions(NullLogger<FriendshipSubscriptions>.Instance);
        int expectedCalls = 0;
        int actualCalls = 0;

        subscriptions.SweepChannels(10, _ =>
        {
            actualCalls++;
            return true;
        });

        Assert.AreEqual(expectedCalls, actualCalls);
    }
}
