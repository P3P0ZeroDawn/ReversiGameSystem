using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Contracts.Faults;
using ContractEntry = ReversiGameSystem.Server.Contracts.Leaderboards.LeaderboardEntry;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.LeaderboardService;

[TestClass]
[TestCategory("Unit")]
public sealed class GetLeaderboardTests : LeaderboardServiceTestBase
{
    [TestMethod]
    public void GetLeaderboard_PlayerWithProfileImage_MapsContractEntryFields()
    {
        Repository.Players = new[]
        {
            new LeaderboardPlayer(42, "PlayerOne", "avatars/42.png", 10, 7)
        };

        ContractEntry[] entries = Service.GetLeaderboard(10);
        ContractEntry actualEntry = entries[0];

        Assert.AreEqual(1, actualEntry.Position);
        Assert.AreEqual(42L, actualEntry.Account.AccountId);
        Assert.AreEqual("PlayerOne", actualEntry.Account.Username);
        Assert.IsTrue(actualEntry.HasProfileImage);
        Assert.AreEqual(10, actualEntry.MatchesPlayed);
        Assert.AreEqual(7, actualEntry.Wins);
        Assert.AreEqual(0.7d, actualEntry.WinRate);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void GetLeaderboard_MissingProfileImage_SetsHasProfileImageFalse(
        string? profileImageUri)
    {
        Repository.Players = new[]
        {
            new LeaderboardPlayer(7, "NoAvatar", profileImageUri, 4, 2)
        };

        ContractEntry[] entries = Service.GetLeaderboard(10);

        Assert.IsFalse(entries[0].HasProfileImage);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(101)]
    public void GetLeaderboard_InvalidMaximum_ThrowsLeaderboardFault(int maximumEntries)
    {
        FaultException<LeaderboardFault> fault =
            Assert.Throws<FaultException<LeaderboardFault>>(
                () => Service.GetLeaderboard(maximumEntries));

        Assert.AreEqual(
            LeaderboardFailure.InvalidMaximumEntries,
            fault.Detail.Reason);
    }
}
