using System;
using System.Collections.Generic;
using System.Resources;
using ReversiGameSystem.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class InstructionsServiceTests
{
    [TestMethod]
    public void LoadGeneral_CompleteResources_ReturnsContent()
    {
        InstructionsService service = CreateService("{0} / {1} / {2}");
        InstructionsContent content = service.LoadGeneral();
        Assert.AreEqual("How to play", content.Title);
        Assert.AreEqual("Rules", content.Message);
    }

    [TestMethod]
    public void LoadForMatch_CompleteResources_AddsContext()
    {
        InstructionsService service = CreateService("{0} / {1} / {2}");
        InstructionsContent content = service.LoadForMatch("dark", 40);
        Assert.AreEqual("dark / 40 / Rules", content.Message);
    }

    [TestMethod]
    public void LoadGeneral_MissingResource_PropagatesFailure()
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
        Assert.AreEqual(true, failed);
    }

    [TestMethod]
    public void LoadForMatch_InvalidFormat_PropagatesFailure()
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
        Assert.AreEqual(true, failed);
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
