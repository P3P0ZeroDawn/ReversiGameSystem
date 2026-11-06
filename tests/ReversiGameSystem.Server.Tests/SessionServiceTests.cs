using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

internal sealed class SessionServiceTests
{
    internal void RequestLoginCode_ThroughWcfChannel_ReturnsExpiryWithoutCreatingSession()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        // Act
        Invoke(context, channel =>
        {
            DateTime expires = channel.RequestLoginCode("PlayerOne", "Password1!");
            // Assert
            TestAssert.AreEqual(context.UtcNow.AddMinutes(5), expires);
            TestAssert.IsNotNull(context.Email);
            TestAssert.AreEqual<VerificationAttempt?>(null, context.Attempt);
            TestAssert.AreEqual(0, context.Sessions.CreatedCount);
            return true;
        });
    }

    internal void CompleteLogin_ThroughWcfChannel_CreatesMappedSessionAndRevokesIt()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        // Act
        Invoke(context, channel =>
        {
            channel.RequestLoginCode("PlayerOne", "Password1!");
            ReversiGameSystem.Server.Contracts.Accounts.AuthenticatedSession session =
                channel.CompleteLogin("PlayerOne", TestAssert.IsNotNull(context.Email).Code);
            // Assert
            TestAssert.AreEqual<long>(42, session.Account.AccountId);
            TestAssert.AreEqual("en-US", session.PreferredLocaleCode);
            TestAssert.AreEqual(true, context.Sessions.TryGet(session.SessionToken, out _));
            TestAssert.AreEqual(1, context.Sessions.CreatedCount);
            channel.CloseSession(session.SessionToken);
            TestAssert.AreEqual(false, context.Sessions.TryGet(session.SessionToken, out _));
            channel.CloseSession(session.SessionToken);
            return true;
        });
    }

    internal void RequestLoginCode_InvalidCredentialsThroughWcf_ReturnsTypedSessionFault()
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
                TestAssert.AreEqual(SessionFailure.InvalidCredentials, fault.Detail.Reason);
                TestAssert.AreEqual<VerificationEmail?>(null, context.Email);
                return true;
            }
            throw new InvalidOperationException("Expected a typed session fault.");
        });
    }

    internal void RequestLoginCode_DeliveryTimeoutThroughWcf_ReturnsSanitizedServiceFault()
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
                TestAssert.IsTrue(!fault.ToString().Contains("Private provider diagnostic"),
                    "Provider diagnostics must not cross the WCF boundary.");
                TestAssert.AreEqual<PendingVerificationCode?>(null, context.Pending);
                return true;
            }
            throw new InvalidOperationException("Expected a controlled service fault.");
        });
    }

    internal void CompleteLogin_ExpiredCodeThroughWcf_ReturnsTypedExpiryFault()
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
                TestAssert.AreEqual(SessionFailure.ExpiredVerificationCode, fault.Detail.Reason);
                return true;
            }
            throw new InvalidOperationException("Expected an expired-code fault.");
        });
    }

    internal void CompleteLogin_InactiveAccount_RejectsSessionCreation()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext { Inactive = true };
        // Act
        SessionOperationResult<AuthenticatedSession> result =
            context.ApplicationService.CompleteLogin("PlayerOne", "000012");

        // Assert
        TestAssert.AreEqual(SessionOperationStatus.InactiveAccount, result.Status);
        TestAssert.AreEqual(0, context.Sessions.CreatedCount);
    }

    internal void CompleteLogin_RepositoryRejectsConsumedCode_DoesNotCreateSession()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext
        {
            ConsumeStatus = VerificationStatus.Invalid
        };
        // Act
        SessionOperationResult<AuthenticatedSession> result =
            context.ApplicationService.CompleteLogin("PlayerOne", "000012");

        // Assert
        TestAssert.AreEqual(SessionOperationStatus.InvalidVerificationCode, result.Status);
        TestAssert.AreEqual(0, context.Sessions.CreatedCount);
    }

    internal void OpenGuestSession_ExpiredGuest_ReleasesReservedName()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        TestAssert.AreEqual(false,
            context.Sessions.TryGetGuest("unknown", out GuestSession? absent));
        TestAssert.AreEqual<GuestSession?>(null, absent);
        TestAssert.AreEqual(false,
            context.Sessions.TryGetGuest(string.Empty, out GuestSession? empty));
        TestAssert.AreEqual<GuestSession?>(null, empty);
        GuestSession previous = context.Sessions.CreateGuest("Guest-Visitor").GetValue();
        TestAssert.AreEqual(true,
            context.Sessions.TryGetGuest(previous.SessionToken, out GuestSession? active));
        TestAssert.AreEqual(previous, active);
        context.UtcNow = previous.ExpiresAtUtc;
        // Act
        GuestSession current = context.Sessions.CreateGuest("Guest-Visitor").GetValue();
        // Assert
        TestAssert.AreEqual(false,
            context.Sessions.TryGetGuest(previous.SessionToken, out GuestSession? expired));
        TestAssert.AreEqual<GuestSession?>(null, expired);
        TestAssert.AreEqual("Guest-Visitor", current.DisplayName);
        TestAssert.IsTrue(previous.SessionToken != current.SessionToken,
            "A new guest must receive a new token.");
    }

    internal void OpenGuestSession_ThroughWcfChannel_ReservesAndReleasesName()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        // Act
        Invoke(context, channel =>
        {
            ReversiGameSystem.Server.Contracts.Accounts.GuestSession guest =
                channel.OpenGuestSession("Visitor");
            // Assert
            TestAssert.AreEqual("Guest-Visitor", guest.DisplayName);
            TestAssert.AreEqual(true, context.Sessions.TryGetGuest(guest.SessionToken, out _));
            try { channel.OpenGuestSession("visitor"); }
            catch (FaultException<SessionFault> fault)
            {
                TestAssert.AreEqual(SessionFailure.GuestNameUnavailable, fault.Detail.Reason);
                channel.CloseSession(guest.SessionToken);
                TestAssert.AreEqual(false,
                    context.Sessions.TryGetGuest(guest.SessionToken, out GuestSession? revoked));
                TestAssert.AreEqual<GuestSession?>(null, revoked);
                TestAssert.AreEqual("Guest-Visitor",
                    channel.OpenGuestSession("Visitor").DisplayName);
                return true;
            }
            throw new InvalidOperationException("Active guest names must be unique.");
        });
    }

    internal void OpenGuestSession_ConcurrentSameName_CreatesOnlyOneGuest()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        int accepted = 0;
        int rejected = 0;
        // Act
        Parallel.For(0, 16, index =>
        {
            SessionOperationResult<GuestSession> result =
                context.Sessions.CreateGuest("Guest-Visitor");
            if (result.Status == SessionOperationStatus.Success)
            {
                Interlocked.Increment(ref accepted);
            }
            else
            {
                TestAssert.AreEqual(
                    SessionOperationStatus.GuestNameUnavailable,
                    result.Status);
                Interlocked.Increment(ref rejected);
            }
        });
        // Assert
        TestAssert.AreEqual(1, accepted);
        TestAssert.AreEqual(15, rejected);
    }

    internal void CloseSession_EmptyTokenThroughWcf_ReturnsTypedFault()
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
                TestAssert.AreEqual(SessionFailure.MissingSessionToken, fault.Detail.Reason);
                return true;
            }
            throw new InvalidOperationException("Expected a missing-token fault.");
        });
    }

    internal void CloseSession_FailingObserver_NotifiesRemainingObservers()
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
        TestAssert.AreEqual("unknown-token", observer.ObservedToken);
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
