using System;
using System.Globalization;
using System.Resources;
using ReversiGameSystem.Chat;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests;

internal static class RemoteMatchMessagesTests
{
    internal static void MatchStatuses_SupportedCultures_HaveLocalizedMessages()
    {
        foreach (MatchStatus status in Enum.GetValues(typeof(MatchStatus)))
        {
            VerifyResource(RemoteMatchMessages.GetMessageResourceKey(status),
                status == MatchStatus.Success);
        }
    }

    internal static void ChatStatuses_SupportedCultures_HaveLocalizedMessages()
    {
        foreach (ChatStatus status in Enum.GetValues(typeof(ChatStatus)))
        {
            VerifyResource(RemoteMatchMessages.GetMessageResourceKey(status),
                status == ChatStatus.Success);
        }
    }

    internal static void MatchNotFound_ResolvesSpecificMessage()
    {
        TestAssert.AreEqual("RemoteMatch_NotFound",
            RemoteMatchMessages.GetMessageResourceKey(MatchStatus.MatchNotFound));
    }

    internal static void UnknownMatchStatus_ThrowsInsteadOfInventingRejection()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(GetUnknownMatchMessage);
    }

    internal static void UnknownChatStatus_ThrowsInsteadOfInventingRejection()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(GetUnknownChatMessage);
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
            TestAssert.AreEqual(string.Empty, resourceKey);
            return;
        }
        foreach (string cultureName in new[] { "en-US", "es-MX" })
        {
            string? message = new ResourceManager("ReversiGameSystem.Properties.Resources",
                typeof(App).Assembly).GetString(resourceKey,
                CultureInfo.GetCultureInfo(cultureName));
            TestAssert.AreEqual(false, string.IsNullOrWhiteSpace(message));
        }
    }
}
