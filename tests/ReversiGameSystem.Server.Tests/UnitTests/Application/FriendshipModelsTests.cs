using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Friendships;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application;

[TestClass]
[TestCategory("Unit")]
public sealed class FriendshipModelsTests
{
    [TestMethod]
    public void FriendAccount_NullUsername_ThrowsArgumentNullException()
    {
        Action action = () => _ = new FriendAccount(10L, null!, true);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void FriendAccount_ValidArguments_SetsAccountId()
    {
        long expectedAccountId = 10L;

        FriendAccount account = new FriendAccount(expectedAccountId, "PlayerTen", true);
        long actualAccountId = account.AccountId;

        Assert.AreEqual(expectedAccountId, actualAccountId);
    }

    [TestMethod]
    public void FriendAccount_ValidArguments_SetsUsername()
    {
        string expectedUsername = "PlayerTen";

        FriendAccount account = new FriendAccount(10L, expectedUsername, false);
        string actualUsername = account.Username;

        Assert.AreEqual(expectedUsername, actualUsername);
    }

    [TestMethod]
    public void FriendAccount_ValidArguments_SetsHasProfileImage()
    {
        bool expectedHasProfileImage = true;

        FriendAccount account = new FriendAccount(10L, "PlayerTen", expectedHasProfileImage);
        bool actualHasProfileImage = account.HasProfileImage;

        Assert.AreEqual(expectedHasProfileImage, actualHasProfileImage);
    }

    [TestMethod]
    public void FriendshipResult_NullValue_ThrowsArgumentNullException()
    {
        Action action = () => _ = new FriendshipResult<string>((string)null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void FriendshipResult_ValidValue_SetsSuccessStatusAndValue()
    {
        FriendshipStatus expectedStatus = FriendshipStatus.Success;
        string expectedValue = "ok";

        FriendshipResult<string> result = new FriendshipResult<string>(expectedValue);
        FriendshipStatus actualStatus = result.Status;
        string? actualValue = result.Value;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedValue, actualValue);
    }

    [TestMethod]
    public void FriendshipResult_SuccessStatusWithoutValue_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => _ = new FriendshipResult<string>(FriendshipStatus.Success);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [TestMethod]
    public void FriendshipResult_FailureStatus_SetsStatusAndNullValue()
    {
        FriendshipStatus expectedStatus = FriendshipStatus.InactiveAccount;

        FriendshipResult<string> result = new FriendshipResult<string>(expectedStatus);
        FriendshipStatus actualStatus = result.Status;
        string? actualValue = result.Value;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsNull(actualValue);
    }
}
