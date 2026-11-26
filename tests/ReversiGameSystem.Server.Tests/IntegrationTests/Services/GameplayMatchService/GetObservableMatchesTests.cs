using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services.GameplayMatchService;

[TestClass]
[TestCategory("Integration")]
public sealed class GetObservableMatchesTests
{
    private static GameplayTestContext _context = null!;
    private static ServiceHost _host = null!;
    private static Uri _address = null!;
    private static MatchCallbackSubscriptions<IGameplayMatchServiceCallback> _subscriptions = null!;

    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        _context = new GameplayTestContext();
        _context.ApplicationService.TrackStartedMatch(_context.CreateMatch(40));
        _subscriptions = new MatchCallbackSubscriptions<IGameplayMatchServiceCallback>(
            (_, _) => true, NullLogger.Instance);
        ReversiGameSystem.Server.ServiceApi.GameplayMatchService service =
            new ReversiGameSystem.Server.ServiceApi.GameplayMatchService(
                _context.ApplicationService, new GameplayMatchPresence(), _subscriptions);
        _address = new Uri("net.pipe://localhost/ReversiSpectatorTests/"
            + Guid.NewGuid().ToString("N") + "/Gameplay");
        _host = new ServiceHost(service);
        _host.Description.Behaviors.Add(
            new ServiceErrorBehavior(NullLogger<ServiceErrorBehavior>.Instance));
        _host.AddServiceEndpoint(typeof(IGameplayMatchService),
            new NetNamedPipeBinding(NetNamedPipeSecurityMode.None), _address);
        _host.Open();
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _subscriptions.Dispose();
        if (_host.State == CommunicationState.Faulted)
        {
            _host.Abort();
        }
        else
        {
            _host.Close();
        }
    }

    [TestMethod]
    public void GetObservableMatches_Spectator_SerializesNullSeat()
    {
        WithChannel(channel =>
        {
            MatchInformation[] matches =
                channel.GetObservableMatches(_context.Spectator.SessionToken);

            Assert.IsNull(matches[0].AssignedSeatNumber);
        });
    }

    [TestMethod]
    public void GetObservableMatches_Participant_SerializesActualSeat()
    {
        WithChannel(channel =>
        {
            MatchInformation[] matches =
                channel.GetObservableMatches(_context.PlayerTwo.SessionToken);

            Assert.AreEqual<int?>(2, matches[0].AssignedSeatNumber);
        });
    }

    [TestMethod]
    public void GetObservableMatches_InvalidSession_ReturnsDeclaredFaultOnUsableChannel()
    {
        WithChannel(channel =>
        {
            Assert.Throws<FaultException<SessionFault>>(() =>
                channel.GetObservableMatches("invalid"));

            Assert.AreEqual(CommunicationState.Opened, ((ICommunicationObject)channel).State);
        });
    }

    [TestMethod]
    public void GetObservableMatches_StorageFailure_ReturnsServiceFaultOnUsableChannel()
    {
        _context.Repository.ListUnavailable = true;
        try
        {
            WithChannel(channel =>
            {
                Assert.Throws<FaultException<ServiceFault>>(() =>
                    channel.GetObservableMatches(_context.Spectator.SessionToken));

                Assert.AreEqual(CommunicationState.Opened, ((ICommunicationObject)channel).State);
            });
        }
        finally
        {
            _context.Repository.ListUnavailable = false;
        }
    }

    [TestMethod]
    public void ObserveMatch_Spectator_CanReadInformationWithoutSeat()
    {
        WithChannel(channel =>
        {
            channel.ObserveMatch(_context.Spectator.SessionToken, 55);

            MatchInformation match =
                channel.GetMatchInformation(_context.Spectator.SessionToken, 55);

            Assert.IsNull(match.AssignedSeatNumber);
        });
    }

    [TestMethod]
    public void OpenMatchChannel_Spectator_ReturnsUnauthorizedOnUsableChannel()
    {
        WithChannel(channel =>
        {
            FaultException<MatchFault> fault = Assert.Throws<FaultException<MatchFault>>(() =>
                channel.OpenMatchChannel(_context.Spectator.SessionToken, 55));

            Assert.AreEqual(MatchFailure.Unauthorized, fault.Detail.Reason);
            Assert.AreEqual(CommunicationState.Opened, ((ICommunicationObject)channel).State);
        });
    }

    private static void WithChannel(Action<IGameplayMatchService> action)
    {
        DuplexChannelFactory<IGameplayMatchService> factory =
            new DuplexChannelFactory<IGameplayMatchService>(new Callback(),
                new NetNamedPipeBinding(NetNamedPipeSecurityMode.None),
                new EndpointAddress(_address));
        IGameplayMatchService channel = factory.CreateChannel();
        try
        {
            action(channel);
            ((ICommunicationObject)channel).Close();
            factory.Close();
        }
        catch
        {
            ((ICommunicationObject)channel).Abort();
            factory.Abort();
            throw;
        }
    }

    private sealed class Callback : IGameplayMatchServiceCallback
    {
        public void OnMatchStateChanged(MatchStateSnapshot state)
        {
        }

        public void OnParticipantConnectionChanged(long matchId, int seatNumber, bool isConnected)
        {
        }
    }
}
