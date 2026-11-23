using System.Globalization;
using System.Resources;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests;

internal static class LobbySettingsTests
{
    private static readonly string[] RequiredResourceKeys =
    {
        "MatchSettings_Title",
        "MatchSettings_Description",
        "MatchSettings_TurnTimeLabel",
        "MatchSettings_TurnTimeHint",
        "MatchSettings_SaveButton",
        "MatchSettings_InvalidTurnTimeTitle",
        "MatchSettings_InvalidTurnTimeMessage",
        "MatchSettings_PersistenceErrorTitle",
        "MatchSettings_PersistenceErrorMessage",
        "WaitingForOpponent_ModifySettingsButton",
        "WaitingForOpponent_TurnTimeFormat"
    };

    internal static void ChangeTurnDuration_UnreachableServer_ReturnsServiceUnavailable()
    {
        const long MatchId = 100L;
        const int TurnDurationSeconds = 60;
        using MatchLobbyClient client = MatchClientsTests.CreateLobby("test-token");

        MatchStatus status = client.ChangeTurnDuration(MatchId, TurnDurationSeconds)
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(MatchStatus.ServiceUnavailable, status);
    }

    internal static void RemoteMatchMessages_InvalidTurnDuration_ResolvesExpectedResourceKey()
    {
        const MatchStatus Status = MatchStatus.InvalidTurnDuration;

        string key = RemoteMatchMessages.GetMessageResourceKey(Status);

        TestAssert.AreEqual("MatchCreation_InvalidTurnTimeMessage", key);
    }

    internal static void LocalizationResources_MatchSettingsKeys_ExistInSupportedCultures()
    {
        ResourceManager resources = new ResourceManager(
            "ReversiGameSystem.Properties.Resources",
            typeof(App).Assembly);
        string[] cultures = { "en-US", "es-MX" };

        foreach (string culture in cultures)
        {
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo(culture);
            foreach (string key in RequiredResourceKeys)
            {
                string? localizedValue = resources.GetString(key, cultureInfo);
                TestAssert.AreEqual(false, string.IsNullOrWhiteSpace(localizedValue));
            }
        }
    }
}
