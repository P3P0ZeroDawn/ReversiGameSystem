using System.Globalization;
using System.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class FriendshipLocalizationTests
{
    [TestMethod]
    public void Presence_SupportedCultures_ContainStatusAndRecoveryMessages()
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
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(
                    resources.GetString(key, CultureInfo.GetCultureInfo(culture))));
            }
        }
        Assert.AreEqual("En línea", resources.GetString("Friends_Online",
            CultureInfo.GetCultureInfo("es-MX")));
        Assert.AreEqual("Online", resources.GetString("Friends_Online",
            CultureInfo.GetCultureInfo("en-US")));
    }
}
