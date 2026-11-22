using System;
using System.Collections.Generic;
using System.Resources;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests;

internal sealed class InstructionsServiceTests
{
    internal void LoadGeneral_CompleteResources_ReturnsContent()
    {
        InstructionsService service = CreateService("{0} / {1} / {2}");
        InstructionsContent content = service.LoadGeneral();
        TestAssert.AreEqual("How to play", content.Title);
        TestAssert.AreEqual("Rules", content.Message);
    }

    internal void LoadForMatch_CompleteResources_AddsContext()
    {
        InstructionsService service = CreateService("{0} / {1} / {2}");
        InstructionsContent content = service.LoadForMatch("dark", 40);
        TestAssert.AreEqual("dark / 40 / Rules", content.Message);
    }

    internal void LoadGeneral_MissingResource_PropagatesFailure()
    {
        InstructionsService service = new InstructionsService(key => string.Empty);
        bool failed = false;
        try
        {
            service.LoadGeneral();
        }
        catch (MissingManifestResourceException)
        {
            failed = true;
        }
        TestAssert.AreEqual(true, failed);
    }

    internal void LoadForMatch_InvalidFormat_PropagatesFailure()
    {
        InstructionsService service = CreateService("{3}");
        bool failed = false;
        try
        {
            service.LoadForMatch("dark", 40);
        }
        catch (FormatException)
        {
            failed = true;
        }
        TestAssert.AreEqual(true, failed);
    }

    private static InstructionsService CreateService(string contextFormat)
    {
        Dictionary<string, string> resources = new Dictionary<string, string>
        {
            { "Instructions_Title", "How to play" },
            { "Instructions_RulesMessage", "Rules" },
            { "Instructions_MatchContextFormat", contextFormat }
        };
        return new InstructionsService(key => resources[key]);
    }
}
