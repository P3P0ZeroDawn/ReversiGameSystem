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
                MatchScreenTests.ExistingGamePage_RealMatch_RendersPlayersBoardAndChat
            });
        }
        if (args.Length == 1 && args[0] == "--match-end-to-end")
        {
            return Run(new Action[] { MatchEndToEndTests.TwoPlayers_RealServer_StartsConfirmedBoardAndExchangesChat });
        }
        if (args.Length == 1 && args[0] == "--friendship-end-to-end")
        {
            return Run(new Action[]
            {
                FriendshipEndToEndTests.Presence_RealAccounts_ReceivesLoginAndSessionClosureCallbacks
            });
        }
        if (args.Length == 1 && args[0] == "--session-integration")
        {
            return Run(new Action[]
            {
                SessionConnectionTests.GuestLifecycle_ThroughGeneratedProxy
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
        Action[] tests =
        {
            RemoteCallTests.SendServiceCall_Timeout_PreservesCauseAndAddsCorrelation,
            RemoteCallTests.SendServiceCall_ConnectionFailure_PreservesCauseAndAddsCorrelation,
            RemoteCallTests.SendServiceCall_ProgrammingFailure_PropagatesOriginalException,
            RemoteMatchMessagesTests.MatchStatuses_SupportedCultures_HaveLocalizedMessages,
            RemoteMatchMessagesTests.ChatStatuses_SupportedCultures_HaveLocalizedMessages,
            RemoteMatchMessagesTests.MatchNotFound_ResolvesSpecificMessage,
            RemoteMatchMessagesTests.UnknownMatchStatus_ThrowsInsteadOfInventingRejection,
            RemoteMatchMessagesTests.UnknownChatStatus_ThrowsInsteadOfInventingRejection,
            accountCreationTests.CreateAccount_MissingInformation_DoesNotCallServer,
            accountCreationTests.CreateAccount_ValidInput_NormalizesAndCallsServer,
            accountCreationTests.CreateAccount_ConcurrentSubmission_ReturnsSubmissionInProgress,
            LobbySettingsTests.ChangeTurnDuration_UnreachableServer_ReturnsServiceUnavailable,
            LobbySettingsTests.RemoteMatchMessages_InvalidTurnDuration_ResolvesExpectedResourceKey,
            LobbySettingsTests.LocalizationResources_MatchSettingsKeys_ExistInSupportedCultures,
            ClientErrorHandlerTests.Callback_ConnectionFailure_RecordsCauseWithoutFatalNotification,
            ClientErrorHandlerTests.Callback_ProgrammingFailure_ReachesFatalBoundary,
            ClientErrorHandlerTests.ReportFatalFailure_ProgrammingError_DisplaysFatalMessage,
            ClientErrorHandlerTests.ReportFatalFailure_UnhandledDeclaredFault_DisplaysFatalMessage,
            ClientErrorHandlerTests.IsConnectionFailure_UnknownFault_DoesNotTreatAsDisconnection,
            ClientErrorHandlerTests.IsConnectionFailure_ProgrammingError_DoesNotTreatAsDisconnection,
            ClientErrorHandlerTests.CanMapFailure_UnknownMatchCode_DoesNotInventStatus,
            ClientErrorHandlerTests.FileLogger_AggregateFailure_PreservesCausesWithoutSecrets,
            ClientErrorHandlerTests.RecordFailure_PrimaryLoggerFails_PersistsBothCausesAndCorrelation,
            ClientErrorHandlerTests.FileLogger_PrimaryFileUnavailable_PersistsOriginalEventInBackup,
            ClientErrorHandlerTests.RecordFailure_AllMediaUnavailable_ReportsUnavailableDiagnostics,
            PlayerPreferencesClientTests.ChangeLanguage_ExistingPreferences_PreservesOtherSettings,
            PlayerPreferencesClientTests.ChangeLanguage_InvalidLocale_ReturnsDeclaredStatus,
            PlayerPreferencesClientTests.ChangeLanguage_RejectedWrite_DoesNotRetry,
            PlayerPreferencesClientTests.ChangeLanguage_UnknownFault_PropagatesFailure,
            PlayerPreferencesClientTests.ChangeLanguage_ServiceFault_PreservesServerCorrelation,
            PlayerPreferencesClientTests.ChangeLanguage_InvalidClientConfiguration_PropagatesFailure,
            PlayerPreferencesClientTests.CreatePreferencesClient_CustomHost_UsesPreferencesEndpoint,
            GameViewModelRecoveryTests.ConnectionLost_UnexpectedRecoveryFault_ReportsBeforeViewDeactivation,
            GameViewModelTests.UpdateClock_ActiveMute_MarksPlayerMuted,
            GameViewModelTests.UpdateClock_ExpiredMute_ClearsMute,
            GameViewModelTests.SendChatMessage_MutedPlayer_ReturnsPlayerMuted,
            GameplayClientTransportTests.Callback_ProcessingFailure_ReachesErrorBoundary,
            GameplayClientTransportTests.RestoreMatchConnection_FinishedMatch_LoadsFinalState,
            GameplayClientTransportTests.LoadMatchState_CancelledRequest_CanRestoreChannel,
            GameplayClientTransportTests.PlacePiece_ConfirmedVersion_PreservesPlacementAtRemoteBoundary,
            MatchClientsTests.Callbacks_StaleVersionsAndDuplicateMessages_PreserveConfirmedState,
            MatchClientsTests.Callbacks_ConclusionSnapshot_PreservesWinnerAndAbandonmentReason,
            MatchClientsTests.Faults_SupportedCultures_ResolveLocalizedRecoveryAndRejectionMessages,
            ClientArchitectureTests.Assemblies_RuntimeReferences_ExcludeServerAndPersistence,
            FailureDiagnosticsTests.ForLogging_ThrownFailure_PreservesOnlySafeDiagnostics,
            FriendshipSnapshotTests.Refresh_ConcurrentConnection_PreservesLatestPresence,
            FriendshipSnapshotTests.Refresh_ConcurrentRemoval_DoesNotRestoreFriend,
            FriendshipSnapshotTests.Notifications_LastChangeWins_AndReadsAreIsolated,
            FriendshipLocalizationTests.Presence_SupportedCultures_ContainStatusAndRecoveryMessages,
            FriendshipConnectionTests.GeneratedProxy_ReceivesPresenceAndPropagatesTypedSessionFault,
            FriendshipConnectionTests.Keepalive_CloseWhileRefreshPending_DoesNotReopenChannel,
            sessionModelsTests.SuccessfulResult_PreservesSession,
            sessionModelsTests.FailureResult_DoesNotContainSession,
            sessionModelsTests.SuccessStatusWithoutValue_IsRejected,
            sessionModelsTests.SessionMessages_SupportedCultures_AreAvailable,
            MatchInvitationClientTests.RemoteMatchMessages_FriendshipFaultAccountUnavailable_ResolvesExpectedResourceKey,
            MatchInvitationClientTests.RemoteMatchMessages_FriendshipFaultFriendshipNotFound_ResolvesExpectedResourceKey,
            MatchInvitationClientTests.RemoteMatchMessages_FriendshipFaultInvalidAccountId_ResolvesExpectedResourceKey,
            MatchInvitationClientTests.RemoteMatchMessages_FriendshipFaultInactiveAccount_ResolvesExpectedResourceKey,
            MatchInvitationClientTests.RemoteMatchMessages_MatchFaultUnauthorized_ResolvesExpectedResourceKey,
            MatchInvitationClientTests.RemoteMatchMessages_MatchFaultInactiveAccount_ResolvesExpectedResourceKey,
            MatchInvitationClientTests.RemoteMatchMessages_MatchFaultMatchNotWaiting_ResolvesExpectedResourceKey,
            MatchInvitationClientTests.RemoteMatchMessages_ServiceUnavailable_ResolvesConnectionFailedKey,
            MatchInvitationClientTests.LocalizationResources_InvitationKeys_ArePresentInAllSupportedCultures,
            MatchInvitationClientTests.TurnTimePolicy_ValidRange_ParsesConfiguredSeconds,
            MatchInvitationClientTests.TurnTimePolicy_OutOfRangeOrNonNumeric_RejectsInput,
            MatchInvitationClientTests.MatchLobbyClient_InviteToMatch_CommunicationException_AbortsWithoutAutoRetry,
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
