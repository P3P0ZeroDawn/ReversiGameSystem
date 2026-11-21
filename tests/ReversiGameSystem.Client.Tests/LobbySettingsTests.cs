using System.Globalization;
using System.Resources;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests;

internal sealed class LobbySettingsTests
{
    private static readonly string[] s_requiredKeys =
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

    internal void ChangeTurnDuration_UnreachableServer_ReturnsServiceUnavailable()
    {
        // Arrange
        const long MatchId = 100L;
        const int TurnDurationSeconds = 60;
        using MatchLobbyClient client = MatchClientsTests.CreateLobby("test-token");

        // Act
        MatchStatus status = client.ChangeTurnDuration(MatchId, TurnDurationSeconds)
            .GetAwaiter()
            .GetResult();

        // Assert
        TestAssert.AreEqual(MatchStatus.ServiceUnavailable, status);
    }

    internal void RemoteMatchMessages_InvalidTurnDuration_ResolvesExpectedResourceKey()
    {
        // Arrange
        const MatchStatus Status = MatchStatus.InvalidTurnDuration;

        // Act
        string key = RemoteMatchMessages.Key(Status);

        // Assert
        TestAssert.AreEqual("MatchCreation_InvalidTurnTimeMessage", key);
    }

    internal void LocalizationResources_MatchSettingsKeys_ExistInSupportedCultures()
    {
        // Arrange
        ResourceManager resources = new ResourceManager(
            "ReversiGameSystem.Properties.Resources",
            typeof(App).Assembly);
        string[] cultures = { "en-US", "es-MX" };

        // Act & Assert
        foreach (string culture in cultures)
        {
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo(culture);
            foreach (string key in s_requiredKeys)
            {
                string? localizedValue = resources.GetString(key, cultureInfo);
                TestAssert.AreEqual(false, string.IsNullOrWhiteSpace(localizedValue));
            }
        }
    }
}
