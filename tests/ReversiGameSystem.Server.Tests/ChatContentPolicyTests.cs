using ReversiGameSystem.Server.Domain.Chat;

namespace ReversiGameSystem.Server.Tests;

internal sealed class ChatContentPolicyTests
{
    internal void Matches_AccentsCaseAndPunctuation_MatchesCompletePhrase()
    {
        // Arrange
        const string content = "A message: PÁLÁBRA, PROHIBIDA!";

        // Act
        bool matches = ChatContentPolicy.Matches(content, "palabra prohibida");

        // Assert
        TestAssert.IsTrue(matches, "Moderation must normalize diacritics, case, and separators.");
    }

    internal void Matches_WordInsideAnotherWord_DoesNotMatch()
    {
        // Arrange
        const string content = "An unblockedword fragment";

        // Act
        bool matches = ChatContentPolicy.Matches(content, "blockedword");

        // Assert
        TestAssert.IsTrue(!matches, "A substring must not count as a complete prohibited word.");
    }

    internal void Matches_EmptyPattern_DoesNotBlockEveryMessage()
    {
        // Arrange
        const string content = "ordinary message";

        // Act
        bool matches = ChatContentPolicy.Matches(content, " , ! ");

        // Assert
        TestAssert.IsTrue(!matches, "An empty normalized pattern cannot prohibit every message.");
    }
}
