using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application;

[TestClass]
[TestCategory("Unit")]
public sealed class PlayerProfileModelsTests
{
    [TestMethod]
    public void PlayerProfile_ZeroMatches_SetsZeroMatchesPlayedAndZeroWinRate()
    {
        int expectedMatchesPlayed = 0;
        double expectedWinRate = 0d;

        PlayerProfile profile = new PlayerProfile(10L, "PlayerTen", null, 0, 0, 0);
        int actualMatchesPlayed = profile.MatchesPlayed;
        double actualWinRate = profile.WinRate;

        Assert.AreEqual(expectedMatchesPlayed, actualMatchesPlayed);
        Assert.AreEqual(expectedWinRate, actualWinRate);
    }

    [TestMethod]
    public void PlayerProfile_WithMatches_ComputesMatchesPlayedAndWinRate()
    {
        int expectedMatchesPlayed = 5;
        double expectedWinRate = 0.6d;

        PlayerProfile profile = new PlayerProfile(10L, "PlayerTen", "profiles/a.png", 3, 1, 1);
        int actualMatchesPlayed = profile.MatchesPlayed;
        double actualWinRate = profile.WinRate;

        Assert.AreEqual(expectedMatchesPlayed, actualMatchesPlayed);
        Assert.AreEqual(expectedWinRate, actualWinRate);
    }

    [TestMethod]
    public void ProfileResult_NullValue_ThrowsArgumentNullException()
    {
        Action action = () => _ = new ProfileResult<string>((string)null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void ProfileResult_ValidValue_SetsSuccessStatusAndValue()
    {
        ProfileStatus expectedStatus = ProfileStatus.Success;
        string expectedValue = "profiles/a.png";

        ProfileResult<string> result = new ProfileResult<string>(expectedValue);
        ProfileStatus actualStatus = result.Status;
        string? actualValue = result.Value;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedValue, actualValue);
    }

    [TestMethod]
    public void ProfileResult_SuccessStatusWithoutValue_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => _ = new ProfileResult<string>(ProfileStatus.Success);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [TestMethod]
    public void ProfileResult_FailureStatus_SetsStatusAndNullValue()
    {
        ProfileStatus expectedStatus = ProfileStatus.AccountNotFound;

        ProfileResult<string> result = new ProfileResult<string>(expectedStatus);
        ProfileStatus actualStatus = result.Status;
        string? actualValue = result.Value;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsNull(actualValue);
    }

    [TestMethod]
    public void GetProfileImageResult_DefaultConstructor_SetsSuccessStatusAndNullImage()
    {
        ProfileStatus expectedStatus = ProfileStatus.Success;

        GetProfileImageResult result = new GetProfileImageResult();
        ProfileStatus actualStatus = result.Status;
        ProfileImageData? actualImage = result.Image;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsNull(actualImage);
    }

    [TestMethod]
    public void GetProfileImageResult_SuccessStatus_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => _ = new GetProfileImageResult(ProfileStatus.Success);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [TestMethod]
    public void GetProfileImageResult_FailureStatus_SetsStatusAndNullImage()
    {
        ProfileStatus expectedStatus = ProfileStatus.InactiveAccount;

        GetProfileImageResult result = new GetProfileImageResult(expectedStatus);
        ProfileStatus actualStatus = result.Status;
        ProfileImageData? actualImage = result.Image;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsNull(actualImage);
    }

    [TestMethod]
    public void GetProfileImageResult_WithImage_SetsSuccessStatusAndImage()
    {
        ProfileStatus expectedStatus = ProfileStatus.Success;
        ProfileImageData expectedImage = new ProfileImageData(
            new byte[] { 137, 80, 78, 71 },
            PlayerImagePolicy.PngMediaType);

        GetProfileImageResult result = new GetProfileImageResult(expectedImage);
        ProfileStatus actualStatus = result.Status;
        ProfileImageData? actualImage = result.Image;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreSame(expectedImage, actualImage);
    }
}
