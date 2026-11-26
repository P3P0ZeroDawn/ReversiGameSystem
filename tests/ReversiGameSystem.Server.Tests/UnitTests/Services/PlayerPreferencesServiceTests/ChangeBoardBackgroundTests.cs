using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.PlayerPreferencesServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class ChangeBoardBackgroundTests : PlayerPreferencesServiceTestBase
{
    [TestMethod]
    public void ChangeBoardBackground_ValidImageContent_MapsAndSavesBackground()
    {
        ImageContent request = CreateValidPngImageContent();
        const string expectedReference =
            TestDoubles.PlayerImageRepositoryStub.DefaultImageReference;

        Service.ChangeBoardBackground(Session.SessionToken, request);
        string actualReference = Repository.StoredPreferences.BoardBackgroundReference;

        Assert.AreEqual(expectedReference, actualReference);
    }

    [TestMethod]
    public void ChangeBoardBackground_NullImage_ThrowsPreferencesFault()
    {
        PreferencesFailure expectedReason = PreferencesFailure.InvalidImage;
        Action action = () => Service.ChangeBoardBackground(Session.SessionToken, null!);

        FaultException<PreferencesFault> exception =
            Assert.Throws<FaultException<PreferencesFault>>(action);
        PreferencesFailure actualReason = exception.Detail.Reason;

        Assert.AreEqual(expectedReason, actualReason);
    }

    [TestMethod]
    public void ChangeBoardBackground_InvalidImage_ThrowsPreferencesFault()
    {
        ImageContent request = new ImageContent
        {
            Content = Array.Empty<byte>(),
            MediaType = "image/png"
        };
        PreferencesFailure expectedReason = PreferencesFailure.InvalidImage;
        Action action = () => Service.ChangeBoardBackground(Session.SessionToken, request);

        FaultException<PreferencesFault> exception =
            Assert.Throws<FaultException<PreferencesFault>>(action);
        PreferencesFailure actualReason = exception.Detail.Reason;

        Assert.AreEqual(expectedReason, actualReason);
    }

    [TestMethod]
    public void ChangeBoardBackground_MissingSessionToken_ThrowsSessionFault()
    {
        ImageContent request = CreateValidPngImageContent();
        SessionFailure expectedReason = SessionFailure.MissingSessionToken;
        Action action = () => Service.ChangeBoardBackground(string.Empty, request);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualReason = exception.Detail.Reason;

        Assert.AreEqual(expectedReason, actualReason);
    }
}
