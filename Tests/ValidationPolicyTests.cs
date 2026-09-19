using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Tests;

internal static class ValidationPolicyTestRunner
{
    private static int Main()
    {
        EmailPolicyTests emailTests = new EmailPolicyTests();
        PasswordPolicyTests passwordTests = new PasswordPolicyTests();
        UsernamePolicyTests usernameTests = new UsernamePolicyTests();
        GuestUsernamePolicyTests guestUsernameTests = new GuestUsernamePolicyTests();
        GuestSessionServiceTests guestSessionTests = new GuestSessionServiceTests();
        PlayerStatisticsTests statisticsTests = new PlayerStatisticsTests();
        TurnTimePolicyTests turnTimeTests = new TurnTimePolicyTests();
        ReversiBoardTests boardTests = new ReversiBoardTests();
        AvailableMatchesServiceTests availableMatchesTests = new AvailableMatchesServiceTests();
        MatchSettingsServiceTests matchSettingsTests = new MatchSettingsServiceTests();
        MatchCodePolicyTests matchCodeTests = new MatchCodePolicyTests();
        JoinMatchServiceTests joinMatchTests = new JoinMatchServiceTests();
        GameMoveServiceTests gameMoveTests = new GameMoveServiceTests();
        AbandonMatchServiceTests abandonMatchTests = new AbandonMatchServiceTests();
        MatchInformationServiceTests matchInformationTests =
            new MatchInformationServiceTests();
        MovementHistoryServiceTests movementHistoryTests =
            new MovementHistoryServiceTests();
        InstructionsServiceTests instructionsTests = new InstructionsServiceTests();
        FriendRequestServiceTests friendRequestTests = new FriendRequestServiceTests();
        FriendRequestsServiceTests friendRequestsTests = new FriendRequestsServiceTests();
        FriendRequestResponseServiceTests responseTests =
            new FriendRequestResponseServiceTests();
        FriendsServiceTests friendsTests = new FriendsServiceTests();
        FriendRemovalServiceTests removalTests = new FriendRemovalServiceTests();
        ChatServiceTests chatTests = new ChatServiceTests();
        ChatReadServiceTests chatReadTests = new ChatReadServiceTests();
        Action[] tests =
        {
            emailTests.Validate_OrdinaryAddress_ReturnsNone,
            emailTests.Validate_TaggedAddress_ReturnsNone,
            emailTests.Validate_InternationalizedDomain_ReturnsNone,
            emailTests.Validate_AddressAtMaximumLength_ReturnsNone,
            emailTests.Validate_AddressAboveMaximumLength_ReturnsTooLong,
            emailTests.Validate_MissingAtSign_ReturnsInvalidStructure,
            emailTests.Validate_MultipleAtSigns_ReturnsInvalidStructure,
            emailTests.Validate_ConsecutiveLocalDots_ReturnsInvalidLocalPart,
            emailTests.Validate_LocalPartAboveMaximum_ReturnsInvalidLocalPart,
            emailTests.Validate_AccentedLocalPart_ReturnsInvalidLocalPart,
            emailTests.Validate_DisplayName_ReturnsInvalidLocalPart,
            emailTests.Validate_SingleLabelDomain_ReturnsInvalidDomain,
            emailTests.Validate_HyphenAtDomainStart_ReturnsInvalidDomain,
            emailTests.Validate_ConsecutiveDomainDots_ReturnsInvalidDomain,
            emailTests.Validate_TrailingDomainDot_ReturnsInvalidDomain,
            emailTests.Validate_DomainLabelAboveMaximum_ReturnsInvalidDomain,
            passwordTests.Validate_SevenCharacters_ReturnsTooShort,
            passwordTests.Validate_EightCharacters_ReturnsNone,
            passwordTests.Validate_AsteriskAsSymbol_ReturnsNone,
            passwordTests.Validate_UnicodeLettersAndDigits_ReturnsNone,
            passwordTests.Validate_OneHundredTwentyEightCharacters_ReturnsNone,
            passwordTests.Validate_OneHundredTwentyNineCharacters_ReturnsTooLong,
            passwordTests.Validate_SupplementarySymbols_CountsCodePoints,
            passwordTests.Validate_WithoutUppercase_ReturnsMissingUppercaseLetter,
            passwordTests.Validate_WithoutLowercase_ReturnsMissingLowercaseLetter,
            passwordTests.Validate_WithoutDigit_ReturnsMissingDigit,
            passwordTests.Validate_WithoutSymbol_ReturnsMissingSymbol,
            passwordTests.Validate_SpaceWithoutSymbol_ReturnsMissingSymbol,
            passwordTests.Validate_ControlCharacter_ReturnsUnsupportedCharacter,
            passwordTests.Validate_LineSeparator_ReturnsUnsupportedCharacter,
            passwordTests.Validate_InvisibleFormatCharacter_ReturnsUnsupportedCharacter,
            passwordTests.Validate_UnpairedSurrogate_ReturnsUnsupportedCharacter,
            usernameTests.Validate_ValidUsername_ReturnsNone,
            usernameTests.Validate_UsernameWithUnderscore_ReturnsNone,
            usernameTests.Validate_EmptyUsername_ReturnsMissing,
            usernameTests.Validate_TooShortUsername_ReturnsInvalidLength,
            usernameTests.Validate_TooLongUsername_ReturnsInvalidLength,
            usernameTests.Validate_LeadingUnderscore_ReturnsInvalidCharacters,
            usernameTests.Validate_UsernameWithSpace_ReturnsInvalidCharacters,
            guestUsernameTests.Validate_ValidName_ReturnsNone,
            guestUsernameTests.Validate_WhitespaceOnly_ReturnsMissing,
            guestUsernameTests.Normalize_NameWithOuterWhitespace_ReturnsBaseName,
            guestSessionTests.TryCreate_ValidName_CreatesActiveTemporarySession,
            guestSessionTests.TryCreate_DuplicateNameIgnoringCase_ReturnsAlreadyInUse,
            guestSessionTests.End_ExistingSession_ReleasesName,
            guestSessionTests.TryCreate_InvalidName_DoesNotCreateSession,
            guestSessionTests.TryCreate_UnavailableStore_ReturnsCreationError,
            statisticsTests.FromCompletedMatches_Empty_ReturnsZeros,
            statisticsTests.FromCompletedMatches_MixedResults_CountsEveryOutcome,
            statisticsTests.FromCompletedMatches_NoWins_ReturnsZeroWinRate,
            turnTimeTests.TryParse_DefaultValue_ReturnsTrue,
            turnTimeTests.TryParse_MinimumAndMaximum_ReturnTrue,
            turnTimeTests.TryParse_OutsideRange_ReturnsFalse,
            turnTimeTests.TryParse_NonInteger_ReturnsFalse,
            boardTests.CreateInitial_HasStandardFourCenterDiscs,
            boardTests.CreateInitial_HasTwoDiscsOfEachColor,
            boardTests.GetValidMoves_InitialBlackTurn_ReturnsFourPositions,
            boardTests.TryApplyMove_ValidMove_FlipsBracketedPiece,
            boardTests.TryApplyMove_MultipleDirections_FlipsEveryLine,
            boardTests.FromRows_ValidSnapshot_ParsesEveryCell,
            boardTests.FromRows_InvalidCell_ThrowsArgumentException,
            availableMatchesTests.FilterJoinable_MixedMatches_ReturnsOnlyOpenHumanMatches,
            matchSettingsTests.ChangeTurnTime_WaitingCreator_UpdatesOnlyTime,
            matchSettingsTests.ChangeTurnTime_StartedMatch_PreservesTime,
            matchSettingsTests.ChangeTurnTime_DifferentPlayer_PreservesTime,
            matchSettingsTests.ChangeTurnTime_InvalidValue_PreservesTime,
            matchCodeTests.TryNormalize_LowercaseCode_ReturnsUppercaseCode,
            matchCodeTests.TryNormalize_IncorrectLength_ReturnsFalse,
            matchCodeTests.TryNormalize_AmbiguousCharacters_ReturnsFalse,
            joinMatchTests.TryJoinByCode_InvalidCode_DoesNotCreateSession,
            joinMatchTests.TryJoinById_UnavailableMatch_DoesNotCreateSession,
            joinMatchTests.TryJoinById_OwnMatch_DoesNotCreateSession,
            joinMatchTests.TryJoinById_AvailableMatch_StartsSession,
            joinMatchTests.TryJoinByCode_AvailableMatch_PreservesGuestSession,
            gameMoveTests.TryMakeMove_ValidMove_RecordsAndAdvancesTurn,
            gameMoveTests.TryMakeMove_OpponentHasNoMove_KeepsCurrentPlayerTurn,
            gameMoveTests.TryMakeMove_InvalidPosition_PreservesSession,
            gameMoveTests.TryMakeMove_WrongTurn_PreservesSession,
            gameMoveTests.PrepareCurrentTurn_NoMove_PassesTurn,
            gameMoveTests.PrepareCurrentTurn_NoMovesForEitherPlayer_FinishesMatch,
            gameMoveTests.PrepareCurrentTurn_EqualFullBoard_FinishesWithDraw,
            gameMoveTests.HandleTimeout_FirstTimeout_MakesAutomaticMove,
            gameMoveTests.HandleTimeout_ThirdTimeout_AwardsOpponentVictory,
            gameMoveTests.TryMakeSystemMove_ValidTurn_RecordsSystemOrigin,
            abandonMatchTests.TryAbandon_ActiveMatch_RecordsCompleteResult,
            abandonMatchTests.TryAbandon_JoinedPlayer_AwardsCreatorVictory,
            abandonMatchTests.TryAbandon_FinishedMatch_PreservesExistingResult,
            matchInformationTests.TryGetHistory_MixedRecords_FiltersAndSorts,
            matchInformationTests.TryGetHistory_NoPlayerMatches_ReturnsEmpty,
            matchInformationTests.TryGetHistory_Record_PreservesSummaryFields,
            movementHistoryTests.TryGetMovements_UnorderedRecords_SortsBySequence,
            movementHistoryTests.TryGetMovements_NoRecords_ReturnsEmpty,
            movementHistoryTests.TryGetMovements_MissingMatch_ReturnsRetrievalError,
            instructionsTests.TryLoadGeneral_CompleteResources_ReturnsContent,
            instructionsTests.TryLoadForMatch_CompleteResources_AddsContext,
            instructionsTests.TryLoadGeneral_MissingResource_ReturnsError,
            instructionsTests.TryLoadForMatch_InvalidFormat_ReturnsError,
            friendRequestTests.Search_Query_ExcludesRequesterAndInactiveAccounts,
            friendRequestTests.Send_SameAccount_ReturnsCannotAddYourself,
            friendRequestTests.Send_ExistingFriend_ReturnsFriendshipAlreadyExists,
            friendRequestTests.Send_PendingPair_ReturnsAlreadyPending,
            friendRequestTests.Send_ActiveAccount_CreatesExactlyOnePendingRequest,
            friendRequestTests.Send_InactiveAccount_ReturnsAccountNotAvailable,
            friendRequestsTests.TryGetPending_MixedRequests_FiltersAndSorts,
            friendRequestsTests.TryGetPending_NoReceivedRequests_ReturnsEmpty,
            responseTests.Respond_AcceptedRequest_CreatesFriendship,
            responseTests.Respond_RejectedRequest_DoesNotCreateFriendship,
            responseTests.Respond_ProcessedRequest_PreservesState,
            responseTests.Respond_DifferentRecipient_PreservesPendingRequest,
            friendsTests.TryGetFriends_MixedRelationships_FiltersAndSorts,
            friendsTests.TryGetFriends_ReversedRelationship_ReturnsFriend,
            friendsTests.TryGetFriends_NoRelationships_ReturnsEmpty,
            removalTests.Remove_ExistingFriendship_RemovesOnlyRelationship,
            removalTests.Remove_MissingFriendship_PreservesExistingFriendship,
            removalTests.Remove_ThirdPartyFriendship_DoesNotModifyRelationship,
            chatTests.Send_EmptyMessage_DoesNotStoreMessage,
            chatTests.Send_ValidMessage_TrimsAndStoresMessage,
            chatTests.Send_MessageAboveLimit_DoesNotStoreMessage,
            chatTests.Send_InactiveConversation_ReturnsUnauthorized,
            chatTests.Send_GuestSession_ReturnsUnauthorized,
            chatTests.Send_ProhibitedSpanishContent_RecordsInfringement,
            chatTests.Send_ProhibitedSubstringInsideWord_AllowsMessage,
            chatTests.Send_ThirdRecentInfringement_AppliesMute,
            chatTests.Send_AfterMuteExpires_StoresSafeMessage,
            chatReadTests.TryGetMessages_AuthorizedPlayer_SortsChronologically,
            chatReadTests.TryGetMessages_EmptyConversation_ReturnsEmpty,
            chatReadTests.MessageReceived_OpenAuthorizedChat_AddsOnlyConversationMessage
        };

        try
        {
            foreach (Action test in tests)
            {
                test();
            }

            Console.WriteLine($"Passed {tests.Length} validation policy tests.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}

internal sealed class FriendRequestServiceTests
{
    internal void Search_Query_ExcludesRequesterAndInactiveAccounts()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendSearchResult result = service.Search(
            "Ana",
            out IReadOnlyList<FriendCandidate> candidates);

        // Assert
        TestAssert.AreEqual(FriendSearchResult.Success, result);
        TestAssert.AreEqual(1, candidates.Count);
        TestAssert.AreEqual("Ana", candidates[0].Username);
    }

    internal void Send_SameAccount_ReturnsCannotAddYourself()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult result = service.Send("pedro");

        // Assert
        TestAssert.AreEqual(FriendRequestResult.CannotAddYourself, result);
    }

    internal void Send_ExistingFriend_ReturnsFriendshipAlreadyExists()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult result = service.Send("EmiPan");

        // Assert
        TestAssert.AreEqual(FriendRequestResult.FriendshipAlreadyExists, result);
    }

    internal void Send_PendingPair_ReturnsAlreadyPending()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult result = service.Send("Emilio");

        // Assert
        TestAssert.AreEqual(FriendRequestResult.FriendRequestAlreadyPending, result);
    }

    internal void Send_ActiveAccount_CreatesExactlyOnePendingRequest()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult firstResult = service.Send("Ana");
        FriendRequestResult secondResult = service.Send("Ana");

        // Assert
        TestAssert.AreEqual(FriendRequestResult.Success, firstResult);
        TestAssert.AreEqual(
            FriendRequestResult.FriendRequestAlreadyPending,
            secondResult);
    }

    internal void Send_InactiveAccount_ReturnsAccountNotAvailable()
    {
        // Arrange
        FriendRequestService service = CreateService();

        // Act
        FriendRequestResult result = service.Send("Luis");

        // Assert
        TestAssert.AreEqual(FriendRequestResult.AccountNotAvailable, result);
    }

    private static FriendRequestService CreateService()
    {
        string friendship = FriendshipStore.CreateRelationshipKey("Pedro", "EmiPan");
        FriendshipStore store = new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Ana"),
                new FriendCandidate("EmiPan"),
                new FriendCandidate("Emilio"),
                new FriendCandidate("Luis", false)
            },
            new[] { friendship },
            new[]
            {
                new FriendshipRequest(
                    "pending",
                    "Emilio",
                    "Pedro",
                    new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc))
            });
        return new FriendRequestService("Pedro", store);
    }
}

internal sealed class FriendRequestsServiceTests
{
    internal void TryGetPending_MixedRequests_FiltersAndSorts()
    {
        // Arrange
        DateTime olderDate = new DateTime(
            2026,
            9,
            17,
            12,
            0,
            0,
            DateTimeKind.Utc);
        DateTime newerDate = olderDate.AddHours(3);
        FriendshipStore store = new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Ana"),
                new FriendCandidate("Luis"),
                new FriendCandidate("Luna")
            },
            friendRequests: new[]
            {
                new FriendshipRequest("older", "Ana", "Pedro", olderDate),
                new FriendshipRequest("newer", "Luis", "pedro", newerDate),
                new FriendshipRequest("outgoing", "Pedro", "Luna", newerDate),
                new FriendshipRequest(
                    "accepted",
                    "Luna",
                    "Pedro",
                    newerDate,
                    FriendshipRequestState.Accepted)
            });
        FriendRequestsService service = new FriendRequestsService("Pedro", store);

        // Act
        FriendRequestsRetrievalResult result = service.TryGetPending(
            out IReadOnlyList<FriendshipRequest> requests);

        // Assert
        TestAssert.AreEqual(FriendRequestsRetrievalResult.Success, result);
        TestAssert.AreEqual(2, requests.Count);
        TestAssert.AreEqual("newer", requests[0].Id);
        TestAssert.AreEqual("Luis", requests[0].SenderUsername);
        TestAssert.AreEqual("older", requests[1].Id);
    }

    internal void TryGetPending_NoReceivedRequests_ReturnsEmpty()
    {
        // Arrange
        FriendshipStore store = new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Ana")
            },
            friendRequests: new[]
            {
                new FriendshipRequest(
                    "outgoing",
                    "Pedro",
                    "Ana",
                    new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc))
            });
        FriendRequestsService service = new FriendRequestsService("Pedro", store);

        // Act
        FriendRequestsRetrievalResult result = service.TryGetPending(
            out IReadOnlyList<FriendshipRequest> requests);

        // Assert
        TestAssert.AreEqual(FriendRequestsRetrievalResult.Success, result);
        TestAssert.AreEqual(0, requests.Count);
    }
}

internal sealed class FriendRequestResponseServiceTests
{
    internal void Respond_AcceptedRequest_CreatesFriendship()
    {
        // Arrange
        FriendshipStore store = CreateStore(FriendshipRequestState.Pending);
        FriendRequestResponseService service =
            new FriendRequestResponseService("Pedro", store);

        // Act
        FriendRequestResponseResult result = service.Respond("request", true);
        FriendRequestResult duplicateResult = store.TryCreateRequest("Pedro", "Ana");

        // Assert
        TestAssert.AreEqual(FriendRequestResponseResult.Accepted, result);
        TestAssert.AreEqual(
            FriendRequestResult.FriendshipAlreadyExists,
            duplicateResult);
        TestAssert.AreEqual(0, store.GetPendingReceived("Pedro").Count);
        TestAssert.AreEqual(1, store.GetFriends("Pedro").Count);
    }

    internal void Respond_RejectedRequest_DoesNotCreateFriendship()
    {
        // Arrange
        FriendshipStore store = CreateStore(FriendshipRequestState.Pending);
        FriendRequestResponseService service =
            new FriendRequestResponseService("Pedro", store);

        // Act
        FriendRequestResponseResult result = service.Respond("request", false);
        FriendRequestResult newRequestResult = store.TryCreateRequest("Pedro", "Ana");

        // Assert
        TestAssert.AreEqual(FriendRequestResponseResult.Rejected, result);
        TestAssert.AreEqual(FriendRequestResult.Success, newRequestResult);
        TestAssert.AreEqual(0, store.GetPendingReceived("Pedro").Count);
    }

    internal void Respond_ProcessedRequest_PreservesState()
    {
        // Arrange
        FriendshipStore store = CreateStore(FriendshipRequestState.Rejected);
        FriendRequestResponseService service =
            new FriendRequestResponseService("Pedro", store);

        // Act
        FriendRequestResponseResult result = service.Respond("request", true);
        FriendRequestResult newRequestResult = store.TryCreateRequest("Pedro", "Ana");

        // Assert
        TestAssert.AreEqual(FriendRequestResponseResult.AlreadyProcessed, result);
        TestAssert.AreEqual(FriendRequestResult.Success, newRequestResult);
    }

    internal void Respond_DifferentRecipient_PreservesPendingRequest()
    {
        // Arrange
        FriendshipStore store = CreateStore(FriendshipRequestState.Pending);
        FriendRequestResponseService service =
            new FriendRequestResponseService("Luis", store);

        // Act
        FriendRequestResponseResult result = service.Respond("request", true);

        // Assert
        TestAssert.AreEqual(FriendRequestResponseResult.Unauthorized, result);
        TestAssert.AreEqual(1, store.GetPendingReceived("Pedro").Count);
    }

    private static FriendshipStore CreateStore(FriendshipRequestState state)
    {
        return new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Ana"),
                new FriendCandidate("Luis")
            },
            friendRequests: new[]
            {
                new FriendshipRequest(
                    "request",
                    "Ana",
                    "Pedro",
                    new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc),
                    state)
            });
    }
}

internal sealed class FriendsServiceTests
{
    internal void TryGetFriends_MixedRelationships_FiltersAndSorts()
    {
        // Arrange
        FriendshipStore store = new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Zoe"),
                new FriendCandidate("Ana"),
                new FriendCandidate("Luis", false),
                new FriendCandidate("Mario")
            },
            new[]
            {
                FriendshipStore.CreateRelationshipKey("Pedro", "Zoe"),
                FriendshipStore.CreateRelationshipKey("Pedro", "Ana"),
                FriendshipStore.CreateRelationshipKey("Pedro", "Luis")
            });
        FriendsService service = new FriendsService("Pedro", store);

        // Act
        FriendsRetrievalResult result = service.TryGetFriends(
            out IReadOnlyList<FriendCandidate> friends);

        // Assert
        TestAssert.AreEqual(FriendsRetrievalResult.Success, result);
        TestAssert.AreEqual(2, friends.Count);
        TestAssert.AreEqual("Ana", friends[0].Username);
        TestAssert.AreEqual("Zoe", friends[1].Username);
    }

    internal void TryGetFriends_ReversedRelationship_ReturnsFriend()
    {
        // Arrange
        FriendshipStore store = new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Ana")
            },
            new[] { FriendshipStore.CreateRelationshipKey("Ana", "Pedro") });
        FriendsService service = new FriendsService("Pedro", store);

        // Act
        FriendsRetrievalResult result = service.TryGetFriends(
            out IReadOnlyList<FriendCandidate> friends);

        // Assert
        TestAssert.AreEqual(FriendsRetrievalResult.Success, result);
        TestAssert.AreEqual(1, friends.Count);
        TestAssert.AreEqual("Ana", friends[0].Username);
    }

    internal void TryGetFriends_NoRelationships_ReturnsEmpty()
    {
        // Arrange
        FriendshipStore store = new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Ana")
            });
        FriendsService service = new FriendsService("Pedro", store);

        // Act
        FriendsRetrievalResult result = service.TryGetFriends(
            out IReadOnlyList<FriendCandidate> friends);

        // Assert
        TestAssert.AreEqual(FriendsRetrievalResult.Success, result);
        TestAssert.AreEqual(0, friends.Count);
    }
}

internal sealed class FriendRemovalServiceTests
{
    internal void Remove_ExistingFriendship_RemovesOnlyRelationship()
    {
        // Arrange
        FriendshipStore store = CreateStore(
            FriendshipStore.CreateRelationshipKey("Pedro", "Ana"));
        FriendRemovalService service = new FriendRemovalService("Pedro", store);

        // Act
        FriendRemovalResult result = service.Remove("Ana");
        FriendRemovalResult repeatedResult = service.Remove("Ana");
        FriendRequestResult newRequestResult = store.TryCreateRequest("Pedro", "Ana");

        // Assert
        TestAssert.AreEqual(FriendRemovalResult.Success, result);
        TestAssert.AreEqual(FriendRemovalResult.NotFound, repeatedResult);
        TestAssert.AreEqual(FriendRequestResult.Success, newRequestResult);
        TestAssert.AreEqual(0, store.GetFriends("Pedro").Count);
    }

    internal void Remove_MissingFriendship_PreservesExistingFriendship()
    {
        // Arrange
        FriendshipStore store = CreateStore(
            FriendshipStore.CreateRelationshipKey("Pedro", "Ana"));
        FriendRemovalService service = new FriendRemovalService("Pedro", store);

        // Act
        FriendRemovalResult result = service.Remove("Luis");

        // Assert
        TestAssert.AreEqual(FriendRemovalResult.NotFound, result);
        TestAssert.AreEqual(1, store.GetFriends("Pedro").Count);
        TestAssert.AreEqual("Ana", store.GetFriends("Pedro")[0].Username);
    }

    internal void Remove_ThirdPartyFriendship_DoesNotModifyRelationship()
    {
        // Arrange
        FriendshipStore store = CreateStore(
            FriendshipStore.CreateRelationshipKey("Ana", "Luis"));
        FriendRemovalService service = new FriendRemovalService("Pedro", store);

        // Act
        FriendRemovalResult result = service.Remove("Ana");

        // Assert
        TestAssert.AreEqual(FriendRemovalResult.NotFound, result);
        TestAssert.AreEqual(1, store.GetFriends("Ana").Count);
        TestAssert.AreEqual("Luis", store.GetFriends("Ana")[0].Username);
    }

    private static FriendshipStore CreateStore(string relationship)
    {
        return new FriendshipStore(
            new[]
            {
                new FriendCandidate("Pedro"),
                new FriendCandidate("Ana"),
                new FriendCandidate("Luis")
            },
            new[] { relationship });
    }
}

internal sealed class ChatServiceTests
{
    internal void Send_EmptyMessage_DoesNotStoreMessage()
    {
        // Arrange
        ChatStore store = new ChatStore();
        ChatService service = new ChatService(CreateActiveSession(), store);

        // Act
        ChatSendResult result = service.Send("   ", out ChatMessage? message, out _);

        // Assert
        TestAssert.AreEqual(ChatSendResult.EmptyMessage, result);
        TestAssert.AreEqual(true, message == null);
        TestAssert.AreEqual(0, GetMessages(service).Count);
    }

    internal void Send_ValidMessage_TrimsAndStoresMessage()
    {
        // Arrange
        DateTime now = new DateTime(
            2026,
            9,
            19,
            12,
            0,
            0,
            DateTimeKind.Utc);
        ChatStore store = new ChatStore(() => now);
        ChatService service = new ChatService(CreateActiveSession(), store);

        // Act
        ChatSendResult result = service.Send(
            "  Good move!  ",
            out ChatMessage? message,
            out _);

        // Assert
        TestAssert.AreEqual(ChatSendResult.Success, result);
        TestAssert.AreEqual(true, message != null);
        TestAssert.AreEqual("Pedro", message!.SenderUsername);
        TestAssert.AreEqual("Good move!", message.Content);
        TestAssert.AreEqual(now, message.SentAtUtc);
        TestAssert.AreEqual(1, GetMessages(service).Count);
    }

    internal void Send_MessageAboveLimit_DoesNotStoreMessage()
    {
        // Arrange
        ChatStore store = new ChatStore();
        ChatService service = new ChatService(CreateActiveSession(), store);
        string content = new string('a', ChatStore.MaximumMessageLength + 1);

        // Act
        ChatSendResult result = service.Send(content, out _, out _);

        // Assert
        TestAssert.AreEqual(ChatSendResult.MessageTooLong, result);
        TestAssert.AreEqual(0, GetMessages(service).Count);
    }

    internal void Send_InactiveConversation_ReturnsUnauthorized()
    {
        // Arrange
        MatchSession session = new MatchSession("WAIT01", 40, "Pedro", null);
        ChatService service = new ChatService(session, new ChatStore());

        // Act
        ChatSendResult result = service.Send("Hello", out _, out _);
        ChatReadResult readResult = service.TryGetMessages(
            out IReadOnlyList<ChatMessage> messages);

        // Assert
        TestAssert.AreEqual(ChatSendResult.Unauthorized, result);
        TestAssert.AreEqual(ChatReadResult.Unauthorized, readResult);
        TestAssert.AreEqual(0, messages.Count);
    }

    internal void Send_GuestSession_ReturnsUnauthorized()
    {
        // Arrange
        MatchSession session = new MatchSession(
            "GUEST1",
            40,
            "Guest",
            "Guest");
        session.JoinAsSecondPlayer("Ana");
        ChatService service = new ChatService(session, new ChatStore());

        // Act
        ChatSendResult result = service.Send("Hello", out _, out _);
        ChatReadResult readResult = service.TryGetMessages(
            out IReadOnlyList<ChatMessage> messages);

        // Assert
        TestAssert.AreEqual(ChatSendResult.Unauthorized, result);
        TestAssert.AreEqual(ChatReadResult.Unauthorized, readResult);
        TestAssert.AreEqual(0, messages.Count);
    }

    internal void Send_ProhibitedSpanishContent_RecordsInfringement()
    {
        // Arrange
        ChatStore store = new ChatStore();
        ChatService service = new ChatService(CreateActiveSession(), store);

        // Act
        ChatSendResult result = service.Send(
            "Eres un estúpido",
            out ChatMessage? message,
            out _);

        // Assert
        TestAssert.AreEqual(ChatSendResult.MessageBlocked, result);
        TestAssert.AreEqual(true, message == null);
        TestAssert.AreEqual(1, store.GetInfringementCount("Pedro"));
        TestAssert.AreEqual(0, GetMessages(service).Count);
    }

    internal void Send_ProhibitedSubstringInsideWord_AllowsMessage()
    {
        // Arrange
        ChatStore store = new ChatStore();
        ChatService service = new ChatService(CreateActiveSession(), store);

        // Act
        ChatSendResult result = service.Send(
            "That move was made stupidly",
            out ChatMessage? message,
            out _);

        // Assert
        TestAssert.AreEqual(ChatSendResult.Success, result);
        TestAssert.AreEqual(true, message != null);
        TestAssert.AreEqual(0, store.GetInfringementCount("Pedro"));
    }

    internal void Send_ThirdRecentInfringement_AppliesMute()
    {
        // Arrange
        DateTime now = new DateTime(
            2026,
            9,
            19,
            12,
            0,
            0,
            DateTimeKind.Utc);
        ChatStore store = new ChatStore(() => now);
        ChatService service = new ChatService(CreateActiveSession(), store);

        // Act
        ChatSendResult first = service.Send("idiota", out _, out _);
        ChatSendResult second = service.Send("moron", out _, out _);
        ChatSendResult third = service.Send(
            "callate",
            out _,
            out DateTime? muteEndsAtUtc);

        // Assert
        TestAssert.AreEqual(ChatSendResult.MessageBlocked, first);
        TestAssert.AreEqual(ChatSendResult.MessageBlocked, second);
        TestAssert.AreEqual(ChatSendResult.PlayerMuted, third);
        TestAssert.AreEqual(
            now.Add(ChatStore.MuteDuration),
            muteEndsAtUtc!.Value);
        TestAssert.AreEqual(3, store.GetInfringementCount("Pedro"));
        TestAssert.AreEqual(0, GetMessages(service).Count);
    }

    internal void Send_AfterMuteExpires_StoresSafeMessage()
    {
        // Arrange
        DateTime now = new DateTime(
            2026,
            9,
            19,
            12,
            0,
            0,
            DateTimeKind.Utc);
        ChatStore store = new ChatStore(() => now);
        ChatService service = new ChatService(CreateActiveSession(), store);
        service.Send("idiota", out _, out _);
        service.Send("moron", out _, out _);
        service.Send("callate", out _, out DateTime? muteEndsAtUtc);

        // Act
        ChatSendResult mutedResult = service.Send("Good move", out _, out _);
        now = muteEndsAtUtc!.Value.AddSeconds(1);
        ChatSendResult successResult = service.Send(
            "Good move",
            out ChatMessage? message,
            out _);

        // Assert
        TestAssert.AreEqual(ChatSendResult.PlayerMuted, mutedResult);
        TestAssert.AreEqual(ChatSendResult.Success, successResult);
        TestAssert.AreEqual(true, message != null);
        TestAssert.AreEqual(1, GetMessages(service).Count);
    }

    private static MatchSession CreateActiveSession()
    {
        MatchSession session = new MatchSession("CHAT01", 40, "Pedro", null);
        session.JoinAsSecondPlayer("Ana");
        return session;
    }

    private static IReadOnlyList<ChatMessage> GetMessages(ChatService service)
    {
        ChatReadResult result = service.TryGetMessages(
            out IReadOnlyList<ChatMessage> messages);
        TestAssert.AreEqual(ChatReadResult.Success, result);
        return messages;
    }
}

internal sealed class ChatReadServiceTests
{
    internal void TryGetMessages_AuthorizedPlayer_SortsChronologically()
    {
        // Arrange
        DateTime now = new DateTime(
            2026,
            9,
            19,
            13,
            0,
            0,
            DateTimeKind.Utc);
        ChatStore store = new ChatStore(() => now);
        ChatService service = new ChatService(
            CreateSession("READ01", DiscColor.Black),
            store);
        service.Send("Later", out _, out _);
        now = now.AddHours(-1);
        service.Send("Earlier", out _, out _);

        // Act
        ChatReadResult result = service.TryGetMessages(
            out IReadOnlyList<ChatMessage> messages);

        // Assert
        TestAssert.AreEqual(ChatReadResult.Success, result);
        TestAssert.AreEqual(2, messages.Count);
        TestAssert.AreEqual("Earlier", messages[0].Content);
        TestAssert.AreEqual("Later", messages[1].Content);
    }

    internal void TryGetMessages_EmptyConversation_ReturnsEmpty()
    {
        // Arrange
        ChatService service = new ChatService(
            CreateSession("EMPTY1", DiscColor.Black),
            new ChatStore());

        // Act
        ChatReadResult result = service.TryGetMessages(
            out IReadOnlyList<ChatMessage> messages);

        // Assert
        TestAssert.AreEqual(ChatReadResult.Success, result);
        TestAssert.AreEqual(0, messages.Count);
    }

    internal void MessageReceived_OpenAuthorizedChat_AddsOnlyConversationMessage()
    {
        // Arrange
        ChatStore store = new ChatStore();
        ChatService reader = new ChatService(
            CreateSession("LIVE01", DiscColor.Black),
            store);
        ChatService participant = new ChatService(
            CreateSession("LIVE01", DiscColor.White),
            store);
        ChatService otherConversation = new ChatService(
            CreateSession("OTHER1", DiscColor.White),
            store);
        List<ChatMessage> received = new List<ChatMessage>();
        void OnMessage(ChatMessage message)
        {
            if (reader.CanReadMessage(message))
            {
                received.Add(message);
            }
        }

        // Act
        reader.MessageReceived += OnMessage;
        participant.Send("Hello Pedro", out _, out _);
        otherConversation.Send("Unrelated", out _, out _);
        reader.MessageReceived -= OnMessage;

        // Assert
        TestAssert.AreEqual(1, received.Count);
        TestAssert.AreEqual("Ana", received[0].SenderUsername);
        TestAssert.AreEqual("Hello Pedro", received[0].Content);
    }

    private static MatchSession CreateSession(string code, DiscColor localColor)
    {
        MatchSession session = new MatchSession(
            code,
            40,
            "Pedro",
            null,
            localColor);
        session.JoinAsSecondPlayer("Ana");
        return session;
    }
}

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
        TestAssert.AreEqual("How to play", content!.Title);
        TestAssert.AreEqual("Rules", content.Message);
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
        TestAssert.AreEqual("dark / 40 / Rules", content!.Message);
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

internal sealed class MovementHistoryServiceTests
{
    internal void TryGetMovements_UnorderedRecords_SortsBySequence()
    {
        // Arrange
        DateTime recordedAt = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
        RecordedMove second = new RecordedMove(
            2, "Ana", "C3", recordedAt.AddMinutes(1), 2, MatchMoveOrigin.Timeout);
        RecordedMove first = new RecordedMove(
            1, "Pedro", "D3", recordedAt, 1, MatchMoveOrigin.Manual);
        MatchInformation match = CreateMatch(new[] { second, first });
        MovementHistoryService service = new MovementHistoryService(new[] { match });

        // Act
        MovementHistoryRetrievalResult result = service.TryGetMovements(
            match.Id,
            out IReadOnlyList<RecordedMove> movements);

        // Assert
        TestAssert.AreEqual(MovementHistoryRetrievalResult.Success, result);
        TestAssert.AreEqual(2, movements.Count);
        TestAssert.AreEqual(1, movements[0].Number);
        TestAssert.AreEqual(2, movements[1].Number);
        TestAssert.AreEqual(recordedAt, movements[0].RecordedAtUtc);
        TestAssert.AreEqual(1, movements[0].AffectedPieces);
        TestAssert.AreEqual(MatchMoveOrigin.Manual, movements[0].Origin);
        TestAssert.AreEqual(MatchMoveOrigin.Timeout, movements[1].Origin);
    }

    internal void TryGetMovements_NoRecords_ReturnsEmpty()
    {
        // Arrange
        MatchInformation match = CreateMatch(Array.Empty<RecordedMove>());
        MovementHistoryService service = new MovementHistoryService(new[] { match });

        // Act
        MovementHistoryRetrievalResult result = service.TryGetMovements(
            match.Id,
            out IReadOnlyList<RecordedMove> movements);

        // Assert
        TestAssert.AreEqual(MovementHistoryRetrievalResult.Success, result);
        TestAssert.AreEqual(0, movements.Count);
    }

    internal void TryGetMovements_MissingMatch_ReturnsRetrievalError()
    {
        // Arrange
        MovementHistoryService service = new MovementHistoryService(
            Array.Empty<MatchInformation>());

        // Act
        MovementHistoryRetrievalResult result = service.TryGetMovements(
            "missing",
            out IReadOnlyList<RecordedMove> movements);

        // Assert
        TestAssert.AreEqual(MovementHistoryRetrievalResult.RetrievalError, result);
        TestAssert.AreEqual(0, movements.Count);
    }

    private static MatchInformation CreateMatch(IReadOnlyList<RecordedMove> movements)
    {
        return new MatchInformation(
            "movements",
            "Pedro",
            "Ana",
            40,
            MatchRecordState.Finished,
            "Pedro",
            new[] { ReversiBoard.CreateInitial() },
            movements,
            new DateTime(2026, 9, 18, 13, 0, 0, DateTimeKind.Utc),
            MatchEndReason.BoardCompleted);
    }
}

internal sealed class MatchInformationServiceTests
{
    internal void TryGetHistory_MixedRecords_FiltersAndSorts()
    {
        // Arrange
        DateTime referenceTime = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
        MatchInformation[] records =
        {
            CreateRecord("older", "Pedro", "Ana", referenceTime.AddDays(-2)),
            CreateRecord("unrelated", "Luis", "Luna", referenceTime),
            CreateRecord("newer", "César", "Pedro", referenceTime.AddHours(-1))
        };
        MatchInformationService service = new MatchInformationService("Pedro", records);

        // Act
        MatchInformationRetrievalResult result = service.TryGetHistory(
            out IReadOnlyList<MatchInformation> history);

        // Assert
        TestAssert.AreEqual(MatchInformationRetrievalResult.Success, result);
        TestAssert.AreEqual(2, history.Count);
        TestAssert.AreEqual("newer", history[0].Id);
        TestAssert.AreEqual("older", history[1].Id);
    }

    internal void TryGetHistory_NoPlayerMatches_ReturnsEmpty()
    {
        // Arrange
        MatchInformation[] records =
        {
            CreateRecord("unrelated", "Ana", "Luis", DateTime.UtcNow)
        };
        MatchInformationService service = new MatchInformationService("Pedro", records);

        // Act
        MatchInformationRetrievalResult result = service.TryGetHistory(
            out IReadOnlyList<MatchInformation> history);

        // Assert
        TestAssert.AreEqual(MatchInformationRetrievalResult.Success, result);
        TestAssert.AreEqual(0, history.Count);
    }

    internal void TryGetHistory_Record_PreservesSummaryFields()
    {
        // Arrange
        DateTime recordedAt = new DateTime(2026, 9, 17, 20, 30, 0, DateTimeKind.Utc);
        MatchInformation record = CreateRecord(
            "summary",
            "Pedro",
            "Ana",
            recordedAt,
            MatchEndReason.Abandonment);
        MatchInformationService service = new MatchInformationService(
            "Pedro",
            new[] { record });

        // Act
        service.TryGetHistory(out IReadOnlyList<MatchInformation> history);

        // Assert
        TestAssert.AreEqual(recordedAt, history[0].RecordedAtUtc);
        TestAssert.AreEqual<MatchEndReason?>(
            MatchEndReason.Abandonment,
            history[0].EndReason);
        TestAssert.AreEqual(4, history[0].LatestBoard.Count(DiscColor.Black));
        TestAssert.AreEqual(1, history[0].LatestBoard.Count(DiscColor.White));
    }

    private static MatchInformation CreateRecord(
        string id,
        string blackPlayer,
        string whitePlayer,
        DateTime recordedAt,
        MatchEndReason endReason = MatchEndReason.BoardCompleted)
    {
        ReversiBoard initialBoard = ReversiBoard.CreateInitial();
        initialBoard.TryApplyMove(
            DiscColor.Black,
            new BoardPosition(2, 3),
            out ReversiBoard? finalBoard);
        return new MatchInformation(
            id,
            blackPlayer,
            whitePlayer,
            40,
            MatchRecordState.Finished,
            blackPlayer,
            new[] { initialBoard, finalBoard! },
            new[]
            {
                new RecordedMove(
                    1,
                    blackPlayer,
                    "D3",
                    recordedAt.AddMinutes(-1),
                    1,
                    MatchMoveOrigin.Manual)
            },
            recordedAt,
            endReason);
    }
}

internal sealed class AbandonMatchServiceTests
{
    internal void TryAbandon_ActiveMatch_RecordsCompleteResult()
    {
        // Arrange
        MatchSession session = CreateSystemMatch();
        GameMoveService moveService = new GameMoveService(new Random(1));
        moveService.TryMakeMove(
            session,
            DiscColor.Black,
            new BoardPosition(2, 3),
            out _,
            out _);
        ReversiBoard boardBeforeAbandonment = session.Board;
        MatchMove recordedMove = session.Moves[0];
        AbandonMatchService service = new AbandonMatchService();

        // Act
        AbandonMatchResult result = service.TryAbandon(session, DiscColor.Black);

        // Assert
        TestAssert.AreEqual(AbandonMatchResult.Success, result);
        TestAssert.AreEqual(MatchState.Finished, session.State);
        TestAssert.AreEqual("System", session.WinnerName);
        TestAssert.AreEqual<DiscColor?>(DiscColor.Black, session.AbandonedByColor);
        TestAssert.AreEqual("Pedro", session.AbandonedByName);
        TestAssert.AreEqual<MatchEndReason?>(
            MatchEndReason.Abandonment,
            session.EndReason);
        TestAssert.AreEqual(true, session.FinishedAtUtc.HasValue);
        TestAssert.AreEqual(1, session.Moves.Count);
        TestAssert.AreEqual(true, ReferenceEquals(recordedMove, session.Moves[0]));
        TestAssert.AreEqual(true, ReferenceEquals(boardBeforeAbandonment, session.Board));
    }

    internal void TryAbandon_JoinedPlayer_AwardsCreatorVictory()
    {
        // Arrange
        MatchSession session = new MatchSession(
            "JOIN17",
            40,
            "Ana",
            null,
            DiscColor.White);
        session.JoinAsSecondPlayer("Pedro");
        AbandonMatchService service = new AbandonMatchService();

        // Act
        AbandonMatchResult result = service.TryAbandon(session, DiscColor.White);

        // Assert
        TestAssert.AreEqual(AbandonMatchResult.Success, result);
        TestAssert.AreEqual("Ana", session.WinnerName);
        TestAssert.AreEqual("Pedro", session.AbandonedByName);
        TestAssert.AreEqual<DiscColor?>(DiscColor.White, session.AbandonedByColor);
    }

    internal void TryAbandon_FinishedMatch_PreservesExistingResult()
    {
        // Arrange
        MatchSession session = CreateSystemMatch();
        session.Finish(DiscColor.Black, MatchEndReason.BoardCompleted);
        DateTime? originalFinishedAt = session.FinishedAtUtc;
        AbandonMatchService service = new AbandonMatchService();

        // Act
        AbandonMatchResult result = service.TryAbandon(session, DiscColor.Black);

        // Assert
        TestAssert.AreEqual(AbandonMatchResult.MatchAlreadyFinished, result);
        TestAssert.AreEqual("Pedro", session.WinnerName);
        TestAssert.AreEqual<MatchEndReason?>(
            MatchEndReason.BoardCompleted,
            session.EndReason);
        TestAssert.AreEqual(originalFinishedAt, session.FinishedAtUtc);
        TestAssert.AreEqual<DiscColor?>(null, session.AbandonedByColor);
        TestAssert.AreEqual<string?>(null, session.AbandonedByName);
    }

    private static MatchSession CreateSystemMatch()
    {
        MatchSession session = new MatchSession(
            "QUIT17",
            40,
            "Pedro",
            null);
        session.StartAgainstSystem("System");
        return session;
    }
}

internal sealed class GameMoveServiceTests
{
    internal void TryMakeMove_ValidMove_RecordsAndAdvancesTurn()
    {
        // Arrange
        MatchSession session = CreateStartedSession();
        session.RegisterTurnTimeout(DiscColor.Black);
        session.RegisterTurnTimeout(DiscColor.Black);
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        GameMoveResult result = service.TryMakeMove(
            session,
            DiscColor.Black,
            new BoardPosition(2, 3),
            out MatchMove? move,
            out DiscColor? skippedColor);

        // Assert
        TestAssert.AreEqual(GameMoveResult.Success, result);
        TestAssert.AreEqual(true, move != null);
        TestAssert.AreEqual(false, move!.WasAutomatic);
        TestAssert.AreEqual<DiscColor?>(null, skippedColor);
        TestAssert.AreEqual(1, session.Moves.Count);
        TestAssert.AreEqual(4, session.Board.Count(DiscColor.Black));
        TestAssert.AreEqual(1, session.Board.Count(DiscColor.White));
        TestAssert.AreEqual(DiscColor.White, session.CurrentTurnColor);
        TestAssert.AreEqual(0, session.GetConsecutiveTimeouts(DiscColor.Black));
    }

    internal void TryMakeMove_OpponentHasNoMove_KeepsCurrentPlayerTurn()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.FromRows(
            ".B.BBBB.", "..BBB...", "BB...BBB", ".BBB.B.B",
            "..BBB..B", "B..BBWBB", ".W.BB...", "BBBB.BBB");
        MatchSession session = CreateStartedSession(board);
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        GameMoveResult result = service.TryMakeMove(
            session,
            DiscColor.Black,
            new BoardPosition(4, 6),
            out _,
            out DiscColor? skippedColor);

        // Assert
        TestAssert.AreEqual(GameMoveResult.Success, result);
        TestAssert.AreEqual<DiscColor?>(DiscColor.White, skippedColor);
        TestAssert.AreEqual(DiscColor.Black, session.CurrentTurnColor);
        TestAssert.AreEqual(MatchState.InProgress, session.State);
    }

    internal void TryMakeMove_InvalidPosition_PreservesSession()
    {
        // Arrange
        MatchSession session = CreateStartedSession();
        ReversiBoard originalBoard = session.Board;
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        GameMoveResult result = service.TryMakeMove(
            session,
            DiscColor.Black,
            new BoardPosition(0, 0),
            out MatchMove? move,
            out _);

        // Assert
        TestAssert.AreEqual(GameMoveResult.InvalidPosition, result);
        TestAssert.AreEqual<MatchMove?>(null, move);
        TestAssert.AreEqual(true, ReferenceEquals(originalBoard, session.Board));
        TestAssert.AreEqual(0, session.Moves.Count);
        TestAssert.AreEqual(DiscColor.Black, session.CurrentTurnColor);
    }

    internal void TryMakeMove_WrongTurn_PreservesSession()
    {
        // Arrange
        MatchSession session = CreateStartedSession();
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        GameMoveResult result = service.TryMakeMove(
            session,
            DiscColor.White,
            new BoardPosition(2, 4),
            out _,
            out _);

        // Assert
        TestAssert.AreEqual(GameMoveResult.NotPlayersTurn, result);
        TestAssert.AreEqual(0, session.Moves.Count);
        TestAssert.AreEqual(DiscColor.Black, session.CurrentTurnColor);
    }

    internal void PrepareCurrentTurn_NoMove_PassesTurn()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.FromRows(
            "WB......", "........", "........", "........",
            "........", "........", "........", "........");
        MatchSession session = CreateStartedSession(board);
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        TurnPreparationResult result = service.PrepareCurrentTurn(
            session,
            out DiscColor? skippedColor);

        // Assert
        TestAssert.AreEqual(TurnPreparationResult.TurnPassed, result);
        TestAssert.AreEqual<DiscColor?>(DiscColor.Black, skippedColor);
        TestAssert.AreEqual(DiscColor.White, session.CurrentTurnColor);
        TestAssert.AreEqual(MatchState.InProgress, session.State);
    }

    internal void PrepareCurrentTurn_NoMovesForEitherPlayer_FinishesMatch()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.FromRows(
            "BBBBBBBB", "BBBBBBBB", "BBBBBBBB", "BBBBBBBB",
            "BBBBBBBB", "BBBBBBBB", "BBBBBBBB", "BBBBBBBB");
        MatchSession session = CreateStartedSession(board);
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        TurnPreparationResult result = service.PrepareCurrentTurn(session, out _);

        // Assert
        TestAssert.AreEqual(TurnPreparationResult.MatchFinished, result);
        TestAssert.AreEqual(MatchState.Finished, session.State);
        TestAssert.AreEqual("Pedro", session.WinnerName);
        TestAssert.AreEqual<MatchEndReason?>(MatchEndReason.BoardCompleted, session.EndReason);
        TestAssert.AreEqual(true, session.FinishedAtUtc.HasValue);
    }

    internal void PrepareCurrentTurn_EqualFullBoard_FinishesWithDraw()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.FromRows(
            "BBBBWWWW", "BBBBWWWW", "BBBBWWWW", "BBBBWWWW",
            "BBBBWWWW", "BBBBWWWW", "BBBBWWWW", "BBBBWWWW");
        MatchSession session = CreateStartedSession(board);
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        TurnPreparationResult result = service.PrepareCurrentTurn(session, out _);

        // Assert
        TestAssert.AreEqual(TurnPreparationResult.MatchFinished, result);
        TestAssert.AreEqual(MatchState.Finished, session.State);
        TestAssert.AreEqual<string?>(null, session.WinnerName);
        TestAssert.AreEqual<MatchEndReason?>(
            MatchEndReason.BoardCompleted,
            session.EndReason);
    }

    internal void HandleTimeout_FirstTimeout_MakesAutomaticMove()
    {
        // Arrange
        MatchSession session = CreateStartedSession();
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        TurnTimeoutResult result = service.HandleTimeout(
            session,
            out MatchMove? move,
            out _);

        // Assert
        TestAssert.AreEqual(TurnTimeoutResult.AutomaticMove, result);
        TestAssert.AreEqual(true, move != null);
        TestAssert.AreEqual(true, move!.WasAutomatic);
        TestAssert.AreEqual(MatchMoveOrigin.Timeout, move.Origin);
        TestAssert.AreEqual(1, session.Moves.Count);
        TestAssert.AreEqual(1, session.GetConsecutiveTimeouts(DiscColor.Black));
        TestAssert.AreEqual(DiscColor.White, session.CurrentTurnColor);
    }

    internal void HandleTimeout_ThirdTimeout_AwardsOpponentVictory()
    {
        // Arrange
        MatchSession session = CreateStartedSession();
        session.RegisterTurnTimeout(DiscColor.Black);
        session.RegisterTurnTimeout(DiscColor.Black);
        GameMoveService service = new GameMoveService(new Random(1));

        // Act
        TurnTimeoutResult result = service.HandleTimeout(session, out _, out _);

        // Assert
        TestAssert.AreEqual(TurnTimeoutResult.MatchFinishedByInactivity, result);
        TestAssert.AreEqual(MatchState.Finished, session.State);
        TestAssert.AreEqual("System", session.WinnerName);
        TestAssert.AreEqual<MatchEndReason?>(MatchEndReason.Inactivity, session.EndReason);
        TestAssert.AreEqual(0, session.Moves.Count);

        GameMoveResult moveAfterFinish = service.TryMakeMove(
            session,
            DiscColor.Black,
            new BoardPosition(2, 3),
            out _,
            out _);
        TestAssert.AreEqual(GameMoveResult.MatchNotInProgress, moveAfterFinish);
    }

    internal void TryMakeSystemMove_ValidTurn_RecordsSystemOrigin()
    {
        // Arrange
        MatchSession session = CreateStartedSession();
        GameMoveService service = new GameMoveService(new Random(1));
        service.TryMakeMove(
            session,
            DiscColor.Black,
            new BoardPosition(2, 3),
            out _,
            out _);

        // Act
        GameMoveResult result = service.TryMakeSystemMove(
            session,
            out MatchMove? move,
            out _);

        // Assert
        TestAssert.AreEqual(GameMoveResult.Success, result);
        TestAssert.AreEqual(true, move != null);
        TestAssert.AreEqual(MatchMoveOrigin.System, move!.Origin);
        TestAssert.AreEqual(2, session.Moves.Count);
        TestAssert.AreEqual(DiscColor.Black, session.CurrentTurnColor);
    }

    private static MatchSession CreateStartedSession(ReversiBoard? board = null)
    {
        MatchSession session = new MatchSession(
            "MV2T4X",
            40,
            "Pedro",
            null,
            DiscColor.Black,
            board);
        session.StartAgainstSystem("System");
        return session;
    }
}

internal sealed class MatchCodePolicyTests
{
    internal void TryNormalize_LowercaseCode_ReturnsUppercaseCode()
    {
        // Arrange

        // Act
        bool valid = MatchCodePolicy.TryNormalize("an4b2c", out string code);

        // Assert
        TestAssert.AreEqual(true, valid);
        TestAssert.AreEqual("AN4B2C", code);
    }

    internal void TryNormalize_IncorrectLength_ReturnsFalse()
        // Act
        // Arrange
    {
        // Assert
        TestAssert.AreEqual(false, MatchCodePolicy.TryNormalize("ABC12", out _));
        TestAssert.AreEqual(false, MatchCodePolicy.TryNormalize("ABC2345", out _));
    }

    internal void TryNormalize_AmbiguousCharacters_ReturnsFalse()
        // Act
        // Arrange
    {
        // Assert
        TestAssert.AreEqual(false, MatchCodePolicy.TryNormalize("ABCI01", out _));
    }
}

internal sealed class JoinMatchServiceTests
{
    internal void TryJoinByCode_InvalidCode_DoesNotCreateSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Pedro", null);

        // Act
        JoinMatchResult result = service.TryJoinByCode("bad", out MatchSession? session);

        // Assert
        TestAssert.AreEqual(JoinMatchResult.InvalidMatchCode, result);
        TestAssert.AreEqual<MatchSession?>(null, session);
    }

    internal void TryJoinById_UnavailableMatch_DoesNotCreateSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Pedro", null);

        // Act
        JoinMatchResult result = service.TryJoinById("full-1", out MatchSession? session);

        // Assert
        TestAssert.AreEqual(JoinMatchResult.NoLongerAvailable, result);
        TestAssert.AreEqual<MatchSession?>(null, session);
    }

    internal void TryJoinById_OwnMatch_DoesNotCreateSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Pedro", null);

        // Act
        JoinMatchResult result = service.TryJoinById("own-1", out MatchSession? session);

        // Assert
        TestAssert.AreEqual(JoinMatchResult.CannotJoinOwnMatch, result);
        TestAssert.AreEqual<MatchSession?>(null, session);
    }

    internal void TryJoinById_AvailableMatch_StartsSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Pedro", null);

        // Act
        JoinMatchResult result = service.TryJoinById("waiting-1", out MatchSession? session);

        // Assert
        TestAssert.AreEqual(JoinMatchResult.Success, result);
        TestAssert.AreEqual(true, session != null);
        TestAssert.AreEqual(MatchState.InProgress, session!.State);
        TestAssert.AreEqual("Ana", session.CreatorName);
        TestAssert.AreEqual("Pedro", session.OpponentName);
        TestAssert.AreEqual("Pedro", session.LocalPlayerName);
        TestAssert.AreEqual(DiscColor.White, session.LocalPlayerColor);
        TestAssert.AreEqual(2, session.Board.Count(DiscColor.Black));
        TestAssert.AreEqual(2, session.Board.Count(DiscColor.White));
    }

    internal void TryJoinByCode_AvailableMatch_PreservesGuestSession()
    {
        // Arrange
        JoinMatchService service = new JoinMatchService("Invitado", "Invitado");

        // Act
        JoinMatchResult result = service.TryJoinByCode("lu5n7p", out MatchSession? session);
        JoinMatchResult secondAttempt = service.TryJoinByCode("LU5N7P", out _);

        // Assert
        TestAssert.AreEqual(JoinMatchResult.Success, result);
        TestAssert.AreEqual(true, session != null);
        TestAssert.AreEqual("Invitado", session!.GuestName);
        TestAssert.AreEqual("Invitado", session.LocalPlayerName);
        TestAssert.AreEqual(MatchState.InProgress, session.State);
        TestAssert.AreEqual(JoinMatchResult.MatchCodeNotAvailable, secondAttempt);
    }
}

internal sealed class MatchSettingsServiceTests
{
    private readonly MatchSettingsService _service = new MatchSettingsService();

    internal void ChangeTurnTime_WaitingCreator_UpdatesOnlyTime()
    {
        // Arrange
        MatchSession session = CreateSession();
        ReversiBoard originalBoard = session.Board;

        // Act
        MatchSettingsModificationResult result =
            _service.ChangeTurnTime(session, "Pedro", 60);

        // Assert
        TestAssert.AreEqual(MatchSettingsModificationResult.Success, result);
        TestAssert.AreEqual(60, session.TurnTimeSeconds);
        TestAssert.AreEqual("ABC123", session.Code);
        TestAssert.AreEqual(MatchState.Waiting, session.State);
        TestAssert.AreEqual(true, ReferenceEquals(originalBoard, session.Board));
    }

    internal void ChangeTurnTime_StartedMatch_PreservesTime()
    {
        // Arrange
        MatchSession session = CreateSession();
        session.StartAgainstSystem("System");

        // Act
        MatchSettingsModificationResult result =
            _service.ChangeTurnTime(session, "Pedro", 60);

        // Assert
        TestAssert.AreEqual(MatchSettingsModificationResult.AlreadyStarted, result);
        TestAssert.AreEqual(40, session.TurnTimeSeconds);
    }

    internal void ChangeTurnTime_DifferentPlayer_PreservesTime()
    {
        // Arrange
        MatchSession session = CreateSession();

        // Act
        MatchSettingsModificationResult result =
            _service.ChangeTurnTime(session, "Cesar", 60);

        // Assert
        TestAssert.AreEqual(MatchSettingsModificationResult.Unauthorized, result);
        TestAssert.AreEqual(40, session.TurnTimeSeconds);
    }

    internal void ChangeTurnTime_InvalidValue_PreservesTime()
    {
        // Arrange
        MatchSession session = CreateSession();

        // Act
        MatchSettingsModificationResult result =
            _service.ChangeTurnTime(session, "Pedro", 121);

        // Assert
        TestAssert.AreEqual(MatchSettingsModificationResult.InvalidTurnTime, result);
        TestAssert.AreEqual(40, session.TurnTimeSeconds);
    }

    private static MatchSession CreateSession()
    {
        return new MatchSession("ABC123", 40, "Pedro", null);
    }
}

internal sealed class AvailableMatchesServiceTests
{
    internal void FilterJoinable_MixedMatches_ReturnsOnlyOpenHumanMatches()
    {
        // Arrange
        AvailableMatch[] matches =
        {
            new AvailableMatch(
                "open", "AB2C3D", "Ana", AvailableMatchState.Waiting, 1, false, 40),
            new AvailableMatch(
                "full", "EF4G5H", "Luis", AvailableMatchState.Waiting, 2, false, 40),
            new AvailableMatch(
                "system", "JK6L7M", "Gael", AvailableMatchState.Waiting, 1, true, 40),
            new AvailableMatch(
                "playing", "NP8Q9R", "Luna", AvailableMatchState.InProgress, 1, false, 40),
            new AvailableMatch(
                "own", "ST2U3V", "Pedro", AvailableMatchState.Waiting, 1, false, 40)
        };

        // Act
        IReadOnlyList<AvailableMatch> actual =
            AvailableMatchesService.FilterJoinable(matches, "Pedro");

        // Assert
        TestAssert.AreEqual(1, actual.Count);
        TestAssert.AreEqual("open", actual[0].Id);
    }
}

internal sealed class TurnTimePolicyTests
{
    internal void TryParse_DefaultValue_ReturnsTrue()
    {
        // Arrange

        // Act
        bool valid = TurnTimePolicy.TryParse("40", out int seconds);

        // Assert
        TestAssert.AreEqual(true, valid);
        TestAssert.AreEqual(40, seconds);
    }

    internal void TryParse_MinimumAndMaximum_ReturnTrue()
        // Act
        // Arrange
    {
        // Assert
        TestAssert.AreEqual(true, TurnTimePolicy.TryParse("10", out _));
        TestAssert.AreEqual(true, TurnTimePolicy.TryParse("120", out _));
    }

    internal void TryParse_OutsideRange_ReturnsFalse()
        // Act
        // Arrange
    {
        // Assert
        TestAssert.AreEqual(false, TurnTimePolicy.TryParse("9", out _));
        TestAssert.AreEqual(false, TurnTimePolicy.TryParse("121", out _));
    }

    internal void TryParse_NonInteger_ReturnsFalse()
        // Act
        // Arrange
    {
        // Assert
        TestAssert.AreEqual(false, TurnTimePolicy.TryParse("40.5", out _));
        TestAssert.AreEqual(false, TurnTimePolicy.TryParse("forty", out _));
    }
}

internal sealed class ReversiBoardTests
{
    internal void CreateInitial_HasStandardFourCenterDiscs()
    {
        // Arrange

        // Act
        ReversiBoard board = ReversiBoard.CreateInitial();

        // Assert
        TestAssert.AreEqual(DiscColor.White, board[3, 3]);
        TestAssert.AreEqual(DiscColor.Black, board[3, 4]);
        TestAssert.AreEqual(DiscColor.Black, board[4, 3]);
        TestAssert.AreEqual(DiscColor.White, board[4, 4]);
    }

    internal void CreateInitial_HasTwoDiscsOfEachColor()
    {
        // Arrange

        // Act
        ReversiBoard board = ReversiBoard.CreateInitial();

        // Assert
        TestAssert.AreEqual(2, board.Count(DiscColor.Black));
        TestAssert.AreEqual(2, board.Count(DiscColor.White));
        TestAssert.AreEqual(60, board.Count(DiscColor.Empty));
    }

    internal void GetValidMoves_InitialBlackTurn_ReturnsFourPositions()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.CreateInitial();

        // Act
        IReadOnlyList<BoardPosition> moves = board.GetValidMoves(DiscColor.Black);

        // Assert
        TestAssert.AreEqual(4, moves.Count);
        TestAssert.AreEqual(true, Contains(moves, new BoardPosition(2, 3)));
        TestAssert.AreEqual(true, Contains(moves, new BoardPosition(3, 2)));
        TestAssert.AreEqual(true, Contains(moves, new BoardPosition(4, 5)));
        TestAssert.AreEqual(true, Contains(moves, new BoardPosition(5, 4)));
    }

    internal void TryApplyMove_ValidMove_FlipsBracketedPiece()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.CreateInitial();

        // Act
        bool applied = board.TryApplyMove(
            DiscColor.Black,
            new BoardPosition(2, 3),
            out ReversiBoard? updatedBoard);

        // Assert
        TestAssert.AreEqual(true, applied);
        TestAssert.AreEqual(true, updatedBoard != null);
        TestAssert.AreEqual(4, updatedBoard!.Count(DiscColor.Black));
        TestAssert.AreEqual(1, updatedBoard.Count(DiscColor.White));
        TestAssert.AreEqual(2, board.Count(DiscColor.Black));
        TestAssert.AreEqual(2, board.Count(DiscColor.White));
    }

    internal void TryApplyMove_MultipleDirections_FlipsEveryLine()
    {
        // Arrange
        ReversiBoard board = ReversiBoard.FromRows(
            "........", ".B.B.B..", "..WWW...", ".BW.WB..",
            "..WWW...", ".B.B.B..", "........", "........");

        // Act
        bool applied = board.TryApplyMove(
            DiscColor.Black,
            new BoardPosition(3, 3),
            out ReversiBoard? updatedBoard);

        // Assert
        TestAssert.AreEqual(true, applied);
        TestAssert.AreEqual(true, updatedBoard != null);
        TestAssert.AreEqual(17, updatedBoard!.Count(DiscColor.Black));
        TestAssert.AreEqual(0, updatedBoard.Count(DiscColor.White));
    }

    internal void FromRows_ValidSnapshot_ParsesEveryCell()
    {
        // Arrange

        // Act
        ReversiBoard board = ReversiBoard.FromRows(
            "B.......", ".W......", "........", "........",
            "........", "........", "........", ".......B");

        // Assert
        TestAssert.AreEqual(2, board.Count(DiscColor.Black));
        TestAssert.AreEqual(1, board.Count(DiscColor.White));
        TestAssert.AreEqual(61, board.Count(DiscColor.Empty));
    }

    internal void FromRows_InvalidCell_ThrowsArgumentException()
    {
        // Arrange

        // Act
        bool threw = false;
        try
        {
            ReversiBoard.FromRows(
                "X.......", "........", "........", "........",
                "........", "........", "........", "........");
        }
        catch (ArgumentException)
        {
            threw = true;
        }

        // Assert
        TestAssert.AreEqual(true, threw);
    }

    private static bool Contains(
        IReadOnlyList<BoardPosition> positions,
        BoardPosition expected)
    {
        foreach (BoardPosition position in positions)
        {
            if (position.Equals(expected))
            {
                return true;
            }
        }

        return false;
    }
}

internal sealed class EmailPolicyTests
{
    internal void Validate_OrdinaryAddress_ReturnsNone()
    {
        // Arrange
        string email = "player@example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.None, actual);
    }

    internal void Validate_TaggedAddress_ReturnsNone()
    {
        // Arrange
        string email = "first.last+tag@example.co.uk";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.None, actual);
    }

    internal void Validate_InternationalizedDomain_ReturnsNone()
    {
        // Arrange
        string email = "player@bücher.de";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.None, actual);
    }

    internal void Validate_AddressAtMaximumLength_ReturnsNone()
    {
        // Arrange
        string label = new string('a', 61);
        string email = new string('b', 64) + "@" + label + "." + label + "." + label + ".com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.None, actual);
    }

    internal void Validate_AddressAboveMaximumLength_ReturnsTooLong()
    {
        // Arrange
        string email = new string('a', 250) + "@example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.TooLong, actual);
    }

    internal void Validate_MissingAtSign_ReturnsInvalidStructure()
    {
        // Arrange
        string email = "player.example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidStructure, actual);
    }

    internal void Validate_MultipleAtSigns_ReturnsInvalidStructure()
    {
        // Arrange
        string email = "player@@example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidStructure, actual);
    }

    internal void Validate_ConsecutiveLocalDots_ReturnsInvalidLocalPart()
    {
        // Arrange
        string email = "first..last@example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidLocalPart, actual);
    }

    internal void Validate_DisplayName_ReturnsInvalidLocalPart()
    {
        // Arrange
        string email = "Player <player@example.com>";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidLocalPart, actual);
    }

    internal void Validate_LocalPartAboveMaximum_ReturnsInvalidLocalPart()
    {
        // Arrange
        string email = new string('a', 65) + "@example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidLocalPart, actual);
    }

    internal void Validate_AccentedLocalPart_ReturnsInvalidLocalPart()
    {
        // Arrange
        string email = "josé@example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidLocalPart, actual);
    }

    internal void Validate_SingleLabelDomain_ReturnsInvalidDomain()
    {
        // Arrange
        string email = "player@localhost";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidDomain, actual);
    }

    internal void Validate_HyphenAtDomainStart_ReturnsInvalidDomain()
    {
        // Arrange
        string email = "player@-example.com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidDomain, actual);
    }

    internal void Validate_ConsecutiveDomainDots_ReturnsInvalidDomain()
    {
        // Arrange
        string email = "player@example..com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidDomain, actual);
    }

    internal void Validate_TrailingDomainDot_ReturnsInvalidDomain()
    {
        // Arrange
        string email = "player@example.com.";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidDomain, actual);
    }

    internal void Validate_DomainLabelAboveMaximum_ReturnsInvalidDomain()
    {
        // Arrange
        string email = "player@" + new string('a', 64) + ".com";

        // Act
        EmailValidationError actual = EmailPolicy.Validate(email);

        // Assert
        TestAssert.AreEqual(EmailValidationError.InvalidDomain, actual);
    }
}

internal sealed class PasswordPolicyTests
{
    internal void Validate_SevenCharacters_ReturnsTooShort()
    {
        // Arrange
        string password = "Aa1234!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.TooShort, actual);
    }

    internal void Validate_EightCharacters_ReturnsNone()
    {
        // Arrange
        string password = "Aa12345!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    internal void Validate_AsteriskAsSymbol_ReturnsNone()
    {
        // Arrange
        string password = "Aa12345*";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    internal void Validate_UnicodeLettersAndDigits_ReturnsNone()
    {
        // Arrange
        string password = "Áb١٢٣٤٥!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    internal void Validate_OneHundredTwentyEightCharacters_ReturnsNone()
    {
        // Arrange
        string password = "Aa1!" + new string('x', 124);

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    internal void Validate_OneHundredTwentyNineCharacters_ReturnsTooLong()
    {
        // Arrange
        string password = "Aa1!" + new string('x', 125);

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.TooLong, actual);
    }

    internal void Validate_SupplementarySymbols_CountsCodePoints()
    {
        // Arrange
        string password = "Aa12345😀";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.None, actual);
    }

    internal void Validate_WithoutUppercase_ReturnsMissingUppercaseLetter()
    {
        // Arrange
        string password = "lower123!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingUppercaseLetter, actual);
    }

    internal void Validate_WithoutLowercase_ReturnsMissingLowercaseLetter()
    {
        // Arrange
        string password = "UPPER123!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingLowercaseLetter, actual);
    }

    internal void Validate_WithoutDigit_ReturnsMissingDigit()
    {
        // Arrange
        string password = "Uppercase!";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingDigit, actual);
    }

    internal void Validate_WithoutSymbol_ReturnsMissingSymbol()
    {
        // Arrange
        string password = "Upper123";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingSymbol, actual);
    }

    internal void Validate_SpaceWithoutSymbol_ReturnsMissingSymbol()
    {
        // Arrange
        string password = "Aa12345 ";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.MissingSymbol, actual);
    }

    internal void Validate_ControlCharacter_ReturnsUnsupportedCharacter()
    {
        // Arrange
        string password = "Aa1234!\n";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.UnsupportedCharacter, actual);
    }

    internal void Validate_InvisibleFormatCharacter_ReturnsUnsupportedCharacter()
    {
        // Arrange
        string password = "Aa1234!\u200B";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.UnsupportedCharacter, actual);
    }

    internal void Validate_LineSeparator_ReturnsUnsupportedCharacter()
    {
        // Arrange
        string password = "Aa1234!\u2028";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.UnsupportedCharacter, actual);
    }

    internal void Validate_UnpairedSurrogate_ReturnsUnsupportedCharacter()
    {
        // Arrange
        string password = "Aa1234!\uD800";

        // Act
        PasswordValidationError actual = PasswordPolicy.Validate(password);

        // Assert
        TestAssert.AreEqual(PasswordValidationError.UnsupportedCharacter, actual);
    }
}

internal sealed class UsernamePolicyTests
{
    internal void Validate_ValidUsername_ReturnsNone()
    {
        // Arrange
        string username = "Pedro123";

        // Act
        UsernameValidationError actual = UsernamePolicy.Validate(username);

        // Assert
        TestAssert.AreEqual(UsernameValidationError.None, actual);
    }

    internal void Validate_UsernameWithUnderscore_ReturnsNone()
    {
        // Arrange
        string username = "Pedro_123";

        // Act
        UsernameValidationError actual = UsernamePolicy.Validate(username);

        // Assert
        TestAssert.AreEqual(UsernameValidationError.None, actual);
    }

    internal void Validate_EmptyUsername_ReturnsMissing()
    {
        // Arrange
        string username = " ";

        // Act
        UsernameValidationError actual = UsernamePolicy.Validate(username);

        // Assert
        TestAssert.AreEqual(UsernameValidationError.Missing, actual);
    }

    internal void Validate_TooShortUsername_ReturnsInvalidLength()
    {
        // Arrange
        string username = "ab";

        // Act
        UsernameValidationError actual = UsernamePolicy.Validate(username);

        // Assert
        TestAssert.AreEqual(UsernameValidationError.InvalidLength, actual);
    }

    internal void Validate_TooLongUsername_ReturnsInvalidLength()
    {
        // Arrange
        string username = new string('a', UsernamePolicy.MaximumLength + 1);

        // Act
        UsernameValidationError actual = UsernamePolicy.Validate(username);

        // Assert
        TestAssert.AreEqual(UsernameValidationError.InvalidLength, actual);
    }

    internal void Validate_LeadingUnderscore_ReturnsInvalidCharacters()
    {
        // Arrange
        string username = "_pedro";

        // Act
        UsernameValidationError actual = UsernamePolicy.Validate(username);

        // Assert
        TestAssert.AreEqual(UsernameValidationError.InvalidCharacters, actual);
    }

    internal void Validate_UsernameWithSpace_ReturnsInvalidCharacters()
    {
        // Arrange
        string username = "Pedro 123";

        // Act
        UsernameValidationError actual = UsernamePolicy.Validate(username);

        // Assert
        TestAssert.AreEqual(UsernameValidationError.InvalidCharacters, actual);
    }
}

internal sealed class GuestUsernamePolicyTests
{
    internal void Validate_ValidName_ReturnsNone()
        // Act
        // Arrange
    {
        // Assert
        TestAssert.AreEqual(
            UsernameValidationError.None,
            GuestUsernamePolicy.Validate("Pedro_27"));
    }

    internal void Validate_WhitespaceOnly_ReturnsMissing()
        // Act
        // Arrange
    {
        // Assert
        TestAssert.AreEqual(
            UsernameValidationError.Missing,
            GuestUsernamePolicy.Validate("   "));
    }

    internal void Normalize_NameWithOuterWhitespace_ReturnsBaseName()
        // Act
        // Arrange
    {
        // Assert
        TestAssert.AreEqual(
            "Pedro",
            GuestUsernamePolicy.Normalize("  Pedro  "));
    }
}

internal sealed class GuestSessionServiceTests
{
    internal void TryCreate_ValidName_CreatesActiveTemporarySession()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore();

        // Act
        GuestSessionCreationResult result = store.TryCreate("Pedro", out GuestSession? session);

        // Assert
        TestAssert.AreEqual(GuestSessionCreationResult.Success, result);
        TestAssert.AreEqual("Pedro", session?.Name);
        TestAssert.AreEqual(true, session?.IsActive);
    }

    internal void TryCreate_DuplicateNameIgnoringCase_ReturnsAlreadyInUse()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore();
        store.TryCreate("Pedro", out _);

        // Act
        GuestSessionCreationResult result = store.TryCreate("pedro", out GuestSession? duplicate);

        // Assert
        TestAssert.AreEqual(GuestSessionCreationResult.NameAlreadyInUse, result);
        TestAssert.AreEqual<GuestSession?>(null, duplicate);
    }

    internal void End_ExistingSession_ReleasesName()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore();
        store.TryCreate("Pedro", out GuestSession? first);
        store.End(first);

        // Act
        GuestSessionCreationResult result = store.TryCreate("Pedro", out GuestSession? replacement);

        // Assert
        TestAssert.AreEqual(false, first?.IsActive);
        TestAssert.AreEqual(GuestSessionCreationResult.Success, result);
        TestAssert.AreEqual(true, replacement?.IsActive);
    }

    internal void TryCreate_InvalidName_DoesNotCreateSession()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore();

        // Act
        GuestSessionCreationResult result = store.TryCreate(
            "Guest name",
            out GuestSession? session);

        // Assert
        TestAssert.AreEqual(GuestSessionCreationResult.InvalidNameFormat, result);
        TestAssert.AreEqual<GuestSession?>(null, session);
    }

    internal void TryCreate_UnavailableStore_ReturnsCreationError()
    {
        // Arrange
        GuestSessionStore store = new GuestSessionStore(false);

        // Act
        GuestSessionCreationResult result = store.TryCreate("Pedro", out GuestSession? session);

        // Assert
        TestAssert.AreEqual(GuestSessionCreationResult.CreationError, result);
        TestAssert.AreEqual<GuestSession?>(null, session);
    }
}

internal sealed class PlayerStatisticsTests
{
    internal void FromCompletedMatches_Empty_ReturnsZeros()
    {
        // Arrange
        CompletedMatchOutcome[] outcomes = Array.Empty<CompletedMatchOutcome>();

        // Act
        PlayerStatistics actual = PlayerStatistics.FromCompletedMatches(outcomes);

        // Assert
        TestAssert.AreEqual(0, actual.MatchesPlayed);
        TestAssert.AreEqual(0, actual.Wins);
        TestAssert.AreEqual(0, actual.Losses);
        TestAssert.AreEqual(0, actual.Draws);
        TestAssert.AreEqual(0d, actual.WinRate);
    }

    internal void FromCompletedMatches_MixedResults_CountsEveryOutcome()
    {
        // Arrange
        CompletedMatchOutcome[] outcomes =
        {
            CompletedMatchOutcome.Win,
            CompletedMatchOutcome.Draw,
            CompletedMatchOutcome.Loss,
            CompletedMatchOutcome.Win
        };

        // Act
        PlayerStatistics actual = PlayerStatistics.FromCompletedMatches(outcomes);

        // Assert
        TestAssert.AreEqual(4, actual.MatchesPlayed);
        TestAssert.AreEqual(2, actual.Wins);
        TestAssert.AreEqual(1, actual.Losses);
        TestAssert.AreEqual(1, actual.Draws);
        TestAssert.AreEqual(0.5d, actual.WinRate);
    }

    internal void FromCompletedMatches_NoWins_ReturnsZeroWinRate()
    {
        // Arrange
        CompletedMatchOutcome[] outcomes =
        {
            CompletedMatchOutcome.Loss,
            CompletedMatchOutcome.Draw
        };

        // Act
        PlayerStatistics actual = PlayerStatistics.FromCompletedMatches(outcomes);

        // Assert
        TestAssert.AreEqual(2, actual.MatchesPlayed);
        TestAssert.AreEqual(0d, actual.WinRate);
    }
}

internal static class TestAssert
{
    internal static void AreEqual<TValue>(TValue expected, TValue actual)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, but received {actual}.");
        }
    }
}
