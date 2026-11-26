using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Contracts.Faults;
using ContractEntry = ReversiGameSystem.Server.Contracts.Leaderboards.LeaderboardEntry;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.LeaderboardServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class GetLeaderboardTests : LeaderboardServiceTestBase
{
    [TestMethod]
    public void GetLeaderboard_PlayerWithProfileImage_MapsContractEntryFields()
    {
        LeaderboardPlayer player = new LeaderboardPlayer(
            42,
            "PlayerOne",
            "avatars/42.png",
            10,
            7);
        Repository.Players = new[] { player };
        int expectedPosition = 1;
        bool expectedHasProfileImage = true;
        double expectedWinRate = 0.7d;

        ContractEntry[] entries = Service.GetLeaderboard(10);
        ContractEntry actualEntry = entries[0];

        Assert.AreEqual(expectedPosition, actualEntry.Position);
        Assert.AreEqual(player.AccountId, actualEntry.Account.AccountId);
        Assert.AreEqual(player.Username, actualEntry.Account.Username);
        Assert.AreEqual(expectedHasProfileImage, actualEntry.HasProfileImage);
        Assert.AreEqual(player.MatchesPlayed, actualEntry.MatchesPlayed);
        Assert.AreEqual(player.Wins, actualEntry.Wins);
        Assert.AreEqual(expectedWinRate, actualEntry.WinRate);
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
        bool expectedHasProfileImage = false;

        ContractEntry[] entries = Service.GetLeaderboard(10);
        bool actualHasProfileImage = entries[0].HasProfileImage;

        Assert.AreEqual(expectedHasProfileImage, actualHasProfileImage);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(101)]
    public void GetLeaderboard_InvalidMaximum_ThrowsLeaderboardFault(int maximumEntries)
    {
        LeaderboardFailure expectedReason = LeaderboardFailure.InvalidMaximumEntries;
        Action action = () => Service.GetLeaderboard(maximumEntries);

        FaultException<LeaderboardFault> fault =
            Assert.Throws<FaultException<LeaderboardFault>>(action);
        LeaderboardFailure actualReason = fault.Detail.Reason;

        Assert.AreEqual(expectedReason, actualReason);
    }
}
