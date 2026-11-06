using System;

namespace ReversiGameSystem.Server.Tests;

internal static class Program
{
    private static int Main(string[] args)
    {
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
                + "[--database|--review-accounts|--friendship-host|--match-host|--match-database]");
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
        SessionServiceTests sessionTests = new SessionServiceTests();
        VerificationCodeServiceTests codeTests = new VerificationCodeServiceTests();
        VerificationEmailSenderTests mailTests = new VerificationEmailSenderTests();
        FriendshipPresenceTests presenceTests = new FriendshipPresenceTests();
        ChatContentPolicyTests chatPolicyTests = new ChatContentPolicyTests();
        MatchBoundaryTests matchBoundaryTests = new MatchBoundaryTests();
        MatchHandlerLifecycleTests lifecycleTests = new MatchHandlerLifecycleTests();
        LobbyAuthorizationTests lobbyAuthorizationTests = new LobbyAuthorizationTests();
        ServiceErrorBehaviorTests errorBehaviorTests = new ServiceErrorBehaviorTests();
        System.Collections.Generic.List<Action> tests =
            new System.Collections.Generic.List<Action>
        {
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
            chatPolicyTests.Matches_AccentsCaseAndPunctuation_MatchesCompletePhrase,
            chatPolicyTests.Matches_WordInsideAnotherWord_DoesNotMatch,
            chatPolicyTests.Matches_EmptyPattern_DoesNotBlockEveryMessage,
            matchBoundaryTests.Gameplay_UnauthorizedParticipant_PreservesDeclaredRejection,
            matchBoundaryTests.Gameplay_InitialState_UsesApplicationSnapshot,
            matchBoundaryTests.Gameplay_UnsupportedWrite_ReturnsDeclaredServiceFault,
            matchBoundaryTests.Lobby_StoreFailure_ReturnsSanitizedServiceFault,
            matchBoundaryTests.Lobby_InviteToMatch_GuestSession_ReturnsMatchFaultUnauthorized,
            matchBoundaryTests.Lobby_InviteToMatch_SelfInvitation_ReturnsMatchFaultUnauthorized,
            matchBoundaryTests.Lobby_InviteToMatch_InvalidFriendId_ReturnsFriendshipFaultInvalidAccountId,
            matchBoundaryTests.Lobby_InviteToMatch_AccountUnavailable_ReturnsFriendshipFaultAccountUnavailable,
            matchBoundaryTests.Lobby_InviteToMatch_FriendshipNotFound_ReturnsFriendshipFaultFriendshipNotFound,
            matchBoundaryTests.Lobby_InviteToMatch_StoreFailure_ReturnsSanitizedServiceFault,
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
            sessionTests.RequestLoginCode_ThroughWcfChannel_ReturnsExpiryWithoutCreatingSession,
            sessionTests.CompleteLogin_ThroughWcfChannel_CreatesMappedSessionAndRevokesIt,
            sessionTests.RequestLoginCode_InvalidCredentialsThroughWcf_ReturnsTypedSessionFault,
            sessionTests.RequestLoginCode_DeliveryTimeoutThroughWcf_ReturnsSanitizedServiceFault,
            sessionTests.CompleteLogin_ExpiredCodeThroughWcf_ReturnsTypedExpiryFault,
            sessionTests.CompleteLogin_InactiveAccount_RejectsSessionCreation,
            sessionTests.CompleteLogin_RepositoryRejectsConsumedCode_DoesNotCreateSession,
            sessionTests.OpenGuestSession_ExpiredGuest_ReleasesReservedName,
            sessionTests.OpenGuestSession_ThroughWcfChannel_ReservesAndReleasesName,
            sessionTests.OpenGuestSession_ConcurrentSameName_CreatesOnlyOneGuest,
            sessionTests.CloseSession_EmptyTokenThroughWcf_ReturnsTypedFault,
            sessionTests.CloseSession_FailingObserver_NotifiesRemainingObservers,
            codeTests.Request_ValidIdentity_DeliversSixDigitsWithFiveMinuteExpiry,
            codeTests.Request_DeliveryTimeout_DoesNotReplacePendingCode,
            codeTests.Consume_ExpiredRepositoryResult_PreservesRejectionAndServerTime,
            codeTests.Protect_DifferentIdentity_BindsCodeToAccountAndPurpose,
            mailTests.Load_ProcessConfiguration_ValidatesPortAndRequiresEncryption,
            mailTests.Send_LocalServerWithheldBanner_CancelsWithinDeliveryBudget,
            FailureDiagnosticsTests.Describe_NestedProviderFailure_ExcludesMessagesAndPrivateData,
            DiagnosticFormatterTests.Run,
            presenceTests.DuplexChannels_PublishPresenceAndOwnFriendSnapshot,
            presenceTests.MultipleSessions_StayOnlineUntilLastRevocation,
            presenceTests.ClosedAndReplacementChannels_PreservePresence,
            presenceTests.RelationshipFailure_PreservesHealthyChannelAndAllowsPresenceRecovery,
            presenceTests.RepositoryFailure_LogsOriginalExceptionAndReturnsDeclaredFault,
            presenceTests.LoggerFailure_ReportsBothFailuresWithoutExposingProviderMessages,
            presenceTests.InvalidInactiveAndExpiredSessions_ReturnTypedFaults,
            presenceTests.UnsupportedOperations_ReturnDeclaredServiceFaultWithoutRepositoryAccess,
            presenceTests.ExpiredReceiver_ReceivesNoCallbacks,
            presenceTests.ExpirySweep_PublishesDisconnection,
            presenceTests.EndedFriendship_ReceivesNoPresenceCallbacks,
            PlayerBoundaryTests.RegisterAccount_ThroughWcf_MapsCredentialsAndOutcome,
            PlayerBoundaryTests.GetProfile_ThroughWcf_MapsIdentityAndStatistics,
            PlayerBoundaryTests.GetProfile_MissingAndInvalidSessionThroughWcf_ReturnsTypedFaults,
            PlayerBoundaryTests.GetProfile_InactiveCallerRequestingAnotherAccount_RejectsThroughWcf,
            PlayerBoundaryTests.GetProfile_InvalidTargetThroughWcf_ReturnsTypedFault,
            PlayerBoundaryTests.GetProfile_MissingTargetThroughWcf_ReturnsTypedFault,
            PlayerBoundaryTests.GetProfile_RepositoryFailureThroughWcf_ReturnsServiceFault,
            PlayerBoundaryTests.GetImage_AbsentImageThroughWcf_ReturnsNull,
            PlayerBoundaryTests.GetImage_PresentImageThroughWcf_ReturnsContent,
            PlayerBoundaryTests.GetImage_ReadFailureThroughWcf_ReturnsServiceFault,
            PlayerBoundaryTests.ChangeUsername_ThroughWcf_NormalizesAndUpdatesOnlyCaller,
            PlayerBoundaryTests.ChangeUsername_UnavailableThroughWcf_ReturnsTypedFault,
            PlayerBoundaryTests.UpdateImage_InvalidThroughWcf_ReturnsTypedFault,
            PlayerBoundaryTests.UpdateImage_AssociationFailureThroughWcf_RemovesNewImage,
            PlayerBoundaryTests.GetLeaderboard_ThroughWcf_MapsOrderedEntries,
            PlayerBoundaryTests.GetLeaderboard_InvalidLimitThroughWcf_ReturnsTypedFault,
            PlayerBoundaryTests.GetLeaderboard_UnavailableThroughWcf_ReturnsServiceFault,
            ModelResourcesTests.Assembly_RequiredRuntimeModel_IsEmbedded
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
