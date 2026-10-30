using System.Globalization;
using System.Resources;

namespace ReversiGameSystem.Client.Tests;

internal static class FriendshipLocalizationTests
{
    internal static void Presence_SupportedCultures_ContainStatusAndRecoveryMessages()
    {
        ResourceManager resources = new("ReversiGameSystem.Properties.Resources",
            typeof(ReversiGameSystem.App).Assembly);
        foreach (string culture in new[] { "en-US", "es-MX" })
        {
            foreach (string key in new[]
            {
                "Friends_Online", "Friends_Offline", "Friends_Refresh", "Friends_Loading",
                "Friends_PresenceUnavailable", "Friends_SessionUnavailable"
            })
            {
                TestAssert.AreEqual(false, string.IsNullOrWhiteSpace(
                    resources.GetString(key, CultureInfo.GetCultureInfo(culture))));
            }
        }
        TestAssert.AreEqual("En línea", resources.GetString("Friends_Online",
            CultureInfo.GetCultureInfo("es-MX")));
        TestAssert.AreEqual("Online", resources.GetString("Friends_Online",
            CultureInfo.GetCultureInfo("en-US")));
    }
}
