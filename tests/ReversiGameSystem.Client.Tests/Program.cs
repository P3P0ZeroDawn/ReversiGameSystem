using System;

namespace ReversiGameSystem.Client.Tests;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--match-screen")
        {
            return Run(new Action[]
            {
                new MatchScreenTests().ExistingGamePage_RealMatch_RendersPlayersBoardAndChat
            });
        }
        if (args.Length == 1 && args[0] == "--match-end-to-end")
        {
            return Run(new Action[] { new MatchEndToEndTests().TwoPlayers_RealServer_StartsConfirmedBoardAndExchangesChat });
        }
        if (args.Length == 1 && args[0] == "--friendship-end-to-end")
        {
            return Run(new Action[]
            {
                new FriendshipEndToEndTests().Presence_RealAccounts_ReceivesLoginAndSessionClosureCallbacks
            });
        }
        if (args.Length == 1 && args[0] == "--session-integration")
        {
            return Run(new Action[]
            {
                new SessionConnectionTests().GuestLifecycle_ThroughGeneratedProxy
            });
        }
        if (args.Length != 0)
        {
            Console.Error.WriteLine("Usage: Client.Tests.exe "
                + "[--session-integration|--friendship-end-to-end|--match-end-to-end|--match-screen]");
            return 2;
        }
        SessionModelsTests sessionModelsTests = new SessionModelsTests();
        ServerConnectionFactoryTests connectionFactoryTests =
            new ServerConnectionFactoryTests();
        AccountCreationViewModelTests accountCreationTests =
            new AccountCreationViewModelTests();
        RemoteCallTests remoteCallTests = new RemoteCallTests();
        RemoteMatchMessagesTests remoteMatchMessagesTests = new RemoteMatchMessagesTests();
        LobbySettingsTests lobbySettingsTests = new LobbySettingsTests();
        ClientErrorHandlerTests clientErrorHandlerTests = new ClientErrorHandlerTests();
        PlayerPreferencesClientTests playerPreferencesClientTests = new PlayerPreferencesClientTests();
        GameViewModelRecoveryTests gameViewModelRecoveryTests = new GameViewModelRecoveryTests();
        GameViewModelTests gameViewModelTests = new GameViewModelTests();
        GameplayClientTransportTests gameplayClientTransportTests = new GameplayClientTransportTests();
        MatchClientsTests matchClientsTests = new MatchClientsTests();
        ClientArchitectureTests clientArchitectureTests = new ClientArchitectureTests();
        FailureDiagnosticsTests failureDiagnosticsTests = new FailureDiagnosticsTests();
        FriendshipSnapshotTests friendshipSnapshotTests = new FriendshipSnapshotTests();
        FriendshipLocalizationTests friendshipLocalizationTests = new FriendshipLocalizationTests();
        FriendshipConnectionTests friendshipConnectionTests = new FriendshipConnectionTests();
        MatchInvitationClientTests matchInvitationClientTests = new MatchInvitationClientTests();
        Action[] tests =
        {
            remoteCallTests.SendServiceCall_Timeout_PreservesCauseAndAddsCorrelation,
            remoteCallTests.SendServiceCall_ConnectionFailure_PreservesCauseAndAddsCorrelation,
            remoteCallTests.SendServiceCall_ProgrammingFailure_PropagatesOriginalException,
            remoteMatchMessagesTests.MatchStatuses_SupportedCultures_HaveLocalizedMessages,
            remoteMatchMessagesTests.ChatStatuses_SupportedCultures_HaveLocalizedMessages,
            remoteMatchMessagesTests.MatchNotFound_ResolvesSpecificMessage,
            remoteMatchMessagesTests.UnknownMatchStatus_ThrowsInsteadOfInventingRejection,
            remoteMatchMessagesTests.UnknownChatStatus_ThrowsInsteadOfInventingRejection,
            accountCreationTests.RequestRegistrationCode_MissingInformation_DoesNotCallServer,
            accountCreationTests.RequestRegistrationCode_ValidInput_NormalizesAndCallsServer,
            accountCreationTests
                .RequestRegistrationCode_ConcurrentSubmission_ReturnsSubmissionInProgress,
            lobbySettingsTests.ChangeTurnDuration_UnreachableServer_ReturnsServiceUnavailable,
            lobbySettingsTests.RemoteMatchMessages_InvalidTurnDuration_ResolvesExpectedResourceKey,
            lobbySettingsTests.LocalizationResources_MatchSettingsKeys_ExistInSupportedCultures,
            clientErrorHandlerTests.Callback_ConnectionFailure_RecordsCauseWithoutFatalNotification,
            clientErrorHandlerTests.Callback_ProgrammingFailure_ReachesFatalBoundary,
            clientErrorHandlerTests.ReportFatalFailure_ProgrammingError_DisplaysFatalMessage,
            clientErrorHandlerTests.ReportFatalFailure_UnhandledDeclaredFault_DisplaysFatalMessage,
            clientErrorHandlerTests.IsConnectionFailure_UnknownFault_DoesNotTreatAsDisconnection,
            clientErrorHandlerTests.IsConnectionFailure_ProgrammingError_DoesNotTreatAsDisconnection,
            clientErrorHandlerTests.CanMapFailure_UnknownMatchCode_DoesNotInventStatus,
            clientErrorHandlerTests.FileLogger_AggregateFailure_PreservesCausesWithoutSecrets,
            clientErrorHandlerTests.RecordFailure_PrimaryLoggerFails_PersistsBothCausesAndCorrelation,
            clientErrorHandlerTests.FileLogger_PrimaryFileUnavailable_PersistsOriginalEventInBackup,
            clientErrorHandlerTests.RecordFailure_AllMediaUnavailable_ReportsUnavailableDiagnostics,
            playerPreferencesClientTests.ChangeLanguage_ExistingPreferences_PreservesOtherSettings,
            playerPreferencesClientTests.ChangeLanguage_InvalidLocale_ReturnsDeclaredStatus,
            playerPreferencesClientTests.ChangeLanguage_RejectedWrite_DoesNotRetry,
            playerPreferencesClientTests.ChangeLanguage_UnknownFault_PropagatesFailure,
            playerPreferencesClientTests.ChangeLanguage_ServiceFault_PreservesServerCorrelation,
            playerPreferencesClientTests.ChangeLanguage_InvalidClientConfiguration_PropagatesFailure,
            playerPreferencesClientTests.CreatePreferencesClient_CustomHost_UsesPreferencesEndpoint,
            gameViewModelRecoveryTests.ConnectionLost_UnexpectedRecoveryFault_ReportsBeforeViewDeactivation,
            gameViewModelTests.UpdateClock_ActiveMute_MarksPlayerMuted,
            gameViewModelTests.UpdateClock_ExpiredMute_ClearsMute,
            gameViewModelTests.SendChatMessage_MutedPlayer_ReturnsPlayerMuted,
            gameplayClientTransportTests.Callback_ProcessingFailure_ReachesErrorBoundary,
            gameplayClientTransportTests.RestoreMatchConnection_FinishedMatch_LoadsFinalState,
            gameplayClientTransportTests.LoadMatchState_CancelledRequest_CanRestoreChannel,
            gameplayClientTransportTests.PlacePiece_ConfirmedVersion_PreservesPlacementAtRemoteBoundary,
            matchClientsTests.Callbacks_StaleVersionsAndDuplicateMessages_PreserveConfirmedState,
            matchClientsTests.Callbacks_ConclusionSnapshot_PreservesWinnerAndAbandonmentReason,
            matchClientsTests.Faults_SupportedCultures_ResolveLocalizedRecoveryAndRejectionMessages,
            clientArchitectureTests.Assemblies_RuntimeReferences_ExcludeServerAndPersistence,
            failureDiagnosticsTests.ForLogging_ThrownFailure_PreservesOnlySafeDiagnostics,
            friendshipSnapshotTests.Refresh_ConcurrentConnection_PreservesLatestPresence,
            friendshipSnapshotTests.Refresh_ConcurrentRemoval_DoesNotRestoreFriend,
            friendshipSnapshotTests.Notifications_LastChangeWins_AndReadsAreIsolated,
            friendshipLocalizationTests.Presence_SupportedCultures_ContainStatusAndRecoveryMessages,
            friendshipConnectionTests.GeneratedProxy_ReceivesPresenceAndPropagatesTypedSessionFault,
            friendshipConnectionTests.Keepalive_CloseWhileRefreshPending_DoesNotReopenChannel,
            sessionModelsTests.SuccessfulResult_PreservesSession,
            sessionModelsTests.FailureResult_DoesNotContainSession,
            sessionModelsTests.SuccessStatusWithoutValue_IsRejected,
            sessionModelsTests.SessionMessages_SupportedCultures_AreAvailable,
            matchInvitationClientTests.RemoteMatchMessages_FriendshipFaultAccountUnavailable_ResolvesExpectedResourceKey,
            matchInvitationClientTests.RemoteMatchMessages_FriendshipFaultFriendshipNotFound_ResolvesExpectedResourceKey,
            matchInvitationClientTests.RemoteMatchMessages_FriendshipFaultInvalidAccountId_ResolvesExpectedResourceKey,
            matchInvitationClientTests.RemoteMatchMessages_FriendshipFaultInactiveAccount_ResolvesExpectedResourceKey,
            matchInvitationClientTests.RemoteMatchMessages_MatchFaultUnauthorized_ResolvesExpectedResourceKey,
            matchInvitationClientTests.RemoteMatchMessages_MatchFaultInactiveAccount_ResolvesExpectedResourceKey,
            matchInvitationClientTests.RemoteMatchMessages_MatchFaultMatchNotWaiting_ResolvesExpectedResourceKey,
            matchInvitationClientTests.RemoteMatchMessages_ServiceUnavailable_ResolvesConnectionFailedKey,
            matchInvitationClientTests.LocalizationResources_InvitationKeys_ArePresentInAllSupportedCultures,
            matchInvitationClientTests.TurnTimePolicy_ValidRange_ParsesConfiguredSeconds,
            matchInvitationClientTests.TurnTimePolicy_OutOfRangeOrNonNumeric_RejectsInput,
            matchInvitationClientTests.MatchLobbyClient_InviteToMatch_CommunicationException_AbortsWithoutAutoRetry,
            connectionFactoryTests.GetServerHost_VariableAbsent_ReturnsLocalhost,
            connectionFactoryTests.GetServerHost_EnvironmentVariableProvided_ReturnsConfiguredHost,
            connectionFactoryTests.CreateEndpointAddress_ValidHost_RewritesAddressPreservingPortAndPath,
            connectionFactoryTests.CreateMatchClients_CustomHost_PreservesConfiguredPortsAndPaths,
            connectionFactoryTests.GetServerHost_WhitespaceOrInvalidHost_ThrowsInvalidOperationException
        };

        return Run(CombineTests(tests));
    }

    private static Action[] CombineTests(Action[] currentTests)
    {
        System.Collections.Generic.List<Action> tests =
            new System.Collections.Generic.List<Action>(currentTests);
        tests.AddRange(LegacyTestSuite.CreateTests());
        return tests.ToArray();
    }

    private static int Run(Action[] tests)
    {
        try
        {
            foreach (Action test in tests)
            {
                test();
            }

            Console.WriteLine($"Passed {tests.Length} client tests.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
