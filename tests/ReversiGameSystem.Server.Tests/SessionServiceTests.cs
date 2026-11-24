using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class SessionServiceTests
{
    [TestMethod]
    public void RequestLoginCode_ThroughWcfChannel_ReturnsExpiryWithoutCreatingSession()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        // Act
        Invoke(context, channel =>
        {
            DateTime expires = channel.RequestLoginCode("PlayerOne", "Password1!");
            // Assert
            Assert.AreEqual(context.UtcNow.AddMinutes(5), expires);
            TestAssert.IsNotNull(context.Email);
            Assert.AreEqual<VerificationAttempt?>(null, context.Attempt);
            return true;
        });
    }

    [TestMethod]
    public void CompleteLogin_ThroughWcfChannel_CreatesMappedSessionAndRevokesIt()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        // Act
        Invoke(context, channel =>
        {
            channel.RequestLoginCode("PlayerOne", "Password1!");
            TestAssert.IsNotNull(context.Email);
            ReversiGameSystem.Server.Contracts.Accounts.AuthenticatedSession session =
                channel.CompleteLogin("PlayerOne", "000012");
            // Assert
            Assert.AreEqual<long>(42, session.Account.AccountId);
            Assert.AreEqual("en-US", session.PreferredLocaleCode);
            Assert.AreEqual(
                SessionStatus.Success,
                context.Sessions.FindAuthenticated(session.SessionToken).Status);
            channel.CloseSession(session.SessionToken);
            Assert.AreEqual(
                SessionStatus.InvalidSession,
                context.Sessions.FindAuthenticated(session.SessionToken).Status);
            channel.CloseSession(session.SessionToken);
            return true;
        });
    }

    [TestMethod]
    public void RequestLoginCode_InvalidCredentialsThroughWcf_ReturnsTypedSessionFault()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        // Act
        Invoke(context, channel =>
        {
            try { channel.RequestLoginCode("PlayerOne", "wrong"); }
            catch (FaultException<SessionFault> fault)
            {
                // Assert
                Assert.AreEqual(SessionFailure.InvalidCredentials, fault.Detail.Reason);
                Assert.AreEqual<EmailMessage?>(null, context.Email);
                return true;
            }
            throw new InvalidOperationException("Expected a typed session fault.");
        });
    }

    [TestMethod]
    public void RequestLoginCode_DeliveryTimeoutThroughWcf_ReturnsSanitizedServiceFault()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext { FailDelivery = true };
        // Act
        Invoke(context, channel =>
        {
            try { channel.RequestLoginCode("PlayerOne", "Password1!"); }
            catch (FaultException<ServiceFault> fault)
            {
                // Assert
                Assert.IsTrue(!fault.ToString().Contains("Private provider diagnostic"),
                    "Provider diagnostics must not cross the WCF boundary.");
                Assert.AreEqual<PendingVerificationCode?>(null, context.Pending);
                return true;
            }
            throw new InvalidOperationException("Expected a controlled service fault.");
        });
    }

    [TestMethod]
    public void CompleteLogin_ExpiredCodeThroughWcf_ReturnsTypedExpiryFault()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext
        {
            ConsumeStatus = VerificationStatus.Expired
        };
        // Act
        Invoke(context, channel =>
        {
            try { channel.CompleteLogin("PlayerOne", "000012"); }
            catch (FaultException<SessionFault> fault)
            {
                // Assert
                Assert.AreEqual(SessionFailure.ExpiredVerificationCode, fault.Detail.Reason);
                return true;
            }
            throw new InvalidOperationException("Expected an expired-code fault.");
        });
    }

    [TestMethod]
    public void CompleteLogin_InactiveAccount_RejectsSessionCreation()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext { Inactive = true };
        // Act
        SessionResult<AuthenticatedSession> result =
            context.ApplicationService.CompleteLogin("PlayerOne", "000012");

        // Assert
        Assert.AreEqual(SessionStatus.InactiveAccount, result.Status);
    }

    [TestMethod]
    public void CompleteLogin_RepositoryRejectsConsumedCode_DoesNotCreateSession()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext
        {
            ConsumeStatus = VerificationStatus.Invalid
        };
        // Act
        SessionResult<AuthenticatedSession> result =
            context.ApplicationService.CompleteLogin("PlayerOne", "000012");

        // Assert
        Assert.AreEqual(SessionStatus.InvalidVerificationCode, result.Status);
    }

    [TestMethod]
    public void OpenGuestSession_ExpiredGuest_ReleasesReservedName()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        Assert.AreEqual(false, context.Sessions.IsGuestActive("unknown"));
        Assert.AreEqual(false, context.Sessions.IsGuestActive(string.Empty));
        GuestSession previous = context.ApplicationService.OpenGuestSession("Visitor").Value
            ?? throw new InvalidOperationException("Guest session expected.");
        Assert.AreEqual(true, context.Sessions.IsGuestActive(previous.SessionToken));
        context.UtcNow = previous.ExpiresAtUtc;
        // Act
        GuestSession current = context.ApplicationService.OpenGuestSession("Visitor").Value
            ?? throw new InvalidOperationException("Guest session expected.");
        // Assert
        Assert.AreEqual(false, context.Sessions.IsGuestActive(previous.SessionToken));
        Assert.AreEqual("Guest-Visitor", current.DisplayName);
        Assert.IsTrue(previous.SessionToken != current.SessionToken,
            "A new guest must receive a new token.");
    }

    [TestMethod]
    public void OpenGuestSession_ThroughWcfChannel_ReservesAndReleasesName()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        // Act
        Invoke(context, channel =>
        {
            ReversiGameSystem.Server.Contracts.Accounts.GuestSession guest =
                channel.OpenGuestSession("Visitor");
            // Assert
            Assert.AreEqual("Guest-Visitor", guest.DisplayName);
            Assert.AreEqual(true, context.Sessions.IsGuestActive(guest.SessionToken));
            try { channel.OpenGuestSession("visitor"); }
            catch (FaultException<SessionFault> fault)
            {
                Assert.AreEqual(SessionFailure.GuestNameUnavailable, fault.Detail.Reason);
                channel.CloseSession(guest.SessionToken);
                Assert.AreEqual(false, context.Sessions.IsGuestActive(guest.SessionToken));
                Assert.AreEqual("Guest-Visitor",
                    channel.OpenGuestSession("Visitor").DisplayName);
                return true;
            }
            throw new InvalidOperationException("Active guest names must be unique.");
        });
    }

    [TestMethod]
    public void OpenGuestSession_ConcurrentSameName_CreatesOnlyOneGuest()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        int accepted = 0;
        int rejected = 0;
        // Act
        Parallel.For(0, 16, index =>
        {
            SessionResult<GuestSession> result =
                context.ApplicationService.OpenGuestSession("Visitor");
            if (result.Status == SessionStatus.Success)
            {
                Interlocked.Increment(ref accepted);
            }
            else
            {
                Assert.AreEqual(
                    SessionStatus.GuestNameUnavailable,
                    result.Status);
                Interlocked.Increment(ref rejected);
            }
        });
        // Assert
        Assert.AreEqual(1, accepted);
        Assert.AreEqual(15, rejected);
    }

    [TestMethod]
    public void CloseSession_EmptyTokenThroughWcf_ReturnsTypedFault()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        // Act
        Invoke(context, channel =>
        {
            try { channel.CloseSession(string.Empty); }
            catch (FaultException<SessionFault> fault)
            {
                // Assert
                Assert.AreEqual(SessionFailure.MissingSessionToken, fault.Detail.Reason);
                return true;
            }
            throw new InvalidOperationException("Expected a missing-token fault.");
        });
    }

    [TestMethod]
    public void CloseSession_FailingObserver_NotifiesRemainingObservers()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        SessionService service = new SessionService(
            context.ApplicationService,
            NullLogger<SessionService>.Instance);
        SessionClosureObserver observer = new SessionClosureObserver();
        service.SessionClosed += observer.Fail;
        service.SessionClosed += observer.Record;

        // Act
        service.CloseSession("unknown-token");

        // Assert
        Assert.AreEqual("unknown-token", observer.ObservedToken);
    }

    private static TResult Invoke<TResult>(SessionTestContext context,
        Func<ISessionService, TResult> action)
    {
        SessionService service = new SessionService(context.ApplicationService,
            NullLogger<SessionService>.Instance);
        Uri address = new Uri("net.pipe://localhost/ReversiGameSystem.Tests/"
            + Guid.NewGuid().ToString("N") + "/");
        NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
        using (ServiceHost host = new ServiceHost(service, address))
        {
            host.Description.Behaviors.Add(new ServiceErrorBehavior(
                NullLogger<ServiceErrorBehavior>.Instance));
            host.AddServiceEndpoint(typeof(ISessionService), binding, "Session");
            host.Open();
            ChannelFactory<ISessionService> factory = new ChannelFactory<ISessionService>(binding,
                new EndpointAddress(new Uri(address, "Session")));
            ISessionService channel = factory.CreateChannel();
            try
            {
                TResult result = action(channel);
                ((ICommunicationObject)channel).Close();
                factory.Close();
                return result;
            }
            catch
            {
                ((ICommunicationObject)channel).Abort();
                factory.Abort();
                throw;
            }
        }
    }
}

internal sealed class SessionClosureObserver
{
    internal string? ObservedToken { get; private set; }

    internal void Fail(string sessionToken)
    {
        ObservedToken = sessionToken;
        throw new InvalidOperationException("Observer failure for isolation testing.");
    }

    internal void Record(string sessionToken)
    {
        ObservedToken = sessionToken;
    }
}
