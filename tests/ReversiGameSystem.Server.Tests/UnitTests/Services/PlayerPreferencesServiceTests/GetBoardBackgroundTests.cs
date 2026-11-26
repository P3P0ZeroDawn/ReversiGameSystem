using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.PlayerPreferencesServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class GetBoardBackgroundTests : PlayerPreferencesServiceTestBase
{
    [TestMethod]
    public void GetBoardBackground_NoStoredReference_ReturnsNull()
    {
        Repository.StoredPreferences.BoardBackgroundReference = string.Empty;

        ImageContent? actualImage = Service.GetBoardBackground(Session.SessionToken);

        Assert.IsNull(actualImage);
    }

    [TestMethod]
    public void GetBoardBackground_StoredReference_MapsImageContent()
    {
        Repository.StoredPreferences.BoardBackgroundReference = "board-backgrounds/custom.png";
        byte[] expectedContent = Images.StoredImage.Content;
        string expectedMediaType = Images.StoredImage.MediaType;

        ImageContent? result = Service.GetBoardBackground(Session.SessionToken);
        byte[] actualContent = result!.Content;
        string actualMediaType = result.MediaType;

        Assert.AreSame(expectedContent, actualContent);
        Assert.AreEqual(expectedMediaType, actualMediaType);
    }

    [TestMethod]
    public void GetBoardBackground_InvalidSession_ThrowsSessionFault()
    {
        Sessions.RevokeSession(Session.SessionToken);
        SessionFailure expectedReason = SessionFailure.InvalidSession;
        Action action = () => Service.GetBoardBackground(Session.SessionToken);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualReason = exception.Detail.Reason;

        Assert.AreEqual(expectedReason, actualReason);
    }
}
