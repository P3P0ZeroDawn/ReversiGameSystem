using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Server.Tests;

internal static class LegacyTestSuite
{
    internal static Action[] CreateTests()
    {
        List<Action> tests = new List<Action>();
        tests.AddRange(CreatePersistenceModelTests());
        tests.AddRange(CreatePlayerProfileApplicationServiceTests());
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

    private static Action[] CreatePlayerProfileApplicationServiceTests()
    {
        PlayerProfileApplicationServiceTests tests =
            new PlayerProfileApplicationServiceTests();
        return new Action[]
        {
            tests.GetProfile_CurrentPlayer_ReturnsCalculatedStatistics,
            tests.GetProfile_PublicProfile_VerifiesCallerThenUsesTargetAccount,
            tests.GetProfile_MissingSessionToken_RejectsBeforeRepositoryAccess,
            tests.GetProfile_InvalidTargetAccountId_RejectsBeforeRepositoryAccess,
            tests.GetProfile_InvalidSession_PrecedesInvalidTargetAccount,
            tests.GetProfile_UnknownSession_RejectsBeforeRepositoryAccess,
            tests.GetProfile_InactiveCaller_DoesNotReadRequestedTarget,
            tests.GetProfile_MissingTarget_ReturnsAccountNotFound,
            tests.GetProfile_RepositoryFailure_PropagatesException,
            tests.GetImage_SelfWithoutImage_QueriesImageReferenceWithoutLoadingProfile,
            tests.GetImage_PublicProfile_VerifiesCallerThenReadsTargetImage,
            tests.ChangeUsername_ValidUsername_NormalizesAndUpdatesCaller,
            tests.ChangeUsername_InvalidUsername_RejectsBeforeRepositoryAccess,
            tests.ChangeUsername_UnavailableUsername_ReturnsExpectedStatus,
            tests.UpdateImage_InactiveAccount_RemovesStoredImage,
            tests.UpdateImage_AssociationFailure_RemovesStoredImageAndPropagates,
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
