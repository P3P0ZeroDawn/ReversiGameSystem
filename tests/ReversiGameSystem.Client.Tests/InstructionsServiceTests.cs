using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class InstructionsServiceTests
{
    internal void TryLoadGeneral_CompleteResources_ReturnsContent()
    {
        // Arrange
        InstructionsService service = CreateService("{0} / {1} / {2}");

        // Act
        InstructionsLoadResult result = service.TryLoadGeneral(
            out InstructionsContent? content);

        // Assert
        TestAssert.AreEqual(InstructionsLoadResult.Success, result);
        TestAssert.AreEqual(true, content != null);
        InstructionsContent actualContent = TestAssert.IsNotNull(content);
        TestAssert.AreEqual("How to play", actualContent.Title);
        TestAssert.AreEqual("Rules", actualContent.Message);
    }

    internal void TryLoadForMatch_CompleteResources_AddsContext()
    {
        // Arrange
        InstructionsService service = CreateService("{0} / {1} / {2}");

        // Act
        InstructionsLoadResult result = service.TryLoadForMatch(
            "dark",
            40,
            out InstructionsContent? content);

        // Assert
        TestAssert.AreEqual(InstructionsLoadResult.Success, result);
        TestAssert.AreEqual("dark / 40 / Rules", TestAssert.IsNotNull(content).Message);
    }

    internal void TryLoadGeneral_MissingResource_ReturnsError()
    {
        // Arrange
        InstructionsService service = new InstructionsService(key =>
            key == "Instructions_Title" ? "How to play" : string.Empty);

        // Act
        InstructionsLoadResult result = service.TryLoadGeneral(out _);

        // Assert
        TestAssert.AreEqual(InstructionsLoadResult.ResourcesError, result);
    }

    internal void TryLoadForMatch_InvalidFormat_ReturnsError()
    {
        // Arrange
        InstructionsService service = CreateService("{3}");

        // Act
        InstructionsLoadResult result = service.TryLoadForMatch("dark", 40, out _);

        // Assert
        TestAssert.AreEqual(InstructionsLoadResult.ResourcesError, result);
    }

    private static InstructionsService CreateService(string contextFormat)
    {
        Dictionary<string, string> resources = new Dictionary<string, string>
        {
            { "Instructions_Title", "How to play" },
            { "Instructions_RulesMessage", "Rules" },
            { "Instructions_MatchContextFormat", contextFormat }
        };
        return new InstructionsService(key => resources.TryGetValue(key, out string value)
            ? value
            : string.Empty);
    }
}
