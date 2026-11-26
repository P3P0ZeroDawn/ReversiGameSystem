using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Client.Tests.TestDoubles;
using ReversiGameSystem.Features.Matches;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests.MatchInformationViewModelTests;

[TestClass]
[TestCategory("Integration")]
public sealed class ActivateTests
{
    [TestMethod]
    public void Activate_Guest_DoesNotOpenObserverChannel()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        ClientSession session = new ClientSession();
        session.StartGuest(new GuestAccountSession("guest-test-session", "Guest",
            DateTime.UtcNow.AddHours(1)));
        MatchInformationViewModel model = new MatchInformationViewModel(
            service.Gameplay, session, 19);

        model.Activate().GetAwaiter().GetResult();

        Assert.AreEqual(0, service.ObserveCalls);
    }

    [TestMethod]
    public void Activate_ParticipantChoosingObservation_UsesObserverOperation()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        MatchInformationViewModel model = new MatchInformationViewModel(service.Gameplay,
            ObservableGameplayServiceStub.CreateSession(), 19);
        try
        {
            model.Activate().GetAwaiter().GetResult();

            Assert.AreEqual(1, service.ObserveCalls);
        }
        finally
        {
            model.Deactivate();
        }
    }
}
