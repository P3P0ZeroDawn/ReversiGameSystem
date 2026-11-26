using System;
using ReversiGameSystem.Server.Application.Accounts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.GameplayMatchApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class GetObservableMatchesTests
{
    [TestMethod]
    public void GetObservableMatches_EmptyStore_ReturnsEmptyArray()
    {
        GameplayTestContext context = new GameplayTestContext();

        MatchResult<Match[]> result = context.ApplicationService.GetObservableMatches(
            context.Spectator.SessionToken);

        Assert.AreEqual(0, result.Value?.Length);
    }

    [TestMethod]
    public void GetObservableMatches_PrivateSystemMatch_ReturnsMatch()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        match.IsPrivate = true;
        match.Participants[1].AccountId = 0;
        match.Participants[1].IsSystem = true;

        MatchResult<Match[]> result = context.ApplicationService.GetObservableMatches(
            context.Spectator.SessionToken);

        Assert.AreEqual(match.Id, result.Value?[0].Id);
    }

    [TestMethod]
    public void GetObservableMatches_RamFinishedButDatabaseInProgress_ExcludesMatch()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        Match stale = match.CreateSnapshot();
        context.ApplicationService.TrackStartedMatch(match);
        context.Repository.StoreMatch(stale);
        context.Repository.SynchronizationAvailable = false;
        context.ApplicationService.ForfeitMatch(context.PlayerOne.SessionToken, match.Id);

        MatchResult<Match[]> result = context.ApplicationService.GetObservableMatches(
            context.Spectator.SessionToken);

        Assert.AreEqual(0, result.Value?.Length);
    }

    [TestMethod]
    public void GetObservableMatches_UnpersistedActiveRamMatch_IncludesMatch()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        Match stale = match.CreateSnapshot();
        stale.State = "Waiting";
        context.ApplicationService.TrackStartedMatch(match);
        context.Repository.StoreMatch(stale);

        MatchResult<Match[]> result = context.ApplicationService.GetObservableMatches(
            context.Spectator.SessionToken);

        Assert.AreEqual(match.Id, result.Value?[0].Id);
    }

    [TestMethod]
    public void GetObservableMatches_MissingSession_ReturnsMissingSessionToken()
    {
        GameplayTestContext context = new GameplayTestContext();

        MatchResult<Match[]> result = context.ApplicationService.GetObservableMatches(string.Empty);

        Assert.AreEqual(MatchStatus.MissingSessionToken, result.Status);
    }

    [TestMethod]
    public void GetObservableMatches_InactiveAccount_ReturnsInactiveAccount()
    {
        GameplayTestContext context = new GameplayTestContext();
        context.Repository.AccessStatus = MatchStatus.InactiveAccount;

        MatchResult<Match[]> result = context.ApplicationService.GetObservableMatches(
            context.Spectator.SessionToken);

        Assert.AreEqual(MatchStatus.InactiveAccount, result.Status);
    }

    [TestMethod]
    public void GetObservableMatches_DatabaseUnavailable_PropagatesFailure()
    {
        GameplayTestContext context = new GameplayTestContext();
        context.ApplicationService.TrackStartedMatch(context.CreateMatch(40));
        context.Repository.ListUnavailable = true;

        Assert.Throws<InvalidOperationException>(() =>
            context.ApplicationService.GetObservableMatches(context.Spectator.SessionToken));
    }
    [TestMethod]
    public void GetObservableMatches_GuestSession_ReturnsInvalidSession()
    {
        GameplayTestContext context = new GameplayTestContext();
        GuestSession guest = new GuestSession("guest", "Visitor", context.Clock.UtcNow.AddHours(1));
        context.Sessions.TryReserveGuest(guest);

        MatchResult<Match[]> result =
            context.ApplicationService.GetObservableMatches(guest.SessionToken);

        Assert.AreEqual(MatchStatus.InvalidSession, result.Status);
    }

    [TestMethod]
    public void GetObservableMatches_FinishesDuringDatabaseRead_ExcludesFinishedMatch()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.Repository.ObservableMatchesLoaded = () =>
            context.ApplicationService.ForfeitMatch(context.PlayerOne.SessionToken, match.Id);

        MatchResult<Match[]> result = context.ApplicationService.GetObservableMatches(
            context.Spectator.SessionToken);

        Assert.AreEqual(0, result.Value?.Length);
    }
}
