using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.GameplayMatchApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class GetMoveHistoryTests
{
    [TestMethod]
    public void GetMoveHistory_ObserverInProgress_ReturnsLiveMoves()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.ApplicationService.TrackStartedMatch(match);
        context.ApplicationService.PlacePiece(context.PlayerOne.SessionToken,
            new MatchPlacement(match.Id, 0, 2, 3));

        MatchResult<MatchMove[]> result = context.ApplicationService.GetMoveHistory(
            context.Spectator.SessionToken, match.Id);

        Assert.AreEqual(1, result.Value?.Length);
    }
}
