using System;
using System.ServiceModel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.ServiceApi;
using ContractAuthenticatedSession =
    ReversiGameSystem.Server.Contracts.Accounts.AuthenticatedSession;
using ContractGuestSession = ReversiGameSystem.Server.Contracts.Accounts.GuestSession;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services;

[TestClass]
[TestCategory("Integration")]
public sealed class SessionServiceIntegrationTests
{
    private const string DefaultUsername = "PlayerOne";
    private const string DefaultPassword = "Password1!";
    private const string GuestBaseName = "Visitor";
    private const string ExpectedGuestDisplayName = "Guest-Visitor";

    private SessionTestContext _context = null!;
    private WcfChannelScope<ISessionService> _scope = null!;
    private ISessionService _channel = null!;

    [TestInitialize]
    public void Initialize()
    {
        _context = new SessionTestContext();
        SessionService service = new SessionService(
            _context.ApplicationService,
            NullLogger<SessionService>.Instance);
        _scope = WcfTestHostHelper.OpenChannel<ISessionService>(service, "Session");
        _channel = _scope.Channel;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void RequestLoginCode_ThroughWcfChannel_ReturnsExpiryWithoutCreatingSession()
    {
        DateTime expectedExpiry = _context.UtcNow.AddMinutes(5);

        DateTime actualExpiry = _channel.RequestLoginCode(DefaultUsername, DefaultPassword);

        Assert.AreEqual(expectedExpiry, actualExpiry);
        Assert.IsNotNull(_context.Email);
        Assert.IsNull(_context.Stored.UsedAtUtc);
    }

    [TestMethod]
    public void CompleteLogin_ThroughWcfChannel_CreatesMappedSessionAndRevokesIt()
    {
        long expectedAccountId = 42L;
        string expectedLocale = "en-US";
        SessionStatus expectedActiveStatus = SessionStatus.Success;
        SessionStatus expectedClosedStatus = SessionStatus.InvalidSession;
        _channel.RequestLoginCode(DefaultUsername, DefaultPassword);
        EmailMessage deliveredEmail = TestAssert.IsNotNull(_context.Email);
        string deliveredCode = Regex
            .Match(deliveredEmail.Body, @"(?<!#)\b(\d{6})\b")
            .Groups[1]
            .Value;

        ContractAuthenticatedSession session =
            _channel.CompleteLogin(DefaultUsername, deliveredCode);
        long actualAccountId = session.Account.AccountId;
        string actualLocale = session.PreferredLocaleCode;
        SessionStatus actualActiveStatus =
            _context.Sessions.FindAuthenticated(session.SessionToken).Status;
        _channel.CloseSession(session.SessionToken);
        SessionStatus actualClosedStatus =
            _context.Sessions.FindAuthenticated(session.SessionToken).Status;

        Assert.AreEqual(expectedAccountId, actualAccountId);
        Assert.AreEqual(expectedLocale, actualLocale);
        Assert.AreEqual(expectedActiveStatus, actualActiveStatus);
        Assert.AreEqual(expectedClosedStatus, actualClosedStatus);
    }

    [TestMethod]
    public void RequestLoginCode_InvalidCredentialsThroughWcf_ReturnsTypedSessionFault()
    {
        SessionFailure expectedReason = SessionFailure.InvalidCredentials;
        CommunicationState expectedChannelState = CommunicationState.Opened;
        Action action = () => _channel.RequestLoginCode(DefaultUsername, "wrong");

        FaultException<SessionFault> fault =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualReason = fault.Detail.Reason;
        CommunicationState actualChannelState = ((ICommunicationObject)_channel).State;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedChannelState, actualChannelState);
        Assert.IsNull(_context.Email);
    }

    [TestMethod]
    public void RequestLoginCode_DeliveryTimeoutThroughWcf_ReturnsSanitizedServiceFault()
    {
        _context.FailDelivery = true;
        bool expectedContainsSecret = false;
        Action action = () => _channel.RequestLoginCode(DefaultUsername, DefaultPassword);

        FaultException<ServiceFault> fault =
            Assert.Throws<FaultException<ServiceFault>>(action);
        bool actualContainsSecret = fault.ToString().Contains("Private provider diagnostic");

        Assert.AreEqual(expectedContainsSecret, actualContainsSecret);
        Assert.IsNull(_context.Pending);
    }

    [TestMethod]
    public void CompleteLogin_ExpiredCodeThroughWcf_ReturnsTypedExpiryFault()
    {
        _context.ConsumeStatus = VerificationStatus.Expired;
        SessionFailure expectedReason = SessionFailure.ExpiredVerificationCode;
        CommunicationState expectedChannelState = CommunicationState.Opened;
        Action action = () => _channel.CompleteLogin(DefaultUsername, "000012");

        FaultException<SessionFault> fault =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualReason = fault.Detail.Reason;
        CommunicationState actualChannelState = ((ICommunicationObject)_channel).State;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }

    [TestMethod]
    public void OpenGuestSession_ThroughWcfChannel_ReservesAndReleasesName()
    {
        SessionFailure expectedDuplicateReason = SessionFailure.GuestNameUnavailable;
        bool expectedActiveAfterOpen = true;
        bool expectedActiveAfterClose = false;

        ContractGuestSession guest = _channel.OpenGuestSession(GuestBaseName);
        string actualDisplayName = guest.DisplayName;
        bool actualActiveAfterOpen = _context.Sessions.IsGuestActive(guest.SessionToken);
        Action duplicateAction = () => _channel.OpenGuestSession("visitor");
        FaultException<SessionFault> fault =
            Assert.Throws<FaultException<SessionFault>>(duplicateAction);
        SessionFailure actualDuplicateReason = fault.Detail.Reason;
        _channel.CloseSession(guest.SessionToken);
        bool actualActiveAfterClose = _context.Sessions.IsGuestActive(guest.SessionToken);
        ContractGuestSession reopened = _channel.OpenGuestSession(GuestBaseName);
        string actualReopenedDisplayName = reopened.DisplayName;

        Assert.AreEqual(ExpectedGuestDisplayName, actualDisplayName);
        Assert.AreEqual(expectedActiveAfterOpen, actualActiveAfterOpen);
        Assert.AreEqual(expectedDuplicateReason, actualDuplicateReason);
        Assert.AreEqual(expectedActiveAfterClose, actualActiveAfterClose);
        Assert.AreEqual(ExpectedGuestDisplayName, actualReopenedDisplayName);
    }

    [TestMethod]
    public void CloseSession_EmptyTokenThroughWcf_ReturnsTypedFault()
    {
        SessionFailure expectedReason = SessionFailure.MissingSessionToken;
        CommunicationState expectedChannelState = CommunicationState.Opened;
        Action action = () => _channel.CloseSession(string.Empty);

        FaultException<SessionFault> fault =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualReason = fault.Detail.Reason;
        CommunicationState actualChannelState = ((ICommunicationObject)_channel).State;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }
}
