using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Server.Tests;

internal static class LegacyTestSuite
{
    internal static Action[] CreateTests()
    {
        List<Action> tests = new List<Action>();
        tests.AddRange(CreateCreateAccountHandlerTests());
        tests.AddRange(CreateEmailPolicyTests());
        tests.AddRange(CreateLeaderboardApplicationServiceTests());
        tests.AddRange(CreateAccountAccessValidationTests());
        tests.AddRange(CreatePasswordPolicyTests());
        tests.AddRange(CreatePersistenceModelTests());
        tests.AddRange(CreatePlayerProfileApplicationServiceTests());
        tests.AddRange(CreateProfileImageStoreIntegrationTests());
        tests.AddRange(CreateUsernamePolicyTests());
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

    private static Action[] CreateEmailPolicyTests()
    {
        EmailPolicyTests tests = new EmailPolicyTests();
        return new Action[]
        {
            tests.Validate_OrdinaryAddress_ReturnsNone,
            tests.Validate_TaggedAddress_ReturnsNone,
            tests.Validate_InternationalizedDomain_ReturnsNone,
            tests.Validate_AddressAtMaximumLength_ReturnsNone,
            tests.Validate_AddressAboveMaximumLength_ReturnsTooLong,
            tests.Validate_MissingAtSign_ReturnsInvalidStructure,
            tests.Validate_MultipleAtSigns_ReturnsInvalidStructure,
            tests.Validate_ConsecutiveLocalDots_ReturnsInvalidLocalPart,
            tests.Validate_LocalPartAboveMaximum_ReturnsInvalidLocalPart,
            tests.Validate_AccentedLocalPart_ReturnsInvalidLocalPart,
            tests.Validate_DisplayName_ReturnsInvalidLocalPart,
            tests.Validate_SingleLabelDomain_ReturnsInvalidDomain,
            tests.Validate_HyphenAtDomainStart_ReturnsInvalidDomain,
            tests.Validate_ConsecutiveDomainDots_ReturnsInvalidDomain,
            tests.Validate_TrailingDomainDot_ReturnsInvalidDomain,
            tests.Validate_DomainLabelAboveMaximum_ReturnsInvalidDomain,
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

    private static Action[] CreatePasswordPolicyTests()
    {
        PasswordPolicyTests tests = new PasswordPolicyTests();
        return new Action[]
        {
            tests.Validate_SevenCharacters_ReturnsTooShort,
            tests.Validate_EightCharacters_ReturnsNone,
            tests.Validate_AsteriskAsSymbol_ReturnsNone,
            tests.Validate_UnicodeLettersAndDigits_ReturnsNone,
            tests.Validate_OneHundredTwentyEightCharacters_ReturnsNone,
            tests.Validate_OneHundredTwentyNineCharacters_ReturnsTooLong,
            tests.Validate_SupplementarySymbols_CountsCodePoints,
            tests.Validate_WithoutUppercase_ReturnsMissingUppercaseLetter,
            tests.Validate_WithoutLowercase_ReturnsMissingLowercaseLetter,
            tests.Validate_WithoutDigit_ReturnsMissingDigit,
            tests.Validate_WithoutSymbol_ReturnsMissingSymbol,
            tests.Validate_SpaceWithoutSymbol_ReturnsMissingSymbol,
            tests.Validate_ControlCharacter_ReturnsUnsupportedCharacter,
            tests.Validate_LineSeparator_ReturnsUnsupportedCharacter,
            tests.Validate_InvisibleFormatCharacter_ReturnsUnsupportedCharacter,
            tests.Validate_UnpairedSurrogate_ReturnsUnsupportedCharacter,
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
            tests.GetAsync_CurrentPlayer_ReturnsCalculatedStatistics,
            tests.GetAsync_PublicProfile_VerifiesCallerThenUsesTargetAccount,
            tests.GetAsync_InvalidInput_RejectsBeforeRepositoryAccess,
            tests.GetAsync_InvalidSession_PrecedesInvalidTargetAccount,
            tests.GetAsync_UnknownSession_RejectsBeforeRepositoryAccess,
            tests.GetAsync_InactiveCaller_DoesNotReadRequestedTarget,
            tests.GetAsync_MissingTarget_ReturnsAccountNotFound,
            tests.GetAsync_RepositoryFailure_PropagatesException,
            tests.ChangeUsernameAsync_ValidUsername_NormalizesAndUpdatesCaller,
            tests.ChangeUsernameAsync_UnavailableUsername_ReturnsExpectedStatus,
            tests.UpdateImageAsync_AssociationFailure_RemovesStoredImageAndPropagates,
        };
    }

    private static Action[] CreateProfileImageStoreIntegrationTests()
    {
        ProfileImageStoreIntegrationTests tests = new ProfileImageStoreIntegrationTests();
        return new Action[]
        {
            tests.SaveAndDelete_ValidImage_UsesRelativeUri,
            tests.TrySave_ValidAndInvalidImages_ReturnsExpectedOutcome,
        };
    }

    private static Action[] CreateUsernamePolicyTests()
    {
        UsernamePolicyTests tests = new UsernamePolicyTests();
        return new Action[]
        {
            tests.Validate_ValidUsername_ReturnsNone,
            tests.Validate_UsernameWithUnderscore_ReturnsNone,
            tests.Validate_EmptyUsername_ReturnsMissing,
            tests.Validate_TooShortUsername_ReturnsInvalidLength,
            tests.Validate_TooLongUsername_ReturnsInvalidLength,
            tests.Validate_LeadingUnderscore_ReturnsInvalidCharacters,
            tests.Validate_UsernameWithSpace_ReturnsInvalidCharacters,
        };
    }
}
