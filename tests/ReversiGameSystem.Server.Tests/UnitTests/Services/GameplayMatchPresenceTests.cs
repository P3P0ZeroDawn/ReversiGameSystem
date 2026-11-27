using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class GameplayMatchPresenceTests
{
    [TestMethod]
    public void RemoveConnection_ActiveSubscriptionRemains_ReturnsNull()
    {
        GameplayMatchPresence presence = new GameplayMatchPresence();

        KeyValuePair<int, bool>[]? actualSnapshot =
            presence.RemoveConnection("token-1", 100L, (_, _) => true);

        Assert.IsNull(actualSnapshot);
    }

    [TestMethod]
    public void RemoveConnection_UntrackedMatch_ReturnsNull()
    {
        GameplayMatchPresence presence = new GameplayMatchPresence();

        KeyValuePair<int, bool>[]? actualSnapshot =
            presence.RemoveConnection("token-1", 100L, (_, _) => false);

        Assert.IsNull(actualSnapshot);
    }

    [TestMethod]
    public void CollectExpiredReconnections_WithoutDisconnectedSeats_ReturnsEmpty()
    {
        GameplayMatchPresence presence = new GameplayMatchPresence();
        int expectedCount = 0;

        (long MatchId, int SeatNumber)[] actualExpired = presence.CollectExpiredReconnections();
        int actualCount = actualExpired.Length;

        Assert.AreEqual(expectedCount, actualCount);
    }
}
