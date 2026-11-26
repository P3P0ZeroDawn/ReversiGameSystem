using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.GameplayMatchApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class GetMatchStateTests
{
    [TestMethod]
    public void GetMatchState_RegisteredObserverAfterConclusion_ReturnsFinalState()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.ApplicationService.ObserveMatch(context.Spectator.SessionToken, match.Id);
        context.ApplicationService.ForfeitMatch(context.PlayerOne.SessionToken, match.Id);

        MatchResult<Match> result = context.ApplicationService.GetMatchState(
            context.Spectator.SessionToken, match.Id);

        Assert.AreEqual("Finished", result.Value?.State);
    }

    [TestMethod]
    public void GetMatchState_UnregisteredObserverFinishedMatch_ReturnsUnauthorized()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        match.State = "Finished";

        MatchResult<Match> result = context.ApplicationService.GetMatchState(
            context.Spectator.SessionToken, match.Id);

        Assert.AreEqual(MatchStatus.Unauthorized, result.Status);
    }

    [TestMethod]
    public void GetMatchState_TrackedMatchWithUnavailableDatabase_ReturnsRamState()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.Repository.AccessUnavailable = true;
        context.Repository.ListUnavailable = true;

        MatchResult<Match> result = context.ApplicationService.GetMatchState(
            context.Spectator.SessionToken, match.Id);

        Assert.AreEqual(MatchStatus.Success, result.Status);
    }
}
