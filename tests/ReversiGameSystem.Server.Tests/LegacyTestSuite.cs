using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Server.Tests;

internal static class LegacyTestSuite
{
    internal static Action[] CreateTests()
    {
        List<Action> tests = new List<Action>();
        tests.AddRange(CreateCreateAccountHandlerTests());
        tests.AddRange(CreateLeaderboardApplicationServiceTests());
        tests.AddRange(CreateAccountAccessValidationTests());
        tests.AddRange(CreatePersistenceModelTests());
        tests.AddRange(CreatePlayerProfileApplicationServiceTests());
        tests.AddRange(CreateProfileImageStoreIntegrationTests());
        return tests.ToArray();
    }

    private static Action[] CreateCreateAccountHandlerTests()
    {
        CreateAccountHandlerTests tests = new CreateAccountHandlerTests();
        return new Action[]
        {
            tests.ExecuteAsync_ValidRequest_StoresAccount,
            tests.ExecuteAsync_MissingRequiredValue_ReturnsInvalidRequest,
            tests.ExecuteAsync_InvalidFields_ReturnSpecificOutcomes,
            tests.ExecuteAsync_InvalidProfileImage_ReturnsInvalidRequest,
            tests.ExecuteAsync_DuplicateUsername_RemovesStoredImage,
            tests.ExecuteAsync_UnsupportedLocale_ReturnsInvalidRequest,
            tests.PasswordHasher_HashAndVerify_UsesSaltedPbkdf2,
        };
    }

    private static Action[] CreateLeaderboardApplicationServiceTests()
    {
        LeaderboardApplicationServiceTests tests =
            new LeaderboardApplicationServiceTests();
        return new Action[]
        {
            tests.Get_ValidRequest_RanksAndLimitsEntries,
            tests.Get_InvalidMaximum_RejectsBeforeRepositoryAccess,
            tests.Get_EmptyRepository_ReturnsSuccessfulEmptyList,
            tests.Get_BoundaryMaximums_AcceptsOneAndOneHundred,
            tests.Get_RepositoryFailure_PropagatesException,
        };
    }

    private static Action[] CreateAccountAccessValidationTests()
    {
        AccountAccessValidationTests tests = new AccountAccessValidationTests();
        return new Action[]
        {
            tests.ValidateCredentials_ValidCredentials_NormalizesAndReturnsActiveAccount,
            tests.ValidateCredentials_MissingCredentials_RejectsBeforeLookup,
            tests.ValidateCredentials_UnknownAccount_ReturnsInvalidCredentials,
            tests.ValidateCredentials_WrongPassword_ReturnsInvalidCredentials,
            tests.ValidateCredentials_InactiveAccount_ReturnsInactiveAccount,
            tests.ValidateCredentials_UnavailableStore_PropagatesTechnicalFailure,
            tests.SessionStore_CreateFindAndExpire_EnforcesLifetime,
        };
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

    private static Action[] CreateProfileImageStoreIntegrationTests()
    {
        ProfileImageStoreIntegrationTests tests = new ProfileImageStoreIntegrationTests();
        return new Action[]
        {
            tests.SaveAndDelete_ValidImage_UsesRelativeUri,
            tests.SaveImage_ValidAndInvalidImages_ReturnsExpectedOutcome,
        };
    }
}
