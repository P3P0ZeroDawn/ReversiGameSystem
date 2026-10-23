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
            TestAssert.IsNotNull(context.Sessions.Find(session.SessionToken));
            TestAssert.AreEqual(1, context.Sessions.CreatedCount);
            channel.CloseSession(session.SessionToken);
            TestAssert.AreEqual<AuthenticatedSessionRecord?>(null,
                context.Sessions.Find(session.SessionToken));
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
            ConsumeResult = VerificationResult.Expired
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
        try { context.Handler.CompleteLogin("PlayerOne", "000012"); }
        catch (SessionRejectedException exception)
        {
            // Assert
            TestAssert.AreEqual(SessionRejection.InactiveAccount, exception.Reason);
            return;
        }
        throw new InvalidOperationException("An inactive account cannot open a session.");
    }

    internal void CompleteLogin_RepositoryRejectsConsumedCode_DoesNotCreateSession()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext
        {
            ConsumeResult = VerificationResult.Invalid
        };
        // Act
        try { context.Handler.CompleteLogin("PlayerOne", "000012"); }
        catch (SessionRejectedException exception)
        {
            // Assert
            TestAssert.AreEqual(SessionRejection.InvalidVerificationCode, exception.Reason);
            TestAssert.AreEqual(0, context.Sessions.CreatedCount);
            return;
        }
        throw new InvalidOperationException("A rejected verification must not create a session.");
    }

    internal void OpenGuestSession_ExpiredGuest_ReleasesReservedName()
    {
        // Arrange
        SessionTestContext context = new SessionTestContext();
        GuestSessionRecord previous = context.Registry.CreateGuest("Visitor");
        context.UtcNow = previous.ExpiresAtUtc;
        // Act
        GuestSessionRecord current = context.Registry.CreateGuest("Visitor");
        // Assert
        TestAssert.AreEqual<GuestSessionRecord?>(null,
            context.Registry.FindGuest(previous.SessionToken));
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
            TestAssert.IsNotNull(context.Registry.FindGuest(guest.SessionToken));
            try { channel.OpenGuestSession("visitor"); }
            catch (FaultException<SessionFault> fault)
            {
                TestAssert.AreEqual(SessionFailure.GuestNameUnavailable, fault.Detail.Reason);
                channel.CloseSession(guest.SessionToken);
                TestAssert.AreEqual<GuestSessionRecord?>(null,
                    context.Registry.FindGuest(guest.SessionToken));
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
            try
            {
                context.Registry.CreateGuest("Visitor");
                Interlocked.Increment(ref accepted);
            }
            catch (SessionRejectedException exception)
            {
                TestAssert.AreEqual(SessionRejection.GuestNameUnavailable, exception.Reason);
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

    private static TResult Invoke<TResult>(SessionTestContext context,
        Func<ISessionService, TResult> action)
    {
        SessionService service = new SessionService(context.Handler,
            NullLogger<SessionService>.Instance);
        Uri address = new Uri("net.pipe://localhost/ReversiGameSystem.Tests/"
            + Guid.NewGuid().ToString("N") + "/");
        NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
        using (ServiceHost host = new ServiceHost(service, address))
        {
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
