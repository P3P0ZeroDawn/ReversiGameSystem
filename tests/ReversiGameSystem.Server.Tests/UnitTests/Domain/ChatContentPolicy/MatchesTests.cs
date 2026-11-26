using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Chat;

namespace ReversiGameSystem.Server.Tests.UnitTests.Domain.ChatContentPolicyTests;

[TestClass]
[TestCategory("Unit")]
public sealed class MatchesTests
{
    [TestMethod]
    public void Matches_AccentsCaseAndPunctuation_MatchesCompletePhrase()
    {
        const string content = "A message: PÁLÁBRA, PROHIBIDA!";
        bool expectedMatch = true;

        bool actualMatch = ChatContentPolicy.Matches(content, "palabra prohibida");

        Assert.AreEqual(expectedMatch, actualMatch);
    }

    [TestMethod]
    public void Matches_WordInsideAnotherWord_DoesNotMatch()
    {
        const string content = "An unblockedword fragment";
        bool expectedMatch = false;

        bool actualMatch = ChatContentPolicy.Matches(content, "blockedword");

        Assert.AreEqual(expectedMatch, actualMatch);
    }

    [TestMethod]
    public void Matches_EmptyPattern_DoesNotBlockEveryMessage()
    {
        const string content = "ordinary message";
        bool expectedMatch = false;

        bool actualMatch = ChatContentPolicy.Matches(content, " , ! ");

        Assert.AreEqual(expectedMatch, actualMatch);
    }

    [TestMethod]
    public void Matches_EmptyContent_DoesNotMatch()
    {
        const string content = "";
        bool expectedMatch = false;

        bool actualMatch = ChatContentPolicy.Matches(content, "prohibida");

        Assert.AreEqual(expectedMatch, actualMatch);
    }

    [TestMethod]
    public void Matches_MultipleSpacesAndSymbols_MatchesNormalizedPhrase()
    {
        const string content = "hola   ---   MÚNDO!!!";
        bool expectedMatch = true;

        bool actualMatch = ChatContentPolicy.Matches(content, "hola mundo");

        Assert.AreEqual(expectedMatch, actualMatch);
    }
}
