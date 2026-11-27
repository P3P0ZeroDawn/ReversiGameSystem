using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Server.Tests;

internal static class LegacyTestSuite
{
    internal static Action[] CreateTests()
    {
        List<Action> tests = new List<Action>();
        tests.AddRange(CreatePersistenceModelTests());
        tests.AddRange(CreatePlayerImageRepositoryTests());
        return tests.ToArray();
    }

    private static Action[] CreatePersistenceModelTests()
    {
        PersistenceModelTests tests = new PersistenceModelTests();
        return new Action[]
        {
            tests.CreateContext_ValidEnvironment_LoadsEmbeddedModel,
        };
    }

    private static Action[] CreatePlayerImageRepositoryTests()
    {
        PlayerImageRepositoryTests tests = new PlayerImageRepositoryTests();
        return new Action[]
        {
            tests.SaveImage_WideTransparentImage_CropsCenterAndCompositesWhite,
            tests.SaveImage_Background_ResizesAndDoesNotUpscale,
            tests.ReadImage_LegacyFormats_PreservesMediaType,
            tests.DeleteImage_TraversalReference_RejectsAndPreservesFile,
            tests.SaveImage_DetailedBackgroundAboveSoftTarget_AcceptsImage,
            tests.SaveAndDelete_ValidImage_UsesRelativeUri,
            tests.SaveImage_ValidAndInvalidImages_ReturnsExpectedOutcome,
        };
    }
}
