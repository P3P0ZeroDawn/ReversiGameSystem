using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Faults;
using ContractPreferences = ReversiGameSystem.Server.Contracts.Players.PlayerPreferences;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.PlayerPreferencesServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class UpdatePlayerPreferencesTests : PlayerPreferencesServiceTestBase
{
    [TestMethod]
    public void UpdatePlayerPreferences_ValidContract_MapsAndSavesPreferences()
    {
        const string expectedLocaleCode = "en-US";
        const string expectedPieceStyleCode = "PROFILE_PICTURES";
        const string expectedTimeZoneId = "UTC";
        const bool expectedUseCustomBackground = true;
        ContractPreferences request = CreateContractPreferences(
            expectedLocaleCode,
            expectedPieceStyleCode,
            expectedTimeZoneId,
            expectedUseCustomBackground);

        Service.UpdatePlayerPreferences(Session.SessionToken, request);
        string actualLocaleCode = Repository.StoredPreferences.PreferredLocaleCode;
        string actualPieceStyleCode = Repository.StoredPreferences.PieceStyleCode;
        string? actualTimeZoneId = Repository.StoredPreferences.TimeZoneId;
        bool actualUseCustomBackground = Repository.StoredPreferences.UseCustomBoardBackground;

        Assert.AreEqual(expectedLocaleCode, actualLocaleCode);
        Assert.AreEqual(expectedPieceStyleCode, actualPieceStyleCode);
        Assert.AreEqual(expectedTimeZoneId, actualTimeZoneId);
        Assert.AreEqual(expectedUseCustomBackground, actualUseCustomBackground);
    }

    [TestMethod]
    public void UpdatePlayerPreferences_NullPreferences_ThrowsPreferencesFault()
    {
        PreferencesFailure expectedReason = PreferencesFailure.MissingPreferences;
        Action action = () => Service.UpdatePlayerPreferences(Session.SessionToken, null!);

        FaultException<PreferencesFault> exception =
            Assert.Throws<FaultException<PreferencesFault>>(action);
        PreferencesFailure actualReason = exception.Detail.Reason;

        Assert.AreEqual(expectedReason, actualReason);
    }

    [TestMethod]
    public void UpdatePlayerPreferences_InvalidLocale_ThrowsPreferencesFault()
    {
        ContractPreferences request = CreateContractPreferences(localeCode: "fr-FR");
        PreferencesFailure expectedReason = PreferencesFailure.InvalidLocale;
        Action action = () => Service.UpdatePlayerPreferences(Session.SessionToken, request);

        FaultException<PreferencesFault> exception =
            Assert.Throws<FaultException<PreferencesFault>>(action);
        PreferencesFailure actualReason = exception.Detail.Reason;

        Assert.AreEqual(expectedReason, actualReason);
    }

    [TestMethod]
    public void UpdatePlayerPreferences_InvalidTimeZone_ThrowsPreferencesFault()
    {
        ContractPreferences request = CreateContractPreferences(timeZoneId: "InvalidZone");
        PreferencesFailure expectedReason = PreferencesFailure.InvalidTimeZone;
        Action action = () => Service.UpdatePlayerPreferences(Session.SessionToken, request);

        FaultException<PreferencesFault> exception =
            Assert.Throws<FaultException<PreferencesFault>>(action);
        PreferencesFailure actualReason = exception.Detail.Reason;

        Assert.AreEqual(expectedReason, actualReason);
    }

    [TestMethod]
    public void UpdatePlayerPreferences_InvalidPieceStyle_ThrowsPreferencesFault()
    {
        ContractPreferences request = CreateContractPreferences(pieceStyleCode: "UNKNOWN");
        PreferencesFailure expectedReason = PreferencesFailure.InvalidPieceStyle;
        Action action = () => Service.UpdatePlayerPreferences(Session.SessionToken, request);

        FaultException<PreferencesFault> exception =
            Assert.Throws<FaultException<PreferencesFault>>(action);
        PreferencesFailure actualReason = exception.Detail.Reason;

        Assert.AreEqual(expectedReason, actualReason);
    }

    [TestMethod]
    public void UpdatePlayerPreferences_MissingSessionToken_ThrowsSessionFault()
    {
        ContractPreferences request = CreateContractPreferences();
        SessionFailure expectedReason = SessionFailure.MissingSessionToken;
        Action action = () => Service.UpdatePlayerPreferences(string.Empty, request);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualReason = exception.Detail.Reason;

        Assert.AreEqual(expectedReason, actualReason);
    }
}
