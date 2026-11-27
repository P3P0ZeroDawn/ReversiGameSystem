using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Preferences;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.Domain.Accounts;
using ReversiGameSystem.Server.ServiceApi;
using ReversiGameSystem.Server.Tests.TestDoubles;
using ContractPreferences = ReversiGameSystem.Server.Contracts.Players.PlayerPreferences;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services;

[TestClass]
[TestCategory("Integration")]
public sealed class PlayerPreferencesServiceIntegrationTests
{
    private const string PrivateProviderMessage = "private-storage-failure";

    private UtcClockStub _clock = null!;
    private SessionStore _sessions = null!;
    private PreferencesRepositoryStub _preferences = null!;
    private TestDoubles.PlayerImageRepositoryStub _images = null!;
    private WcfChannelScope<IPlayerPreferencesService> _scope = null!;
    private IPlayerPreferencesService _channel = null!;
    private string _sessionToken = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        _clock = new UtcClockStub();
        _sessions = new SessionStore(_clock);
        _preferences = new PreferencesRepositoryStub();
        _images = new TestDoubles.PlayerImageRepositoryStub();
        PreferencesApplicationService applicationService =
            new PreferencesApplicationService(_sessions, _preferences, _images);
        PlayerPreferencesService service = new PlayerPreferencesService(applicationService);
        _scope = WcfTestHostHelper.OpenChannel<IPlayerPreferencesService>(
            service,
            "PlayerPreferences");
        _channel = _scope.Channel;
        _sessionToken = TestSessionHelper.CreateSession(_sessions, 10L, "PlayerOne").SessionToken;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _scope?.Dispose();
    }

    [TestMethod]
    public void GetPlayerPreferences_ThroughWcf_MapsStoredPreferences()
    {
        string expectedLocale = "es-MX";
        string expectedPieceStyle = "STANDARD";

        ContractPreferences actualPreferences = _channel.GetPlayerPreferences(_sessionToken);
        string actualLocale = actualPreferences.PreferredLocaleCode;
        string actualPieceStyle = actualPreferences.PieceStyleCode;

        Assert.AreEqual(expectedLocale, actualLocale);
        Assert.AreEqual(expectedPieceStyle, actualPieceStyle);
    }

    [TestMethod]
    public void UpdatePlayerPreferences_ThroughWcf_PersistsPreferences()
    {
        string expectedLocale = "en-US";
        string expectedPieceStyle = "PROFILE_PICTURES";
        ContractPreferences updated = new ContractPreferences
        {
            PreferredLocaleCode = expectedLocale,
            PieceStyleCode = expectedPieceStyle
        };

        _channel.UpdatePlayerPreferences(_sessionToken, updated);
        string actualLocale = _preferences.StoredPreferences.PreferredLocaleCode;
        string actualPieceStyle = _preferences.StoredPreferences.PieceStyleCode;

        Assert.AreEqual(expectedLocale, actualLocale);
        Assert.AreEqual(expectedPieceStyle, actualPieceStyle);
    }

    [TestMethod]
    public void UpdatePlayerPreferences_InvalidLocaleThroughWcf_ReturnsTypedFaultAndKeepsOpen()
    {
        PreferencesFailure expectedReason = PreferencesFailure.InvalidLocale;
        CommunicationState expectedChannelState = CommunicationState.Opened;
        ContractPreferences invalid = new ContractPreferences
        {
            PreferredLocaleCode = "xx-XX",
            PieceStyleCode = "STANDARD"
        };
        Action action = () => _channel.UpdatePlayerPreferences(_sessionToken, invalid);

        FaultException<PreferencesFault> fault =
            Assert.Throws<FaultException<PreferencesFault>>(action);
        PreferencesFailure actualReason = fault.Detail.Reason;
        CommunicationState actualChannelState = _scope.ChannelState;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }

    [TestMethod]
    public void ChangeBoardBackground_AssociationFailureThroughWcf_RemovesImageAndSanitizesFault()
    {
        _preferences.AssociationFailure = new InvalidOperationException(PrivateProviderMessage);
        string expectedDeletedReference =
            TestDoubles.PlayerImageRepositoryStub.DefaultImageReference;
        bool expectedHasCorrelation = true;
        bool expectedContainsSecret = false;
        ImageContent content = new ImageContent
        {
            MediaType = PlayerImagePolicy.PngMediaType,
            Content = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }
        };
        Action action = () => _channel.ChangeBoardBackground(_sessionToken, content);

        FaultException<ServiceFault> fault =
            Assert.Throws<FaultException<ServiceFault>>(action);
        bool actualHasCorrelation = !string.IsNullOrWhiteSpace(fault.Detail.CorrelationId);
        bool actualContainsSecret = fault.ToString().Contains(PrivateProviderMessage);
        string actualDeletedReference = _images.DeletedReference;

        Assert.AreEqual(expectedHasCorrelation, actualHasCorrelation);
        Assert.AreEqual(expectedContainsSecret, actualContainsSecret);
        Assert.AreEqual(expectedDeletedReference, actualDeletedReference);
    }
}
