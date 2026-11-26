using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.GameplayMatchApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class ObserveMatchTests
{
    [TestMethod]
    public void ObserveMatch_InactiveAccount_ReturnsInactiveAccount()
    {
        GameplayTestContext context = new GameplayTestContext();
        Match match = context.CreateMatch(40);
        context.Repository.AccessStatus = MatchStatus.InactiveAccount;

        MatchResult<Match> result = context.ApplicationService.ObserveMatch(
            context.Spectator.SessionToken, match.Id);

        Assert.AreEqual(MatchStatus.InactiveAccount, result.Status);
    }

}
