using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Chat;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class ChatContentPolicyTests
{
    [TestMethod]
    public void Matches_AccentsCaseAndPunctuation_MatchesCompletePhrase()
    {
        const string content = "A message: PÁLÁBRA, PROHIBIDA!";
        bool expectedMatch = true;

        bool actualMatch = ChatContentPolicy.Matches(content, "palabra prohibida");

        Assert.AreEqual(
            expectedMatch,
            actualMatch,
            "Moderation must normalize diacritics, case, and separators.");
    }

    [TestMethod]
    public void Matches_WordInsideAnotherWord_DoesNotMatch()
    {
        const string content = "An unblockedword fragment";
        bool expectedMatch = false;

        bool actualMatch = ChatContentPolicy.Matches(content, "blockedword");

        Assert.AreEqual(
            expectedMatch,
            actualMatch,
            "A substring must not count as a complete prohibited word.");
    }

    [TestMethod]
    public void Matches_EmptyPattern_DoesNotBlockEveryMessage()
    {
        const string content = "ordinary message";
        bool expectedMatch = false;

        bool actualMatch = ChatContentPolicy.Matches(content, " , ! ");

        Assert.AreEqual(
            expectedMatch,
            actualMatch,
            "An empty normalized pattern cannot prohibit every message.");
    }
}
