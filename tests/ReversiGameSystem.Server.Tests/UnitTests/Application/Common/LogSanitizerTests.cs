using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.Common;

[TestClass]
[TestCategory("Unit")]
public sealed class LogSanitizerTests
{
    [TestMethod]
    public void SanitizeText_WithNewlinesAndControlChars_ReplacesWithSpaces()
    {
        string expectedText = "Guest  [INF] forged line";

        string actualText = LogSanitizer.SanitizeText("Guest\r\n[INF] forged\tline");

        Assert.AreEqual(expectedText, actualText);
    }

    [TestMethod]
    public void SanitizeText_ExceedingMaxLength_TruncatesOutput()
    {
        string expectedText = "abcde";

        string actualText = LogSanitizer.SanitizeText("abcdefghij", 5);

        Assert.AreEqual(expectedText, actualText);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("   \r\n\t ")]
    public void SanitizeText_NullOrWhitespace_ReturnsEmptyPlaceholder(string? input)
    {
        string expectedText = "[empty]";

        string actualText = LogSanitizer.SanitizeText(input);

        Assert.AreEqual(expectedText, actualText);
    }

    [TestMethod]
    public void SanitizeText_NonPositiveMaxLength_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => _ = LogSanitizer.SanitizeText("abc", 0);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [TestMethod]
    public void MaskEmail_ValidAddress_MasksLocalPartAndPreservesDomain()
    {
        string expectedEmail = "p***@example.com";

        string actualEmail = LogSanitizer.MaskEmail("player.one@example.com");

        Assert.AreEqual(expectedEmail, actualEmail);
    }

    [TestMethod]
    [DataRow("not-an-email")]
    [DataRow("@example.com")]
    [DataRow("player@")]
    public void MaskEmail_MalformedAddress_ReturnsInvalidPlaceholder(string malformedEmail)
    {
        string expectedEmail = "[invalid-email]";

        string actualEmail = LogSanitizer.MaskEmail(malformedEmail);

        Assert.AreEqual(expectedEmail, actualEmail);
    }

    [TestMethod]
    public void MaskIdentifier_EmailAddress_MasksLocalPart()
    {
        string expectedIdentifier = "p***@example.com";

        string actualIdentifier = LogSanitizer.MaskIdentifier("player@example.com");

        Assert.AreEqual(expectedIdentifier, actualIdentifier);
    }

    [TestMethod]
    public void MaskIdentifier_UsernameWithControlChar_SanitizesControlChars()
    {
        string expectedIdentifier = "PlayerOne";

        string actualIdentifier = LogSanitizer.MaskIdentifier("PlayerOne\n");

        Assert.AreEqual(expectedIdentifier, actualIdentifier);
    }

    [TestMethod]
    public void MaskIdentifier_Whitespace_ReturnsEmptyPlaceholder()
    {
        string expectedIdentifier = "[empty]";

        string actualIdentifier = LogSanitizer.MaskIdentifier(" ");

        Assert.AreEqual(expectedIdentifier, actualIdentifier);
    }
}
