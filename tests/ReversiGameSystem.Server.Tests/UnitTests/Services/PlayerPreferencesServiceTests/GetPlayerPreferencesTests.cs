using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Faults;
using ContractPreferences = ReversiGameSystem.Server.Contracts.Players.PlayerPreferences;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.PlayerPreferencesServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class GetPlayerPreferencesTests : PlayerPreferencesServiceTestBase
{
    [TestMethod]
    public void GetPlayerPreferences_ValidSession_MapsAllPreferenceFields()
    {
        const string expectedLocaleCode = "en-US";
        const string expectedTimeZoneId = "UTC";
        const string expectedPieceStyleCode = "PROFILE_PICTURES";
        const bool expectedUseCustomBackground = true;
        Repository.StoredPreferences.PreferredLocaleCode = expectedLocaleCode;
        Repository.StoredPreferences.TimeZoneId = expectedTimeZoneId;
        Repository.StoredPreferences.PieceStyleCode = expectedPieceStyleCode;
        Repository.StoredPreferences.UseCustomBoardBackground = expectedUseCustomBackground;

        ContractPreferences result = Service.GetPlayerPreferences(Session.SessionToken);
        string actualLocaleCode = result.PreferredLocaleCode;
        string? actualTimeZoneId = result.TimeZoneId;
        string actualPieceStyleCode = result.PieceStyleCode;
        bool actualUseCustomBackground = result.UseCustomBoardBackground;

        Assert.AreEqual(expectedLocaleCode, actualLocaleCode);
        Assert.AreEqual(expectedTimeZoneId, actualTimeZoneId);
        Assert.AreEqual(expectedPieceStyleCode, actualPieceStyleCode);
        Assert.AreEqual(expectedUseCustomBackground, actualUseCustomBackground);
    }

    [TestMethod]
    public void GetPlayerPreferences_MissingSessionToken_ThrowsSessionFault()
    {
        SessionFailure expectedReason = SessionFailure.MissingSessionToken;
        Action action = () => Service.GetPlayerPreferences(string.Empty);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualReason = exception.Detail.Reason;

        Assert.AreEqual(expectedReason, actualReason);
    }

    [TestMethod]
    public void GetPlayerPreferences_InvalidSession_ThrowsSessionFault()
    {
        Sessions.RevokeSession(Session.SessionToken);
        SessionFailure expectedReason = SessionFailure.InvalidSession;
        Action action = () => Service.GetPlayerPreferences(Session.SessionToken);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualReason = exception.Detail.Reason;

        Assert.AreEqual(expectedReason, actualReason);
    }

    [TestMethod]
    public void GetPlayerPreferences_InactiveAccount_ThrowsPreferencesFault()
    {
        Repository.IsActiveAccount = false;
        PreferencesFailure expectedReason = PreferencesFailure.InactiveAccount;
        Action action = () => Service.GetPlayerPreferences(Session.SessionToken);

        FaultException<PreferencesFault> exception =
            Assert.Throws<FaultException<PreferencesFault>>(action);
        PreferencesFailure actualReason = exception.Detail.Reason;

        Assert.AreEqual(expectedReason, actualReason);
    }
}
