using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Client.Tests.TestDoubles;
using ReversiGameSystem.Features.Matches;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests.AvailableMatchesViewModelTests;

[TestClass]
[TestCategory("Integration")]
public sealed class LoadObservableMatchesTests
{
    [TestMethod]
    public void LoadObservableMatches_Guest_DoesNotCallServer()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        ClientSession session = new ClientSession();
        session.StartGuest(new GuestAccountSession("guest-test-session", "Guest",
            DateTime.UtcNow.AddHours(1)));
        AvailableMatchesViewModel model = new AvailableMatchesViewModel(session, service.Gameplay);

        model.LoadObservableMatches().GetAwaiter().GetResult();

        Assert.AreEqual(0, service.ListCalls);
    }

    [TestMethod]
    public void LoadObservableMatches_Authenticated_IncludesOwnSeatMatch()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        AvailableMatchesViewModel model = new AvailableMatchesViewModel(
            ObservableGameplayServiceStub.CreateSession(), service.Gameplay);

        model.LoadObservableMatches().GetAwaiter().GetResult();

        CollectionAssert.AreEqual(new long[] { 19, 20 },
            model.ObservableMatches.Select(match => match.MatchId).ToArray());
    }

    [TestMethod]
    public void LoadObservableMatches_SessionClearedDuringRequest_DiscardsResponse()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        ClientSession session = ObservableGameplayServiceStub.CreateSession();
        service.BeforeListResponse = session.Clear;
        AvailableMatchesViewModel model = new AvailableMatchesViewModel(session, service.Gameplay);

        MatchStatus status = model.LoadObservableMatches().GetAwaiter().GetResult();

        Assert.AreEqual(MatchStatus.InvalidSession, status);
    }
}
