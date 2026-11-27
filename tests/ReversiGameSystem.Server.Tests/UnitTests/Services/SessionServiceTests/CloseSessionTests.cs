using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.SessionServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class CloseSessionTests : SessionServiceTestBase
{
    [TestMethod]
    public void CloseSession_EmptyToken_ThrowsSessionFault()
    {
        SessionFailure expectedReason = SessionFailure.MissingSessionToken;
        Action action = () => Service.CloseSession("");

        FaultException<SessionFault> actualException =
            Assert.Throws<FaultException<SessionFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void CloseSession_ActiveSession_NotifiesSessionClosedObserver()
    {
        AuthenticatedSession session = TestSessionHelper.CreateSession(
            Sessions,
            42,
            "PlayerOne");
        string? actualClosedToken = null;
        Service.SessionClosed += token => actualClosedToken = token;
        string expectedClosedToken = session.SessionToken;

        Service.CloseSession(session.SessionToken);

        Assert.AreEqual(expectedClosedToken, actualClosedToken);
    }

    [TestMethod]
    public void CloseSession_FailingObserver_NotifiesRemainingObservers()
    {
        AuthenticatedSession session = TestSessionHelper.CreateSession(
            Sessions,
            42,
            "PlayerOne");
        string? actualClosedToken = null;
        Service.SessionClosed += _ => throw new InvalidOperationException("Observer failed.");
        Service.SessionClosed += token => actualClosedToken = token;
        string expectedClosedToken = session.SessionToken;

        Service.CloseSession(session.SessionToken);

        Assert.AreEqual(expectedClosedToken, actualClosedToken);
    }
}
