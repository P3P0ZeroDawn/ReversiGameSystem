using System;

namespace ReversiGameSystem.Server.Tests;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--preferences-database")
        {
            return RunTests(new Action[] { PreferencesDatabaseTests.Run });
        }
        if (args.Length == 1 && args[0] == "--match-database")
        {
            return RunTests(new Action[] { ReviewMatchDatabaseTests.Run });
        }
        if (args.Length == 1 && args[0] == "--match-host")
        {
            return RunTests(new Action[] { ReviewMatchHost.Run });
        }
        if (args.Length == 1 && args[0] == "--friendship-host")
        {
            return RunTests(new Action[] { ReviewPresenceHost.Run });
        }
        if (args.Length == 1 && args[0] == "--review-accounts")
        {
            return RunTests(new Action[] { ReviewAccountDatabaseTests.RunDisposableDatabaseChecks });
        }
        if (args.Length == 1 && args[0] == "--database")
        {
            return RunTests(new Action[] { VerificationDatabaseTests.RunDisposableDatabaseChecks });
        }
        if (args.Length != 0)
        {
            Console.Error.WriteLine("Usage: Server.Tests.exe "
                + "[--database|--review-accounts|--friendship-host|--match-host|--match-database"
                + "|--preferences-database]");
            return 2;
        }
        System.Collections.Generic.List<Action> tests =
            new System.Collections.Generic.List<Action>(CreateTests());
        tests.AddRange(LegacyTestSuite.CreateTests());
        return RunTests(tests.ToArray());
    }

    private static Action[] CreateTests()
    {
        ContractSerializationTests serializationTests = new ContractSerializationTests();
        ContractArchitectureTests architectureTests = new ContractArchitectureTests();
        MatchSerializationTests matchTests = new MatchSerializationTests();
        CommunicationSerializationTests communicationTests = new CommunicationSerializationTests();
        VerificationCodeServiceTests codeTests = new VerificationCodeServiceTests();
        VerificationEmailSenderTests mailTests = new VerificationEmailSenderTests();
        MatchBoundaryTests matchBoundaryTests = new MatchBoundaryTests();
        MatchApplicationServiceLifecycleTests lifecycleTests = new MatchApplicationServiceLifecycleTests();
        LobbyAuthorizationTests lobbyAuthorizationTests = new LobbyAuthorizationTests();
        IntegrationTests.Services.ServiceErrorBehaviorIntegrationTests errorBehaviorTests =
            new IntegrationTests.Services.ServiceErrorBehaviorIntegrationTests();
        GameplayMatchRulesTests gameplayRulesTests = new GameplayMatchRulesTests();
        GameplayApplicationServiceTests gameplayTests = new GameplayApplicationServiceTests();
        System.Collections.Generic.List<Action> tests =
            new System.Collections.Generic.List<Action>
        {
            gameplayTests.TrackStartedMatch_InitializesBoardAndTurnCorrectly,
            gameplayTests.PlacePiece_MissingPlacement_ReturnsMissingPlacement,
            gameplayTests.PlacePiece_NegativeVersion_ReturnsInvalidMatchStateVersion,
            gameplayTests.PlacePiece_InvalidSession_ReturnsInvalidSession,
            gameplayTests.PlacePiece_NotPlayersTurn_ReturnsNotPlayersTurn,
            gameplayTests.PlacePiece_StaleVersion_ReturnsStaleMatchStateVersion,
            gameplayTests.PlacePiece_InvalidPosition_ReturnsInvalidPosition,
            gameplayTests.PlacePiece_ValidMove_FlipsPiecesAdvancesTurnAndSynchronizesMatch,
            gameplayTests.PlacePiece_PersistenceUnavailable_PreservesAndRetriesConfirmedState,
            gameplayTests.PlacePiece_ResetsConsecutiveTimeoutCount,
            gameplayTests.ForfeitMatch_NonParticipant_ReturnsUnauthorized,
            gameplayTests.ForfeitMatch_Participant_ConcludesMatchWithAbandonment,
            gameplayTests.ForfeitMatch_AlreadyFinished_IsIdempotent,
            gameplayTests.ProcessTurnTimeouts_TurnNotExpired_DoesNothing,
            gameplayTests.ProcessTurnTimeouts_SingleTimeout_PlaysAutomaticMoveAndIncrementsTimeout,
            gameplayTests.ProcessTurnTimeouts_ThreeConsecutiveTimeouts_ConcludesMatchWithInactivity,
            gameplayTests.ObserveMatch_InProgress_ReturnsActiveMatch,
            gameplayTests.ObserveMatch_FinishedMatch_ReturnsSpectatingUnavailable,
            gameplayTests.GetMoveHistory_ReturnsOrderedMoves,
            gameplayTests.PlacePiece_OpponentHasNoLegalMoves_SkipsOpponentTurn,
            gameplayTests.PlacePiece_NoPlayerHasLegalMoves_ConcludesMatchNormally,
            gameplayTests.ProcessTurnTimeouts_DatabaseUnavailable_StillTransitionsExpiredTurns,
            gameplayTests.SynchronizePendingMatches_FinishedMatchPersisted_EvictsFromActiveMatches,
            gameplayTests.GetAuthorizedMatchSnapshot_Participant_ReturnsSnapshot,
            gameplayTests.GetAuthorizedMatchSnapshot_NonParticipant_ReturnsUnauthorized,
            gameplayTests.SynchronizePendingMatches_PersistenceFailureOnFirstMatch_StopsTickForBackoff,
            gameplayRulesTests.InitializeStartedMatch_ActiveMatch_SetsOpeningBoardAndBlackTurn,
            gameplayRulesTests.ValidatePlacement_NotInProgress_ReturnsMatchNotInProgress,
            gameplayRulesTests.ValidatePlacement_ValidTurnAndVersion_ReturnsAuthorizedParticipant,
            gameplayRulesTests.ApplyPlayerMove_ValidMove_UpdatesStateWithoutMutatingSnapshot,
            gameplayRulesTests.ApplyTimeoutIfExpired_ThirdTimeout_ConcludesInactivityAndAdvancesVersion,
            gameplayRulesTests.ForfeitByParticipant_SecondCall_ReturnsMatchFinishedWithoutMutatingVersion,
            gameplayRulesTests.CreateSnapshot_MutatingOriginalMatch_DoesNotAffectSnapshot,
            lobbyAuthorizationTests.Authorize_LiveSession_ChecksAccountWithoutListingMatches,
            lobbyAuthorizationTests.Authorize_ExpiredSession_RejectsBeforeAccountQuery,
            lobbyAuthorizationTests.Authorize_RevokedSession_RejectsBeforeAccountQuery,
            lobbyAuthorizationTests.OpenLobbyChannel_InactiveAccount_ReturnsMatchFaultWithoutListing,
            lobbyAuthorizationTests.OpenLobbyChannel_MissingToken_ReturnsSessionFaultBeforeAccountQuery,
            errorBehaviorTests.UnexpectedFailure_ThroughWcf_ReturnsSanitizedFaultAndLogsOnce,
            errorBehaviorTests.UnexpectedFailure_WithIncomingCorrelation_PreservesIdentifier,
            errorBehaviorTests.UnexpectedFailure_AfterCorrelationWasCreated_ReusesIdentifier,
            errorBehaviorTests.UndeclaredFault_ThroughWcf_IsSanitizedAndLogged,
            errorBehaviorTests.LoggingFailure_ThroughWcf_StillReturnsSanitizedFault,
            errorBehaviorTests.BusinessFault_ThroughWcf_PreservesTypeWithoutTechnicalLog,
            matchBoundaryTests.Gameplay_UnauthorizedParticipant_PreservesDeclaredRejection,
            matchBoundaryTests.Gameplay_InitialState_UsesApplicationSnapshot,
            matchBoundaryTests.Gameplay_StoreFailure_PropagatesToGlobalHandler,
            matchBoundaryTests.Lobby_StoreFailure_PropagatesToGlobalHandler,
            matchBoundaryTests.Lobby_InviteToMatch_GuestSession_ReturnsMatchFaultUnauthorized,
            matchBoundaryTests.Lobby_InviteToMatch_SelfInvitation_ReturnsMatchFaultUnauthorized,
            matchBoundaryTests.Lobby_InviteToMatch_InvalidFriendId_ReturnsFriendshipFaultInvalidAccountId,
            matchBoundaryTests.Lobby_InviteToMatch_AccountUnavailable_ReturnsFriendshipFaultAccountUnavailable,
            matchBoundaryTests.Lobby_InviteToMatch_FriendshipNotFound_ReturnsFriendshipFaultFriendshipNotFound,
            matchBoundaryTests.Lobby_InviteToMatch_StoreFailure_PropagatesToGlobalHandler,
            lifecycleTests.CancelWaitingForSession_TransientFailure_RetainsLobbyForRetry,
            lifecycleTests.CancelExpiredWaitingSessions_ChannelCleanupFailure_RetriesWhileSessionIsValid,
            lifecycleTests.CancelWaitingForSession_JoinAlreadyStarted_PreservesStartedMatch,
            lifecycleTests.Create_ExpiredSession_DoesNotCreateLobby,
            serializationTests.AuthenticatedSession_RoundTrip_PreservesAccountAndExpiry,
            serializationTests.AccountRegistrationCredentials_RoundTrip_PreservesProfileImage,
            serializationTests.PlayerProfile_RoundTrip_PreservesStatistics,
            serializationTests.LeaderboardEntry_RoundTrip_PreservesPositions,
            serializationTests.ServiceFault_RoundTrip_PreservesDiagnosticData,
            architectureTests.ServiceContracts_CatalogSignatures_PreserveWireBoundary,
            architectureTests.DataContracts_CatalogMembers_PreserveRequiredShape,
            architectureTests.Enums_CatalogValues_PreserveSerializedVocabulary,
            architectureTests.ServiceFault_RetainedRegistration_PreservesRequiredTransientMember,
            architectureTests.ContractAssembly_ReferencedAssemblies_ExcludePersistence,
            matchTests.MatchStateSnapshot_ActiveTurn_PreservesBoardAndVersion,
            matchTests.MatchStateSnapshot_FinishedDraw_PreservesConclusionAndAbsentTurn,
            matchTests.MatchInformation_ObserverAndGuest_PreservesOptionalIdentities,
            matchTests.MatchDetails_ReplayFrames_PreservesMoveAndFrameOrder,
            matchTests.MatchSummary_ActiveAndFinishedHistory_PreservesOptionalConclusion,
            matchTests.MatchDetails_EmptyHistory_PreservesEmptyArrays,
            communicationTests.NewAccount_ProfileImage_PreservesContentAndVerificationData,
            communicationTests.NewAccount_AbsentProfileImage_PreservesNull,
            communicationTests.PlayerPreferences_LocalZone_PreservesZoneAndBackground,
            communicationTests.GuestSession_RoundTrip_PreservesConfirmedIdentityAndExpiry,
            communicationTests.FriendRequest_RoundTrip_PreservesRequestAndBothAccounts,
            communicationTests.MatchChatEntry_RoundTrip_PreservesCompositeIdentityAndUnicode,
            communicationTests.ChatFault_MutedAndBlocked_PreservesOptionalMuteExpiry,
            communicationTests.ChatMuteNotification_RoundTrip_PreservesAccountSanctionWindow,
            communicationTests.BusinessFaults_EveryCatalogReason_RoundTripsTypedReason,
            codeTests.IssueVerificationCode_ValidIdentity_DeliversSixDigitsWithFiveMinuteExpiry,
            codeTests.IssueVerificationCode_DeliveryTimeout_DoesNotReplacePendingCode,
            codeTests.VerifyCode_ExactExpiry_RejectsExpiredCode,
            codeTests.VerifyCode_FiveFailures_InvalidatesCodeOnFifthAttempt,
            codeTests.VerifyCode_ValidCodeUsedOnce_RejectsReuse,
            codeTests.ComputeCodeDigest_DifferentIdentity_BindsCodeToAccountAndPurpose,
            codeTests.IssueVerificationCode_SpanishAndDefaultLocales_RenderSingleLanguageEmails,
            mailTests.Load_ConfiguredPort_ReturnsConfiguredPort,
            mailTests.Send_LocalServerWithheldBanner_CancelsWithinDeliveryBudget,
            new UnitTests.Services.FailureDiagnosticsTests()
                .Describe_NestedProviderFailure_ExcludesPrivateData,
            DiagnosticFormatterTests.Run,
            () => new ModelResourcesTests().Assembly_RequiredRuntimeModel_IsEmbedded("Model.Database_model.csdl"),
            () => new ModelResourcesTests().Assembly_RequiredRuntimeModel_IsEmbedded("Model.Database_model.ssdl"),
            () => new ModelResourcesTests().Assembly_RequiredRuntimeModel_IsEmbedded("Model.Database_model.msl")
        };
        tests.AddRange(MatchInvitationTests.CreateTests());
        return tests.ToArray();
    }

    private static int RunTests(Action[] tests)
    {
        try
        {
            foreach (Action test in tests)
            {
                test();
            }

            Console.WriteLine($"Passed {tests.Length} server contract tests.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
