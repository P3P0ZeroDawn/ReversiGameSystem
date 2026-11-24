using System;
using System.Globalization;
using System.Resources;
using ReversiGameSystem.Chat;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class RemoteMatchMessagesTests
{
    [TestMethod]
    public void MatchStatuses_SupportedCultures_HaveLocalizedMessages()
    {
        foreach (MatchStatus status in Enum.GetValues(typeof(MatchStatus)))
        {
            VerifyResource(RemoteMatchMessages.GetMessageResourceKey(status),
                status == MatchStatus.Success);
        }
    }

    [TestMethod]
    public void ChatStatuses_SupportedCultures_HaveLocalizedMessages()
    {
        foreach (ChatStatus status in Enum.GetValues(typeof(ChatStatus)))
        {
            VerifyResource(RemoteMatchMessages.GetMessageResourceKey(status),
                status == ChatStatus.Success);
        }
    }

    [TestMethod]
    public void MatchNotFound_ResolvesSpecificMessage()
    {
        Assert.AreEqual("RemoteMatch_NotFound",
            RemoteMatchMessages.GetMessageResourceKey(MatchStatus.MatchNotFound));
    }

    [TestMethod]
    public void UnknownMatchStatus_ThrowsInsteadOfInventingRejection()
    {
        Assert.Throws<ArgumentOutOfRangeException>(GetUnknownMatchMessage);
    }

    [TestMethod]
    public void UnknownChatStatus_ThrowsInsteadOfInventingRejection()
    {
        Assert.Throws<ArgumentOutOfRangeException>(GetUnknownChatMessage);
    }

    private static void GetUnknownMatchMessage()
    {
        RemoteMatchMessages.GetMessageResourceKey((MatchStatus)int.MaxValue);
    }

    private static void GetUnknownChatMessage()
    {
        RemoteMatchMessages.GetMessageResourceKey((ChatStatus)int.MaxValue);
    }

    private static void VerifyResource(string resourceKey, bool success)
    {
        if (success)
        {
            Assert.AreEqual(string.Empty, resourceKey);
            return;
        }
        foreach (string cultureName in new[] { "en-US", "es-MX" })
        {
            string? message = new ResourceManager("ReversiGameSystem.Properties.Resources",
                typeof(App).Assembly).GetString(resourceKey,
                CultureInfo.GetCultureInfo(cultureName));
            Assert.AreEqual(false, string.IsNullOrWhiteSpace(message));
        }
    }
}
