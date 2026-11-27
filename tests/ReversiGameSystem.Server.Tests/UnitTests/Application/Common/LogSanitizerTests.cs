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
        string result = LogSanitizer.SanitizeText("Guest\r\n[INF] forged\tline");

        Assert.AreEqual("Guest  [INF] forged line", result);
    }

    [TestMethod]
    public void SanitizeText_ExceedingMaxLength_TruncatesOutput()
    {
        string result = LogSanitizer.SanitizeText("abcdefghij", 5);

        Assert.AreEqual("abcde", result);
    }

    [TestMethod]
    public void SanitizeText_NullOrWhitespace_ReturnsEmptyPlaceholder()
    {
        Assert.AreEqual("[empty]", LogSanitizer.SanitizeText(null));
        Assert.AreEqual("[empty]", LogSanitizer.SanitizeText("   \r\n\t "));
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
        string result = LogSanitizer.MaskEmail("player.one@example.com");

        Assert.AreEqual("p***@example.com", result);
    }

    [TestMethod]
    public void MaskEmail_MalformedAddress_ReturnsInvalidPlaceholder()
    {
        Assert.AreEqual("[invalid-email]", LogSanitizer.MaskEmail("not-an-email"));
        Assert.AreEqual("[invalid-email]", LogSanitizer.MaskEmail("@example.com"));
        Assert.AreEqual("[invalid-email]", LogSanitizer.MaskEmail("player@"));
    }

    [TestMethod]
    public void MaskIdentifier_EmailVsUsername_AppliesAppropriateRedaction()
    {
        Assert.AreEqual("p***@example.com", LogSanitizer.MaskIdentifier("player@example.com"));
        Assert.AreEqual("PlayerOne", LogSanitizer.MaskIdentifier("PlayerOne\n"));
        Assert.AreEqual("[empty]", LogSanitizer.MaskIdentifier(" "));
    }
}
