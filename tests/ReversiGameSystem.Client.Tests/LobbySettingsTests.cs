using System.Globalization;
using System.Resources;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests;

/// <summary>
/// Verifies client-side behavior and localization for modifying lobby turn duration.
/// </summary>
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

    /// <summary>
    /// Verifies that ChangeTurnDuration returns ServiceUnavailable when the server is unreachable.
    /// </summary>
    internal void ChangeTurnDuration_UnreachableServer_ReturnsServiceUnavailable()
    {
        // Arrange
        const long MatchId = 100L;
        const int TurnDurationSeconds = 60;
        using RemoteMatchClient client = RemoteMatchClientTests.Create("test-token");

        // Act
        MatchResult<LobbySnapshot> result = client.ChangeTurnDuration(MatchId, TurnDurationSeconds)
            .GetAwaiter()
            .GetResult();

        // Assert
        TestAssert.AreEqual(MatchStatus.ServiceUnavailable, result.Status);
        TestAssert.AreEqual(null, result.Value);
    }

    /// <summary>
    /// Verifies that InvalidTurnDuration resolves to the localized turn-duration error key.
    /// </summary>
    internal void RemoteMatchMessages_InvalidTurnDuration_ResolvesExpectedResourceKey()
    {
        // Arrange
        const MatchStatus Status = MatchStatus.InvalidTurnDuration;

        // Act
        string key = RemoteMatchMessages.Key(Status);

        // Assert
        TestAssert.AreEqual("MatchCreation_InvalidTurnTimeMessage", key);
    }

    /// <summary>
    /// Verifies that all match-settings localization keys exist in all supported cultures.
    /// </summary>
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
