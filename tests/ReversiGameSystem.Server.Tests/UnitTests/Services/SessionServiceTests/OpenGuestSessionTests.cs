using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Faults;
using ContractGuestSession = ReversiGameSystem.Server.Contracts.Accounts.GuestSession;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.SessionServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class OpenGuestSessionTests : SessionServiceTestBase
{
    [TestMethod]
    public void OpenGuestSession_InvalidName_ThrowsSessionFault()
    {
        SessionFailure expectedReason = SessionFailure.InvalidGuestName;
        Action action = () => Service.OpenGuestSession("ab");

        FaultException<SessionFault> actualException =
            Assert.Throws<FaultException<SessionFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void OpenGuestSession_DuplicateName_ThrowsSessionFault()
    {
        Service.OpenGuestSession("PlayerOne");
        SessionFailure expectedReason = SessionFailure.GuestNameUnavailable;
        Action action = () => Service.OpenGuestSession("PlayerOne");

        FaultException<SessionFault> actualException =
            Assert.Throws<FaultException<SessionFault>>(action);

        Assert.AreEqual(expectedReason, actualException.Detail.Reason);
    }

    [TestMethod]
    public void OpenGuestSession_ValidName_MapsDisplayName()
    {
        string expectedDisplayName = "Guest-PlayerOne";

        ContractGuestSession actualSession = Service.OpenGuestSession("PlayerOne");

        Assert.AreEqual(expectedDisplayName, actualSession.DisplayName);
    }
}
