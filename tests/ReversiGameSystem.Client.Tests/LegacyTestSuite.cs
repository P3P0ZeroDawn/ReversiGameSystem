using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Client.Tests;

internal static class LegacyTestSuite
{
    internal static Action[] CreateTests()
    {
        List<Action> tests = new List<Action>();
        tests.AddRange(CreateAbandonMatchServiceTests());
        tests.AddRange(CreateAvailableMatchesServiceTests());
        tests.AddRange(CreateChatReadServiceTests());
        tests.AddRange(CreateChatServiceTests());
        tests.AddRange(CreateEmailPolicyTests());
        tests.AddRange(CreateFriendRemovalServiceTests());
        tests.AddRange(CreateFriendRequestResponseServiceTests());
        tests.AddRange(CreateFriendRequestServiceTests());
        tests.AddRange(CreateFriendRequestsServiceTests());
        tests.AddRange(CreateFriendsServiceTests());
        tests.AddRange(CreateGameMoveServiceTests());
        tests.AddRange(CreateGuestSessionServiceTests());
        tests.AddRange(CreateGuestUsernamePolicyTests());
        tests.AddRange(CreateInstructionsServiceTests());
        tests.AddRange(CreateJoinMatchServiceTests());
        tests.AddRange(CreateMatchCodePolicyTests());
        tests.AddRange(CreateMatchInformationServiceTests());
        tests.AddRange(CreateMatchSettingsServiceTests());
        tests.AddRange(CreateMovementHistoryServiceTests());
        tests.AddRange(CreatePasswordPolicyTests());
        tests.AddRange(CreatePlayerStatisticsTests());
        tests.AddRange(CreateReversiBoardTests());
        tests.AddRange(CreateTurnTimePolicyTests());
        tests.AddRange(CreateUsernamePolicyTests());
        return tests.ToArray();
    }

    private static Action[] CreateAbandonMatchServiceTests()
    {
        AbandonMatchServiceTests tests = new AbandonMatchServiceTests();
        return new Action[]
        {
            tests.TryAbandon_ActiveMatch_RecordsCompleteResult,
            tests.TryAbandon_JoinedPlayer_AwardsCreatorVictory,
            tests.TryAbandon_FinishedMatch_PreservesExistingResult,
        };
    }

    private static Action[] CreateAvailableMatchesServiceTests()
    {
        AvailableMatchesServiceTests tests = new AvailableMatchesServiceTests();
        return new Action[]
        {
            tests.FilterJoinable_MixedMatches_ReturnsOnlyOpenHumanMatches,
        };
    }

    private static Action[] CreateChatReadServiceTests()
    {
        ChatReadServiceTests tests = new ChatReadServiceTests();
        return new Action[]
        {
            tests.TryGetMessages_AuthorizedPlayer_SortsChronologically,
            tests.TryGetMessages_EmptyConversation_ReturnsEmpty,
            tests.MessageReceived_OpenAuthorizedChat_AddsOnlyConversationMessage,
        };
    }

    private static Action[] CreateChatServiceTests()
    {
        ChatServiceTests tests = new ChatServiceTests();
        return new Action[]
        {
            tests.Send_EmptyMessage_DoesNotStoreMessage,
            tests.Send_ValidMessage_TrimsAndStoresMessage,
            tests.Send_MessageAboveLimit_DoesNotStoreMessage,
            tests.Send_InactiveConversation_ReturnsUnauthorized,
            tests.Send_GuestSession_ReturnsUnauthorized,
            tests.Send_ProhibitedSpanishContent_RecordsInfringement,
            tests.Send_ProhibitedSubstringInsideWord_AllowsMessage,
            tests.Send_ThirdRecentInfringement_AppliesMute,
            tests.Send_AfterMuteExpires_StoresSafeMessage,
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

    private static Action[] CreateFriendRemovalServiceTests()
    {
        FriendRemovalServiceTests tests = new FriendRemovalServiceTests();
        return new Action[]
        {
            tests.Remove_ExistingFriendship_RemovesOnlyRelationship,
            tests.Remove_MissingFriendship_PreservesExistingFriendship,
            tests.Remove_ThirdPartyFriendship_DoesNotModifyRelationship,
        };
    }

    private static Action[] CreateFriendRequestResponseServiceTests()
    {
        FriendRequestResponseServiceTests tests = new FriendRequestResponseServiceTests();
        return new Action[]
        {
            tests.Respond_AcceptedRequest_CreatesFriendship,
            tests.Respond_RejectedRequest_DoesNotCreateFriendship,
            tests.Respond_ProcessedRequest_PreservesState,
            tests.Respond_DifferentRecipient_PreservesPendingRequest,
        };
    }

    private static Action[] CreateFriendRequestServiceTests()
    {
        FriendRequestServiceTests tests = new FriendRequestServiceTests();
        return new Action[]
        {
            tests.Search_Query_ExcludesRequesterAndInactiveAccounts,
            tests.Send_SameAccount_ReturnsCannotAddYourself,
            tests.Send_ExistingFriend_ReturnsFriendshipAlreadyExists,
            tests.Send_PendingPair_ReturnsAlreadyPending,
            tests.Send_ActiveAccount_CreatesExactlyOnePendingRequest,
            tests.Send_InactiveAccount_ReturnsAccountNotAvailable,
        };
    }

    private static Action[] CreateFriendRequestsServiceTests()
    {
        FriendRequestsServiceTests tests = new FriendRequestsServiceTests();
        return new Action[]
        {
            tests.TryGetPending_MixedRequests_FiltersAndSorts,
            tests.TryGetPending_NoReceivedRequests_ReturnsEmpty,
        };
    }

    private static Action[] CreateFriendsServiceTests()
    {
        FriendsServiceTests tests = new FriendsServiceTests();
        return new Action[]
        {
            tests.TryGetFriends_MixedRelationships_FiltersAndSorts,
            tests.TryGetFriends_ReversedRelationship_ReturnsFriend,
            tests.TryGetFriends_NoRelationships_ReturnsEmpty,
        };
    }

    private static Action[] CreateGameMoveServiceTests()
    {
        GameMoveServiceTests tests = new GameMoveServiceTests();
        return new Action[]
        {
            tests.TryMakeMove_ValidMove_RecordsAndAdvancesTurn,
            tests.TryMakeMove_OpponentHasNoMove_KeepsCurrentPlayerTurn,
            tests.TryMakeMove_InvalidPosition_PreservesSession,
            tests.TryMakeMove_WrongTurn_PreservesSession,
            tests.PrepareCurrentTurn_NoMove_PassesTurn,
            tests.PrepareCurrentTurn_NoMovesForEitherPlayer_FinishesMatch,
            tests.PrepareCurrentTurn_EqualFullBoard_FinishesWithDraw,
            tests.HandleTimeout_FirstTimeout_MakesAutomaticMove,
            tests.HandleTimeout_ThirdTimeout_AwardsOpponentVictory,
            tests.TryMakeSystemMove_ValidTurn_RecordsSystemOrigin,
        };
    }

    private static Action[] CreateGuestSessionServiceTests()
    {
        GuestSessionServiceTests tests = new GuestSessionServiceTests();
        return new Action[]
        {
            tests.TryCreate_ValidName_CreatesActiveTemporarySession,
            tests.TryCreate_DuplicateNameIgnoringCase_ReturnsAlreadyInUse,
            tests.End_ExistingSession_ReleasesName,
            tests.TryCreate_InvalidName_DoesNotCreateSession,
            tests.TryCreate_UnavailableStore_ReturnsCreationError,
        };
    }

    private static Action[] CreateGuestUsernamePolicyTests()
    {
        GuestUsernamePolicyTests tests = new GuestUsernamePolicyTests();
        return new Action[]
        {
            tests.Validate_ValidName_ReturnsNone,
            tests.Validate_WhitespaceOnly_ReturnsMissing,
            tests.Normalize_NameWithOuterWhitespace_ReturnsBaseName,
        };
    }

    private static Action[] CreateInstructionsServiceTests()
    {
        InstructionsServiceTests tests = new InstructionsServiceTests();
        return new Action[]
        {
            tests.TryLoadGeneral_CompleteResources_ReturnsContent,
            tests.TryLoadForMatch_CompleteResources_AddsContext,
            tests.TryLoadGeneral_MissingResource_ReturnsError,
            tests.TryLoadForMatch_InvalidFormat_ReturnsError,
        };
    }

    private static Action[] CreateJoinMatchServiceTests()
    {
        JoinMatchServiceTests tests = new JoinMatchServiceTests();
        return new Action[]
        {
            tests.TryJoinByCode_InvalidCode_DoesNotCreateSession,
            tests.TryJoinById_UnavailableMatch_DoesNotCreateSession,
            tests.TryJoinById_OwnMatch_DoesNotCreateSession,
            tests.TryJoinById_AvailableMatch_StartsSession,
            tests.TryJoinByCode_AvailableMatch_PreservesGuestSession,
        };
    }

    private static Action[] CreateMatchCodePolicyTests()
    {
        MatchCodePolicyTests tests = new MatchCodePolicyTests();
        return new Action[]
        {
            tests.TryNormalize_LowercaseCode_ReturnsUppercaseCode,
            tests.TryNormalize_IncorrectLength_ReturnsFalse,
            tests.TryNormalize_AmbiguousCharacters_ReturnsFalse,
        };
    }

    private static Action[] CreateMatchInformationServiceTests()
    {
        MatchInformationServiceTests tests = new MatchInformationServiceTests();
        return new Action[]
        {
            tests.TryGetHistory_MixedRecords_FiltersAndSorts,
            tests.TryGetHistory_NoPlayerMatches_ReturnsEmpty,
            tests.TryGetHistory_Record_PreservesSummaryFields,
        };
    }

    private static Action[] CreateMatchSettingsServiceTests()
    {
        MatchSettingsServiceTests tests = new MatchSettingsServiceTests();
        return new Action[]
        {
            tests.ChangeTurnTime_WaitingCreator_UpdatesOnlyTime,
            tests.ChangeTurnTime_StartedMatch_PreservesTime,
            tests.ChangeTurnTime_DifferentPlayer_PreservesTime,
            tests.ChangeTurnTime_InvalidValue_PreservesTime,
        };
    }

    private static Action[] CreateMovementHistoryServiceTests()
    {
        MovementHistoryServiceTests tests = new MovementHistoryServiceTests();
        return new Action[]
        {
            tests.TryGetMovements_UnorderedRecords_SortsBySequence,
            tests.TryGetMovements_NoRecords_ReturnsEmpty,
            tests.TryGetMovements_MissingMatch_ReturnsRetrievalError,
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

    private static Action[] CreatePlayerStatisticsTests()
    {
        PlayerStatisticsTests tests = new PlayerStatisticsTests();
        return new Action[]
        {
            tests.FromCompletedMatches_Empty_ReturnsZeros,
            tests.FromCompletedMatches_MixedResults_CountsEveryOutcome,
            tests.FromCompletedMatches_NoWins_ReturnsZeroWinRate,
        };
    }

    private static Action[] CreateReversiBoardTests()
    {
        ReversiBoardTests tests = new ReversiBoardTests();
        return new Action[]
        {
            tests.CreateInitial_HasStandardFourCenterDiscs,
            tests.CreateInitial_HasTwoDiscsOfEachColor,
            tests.GetValidMoves_InitialBlackTurn_ReturnsFourPositions,
            tests.TryApplyMove_ValidMove_FlipsBracketedPiece,
            tests.TryApplyMove_MultipleDirections_FlipsEveryLine,
            tests.FromRows_ValidSnapshot_ParsesEveryCell,
            tests.FromRows_InvalidCell_ThrowsArgumentException,
        };
    }

    private static Action[] CreateTurnTimePolicyTests()
    {
        TurnTimePolicyTests tests = new TurnTimePolicyTests();
        return new Action[]
        {
            tests.TryParse_DefaultValue_ReturnsTrue,
            tests.TryParse_MinimumAndMaximum_ReturnTrue,
            tests.TryParse_OutsideRange_ReturnsFalse,
            tests.TryParse_NonInteger_ReturnsFalse,
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
