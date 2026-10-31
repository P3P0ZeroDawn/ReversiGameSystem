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
        AuthenticationOutcomeMapperTests mapperTests =
            new AuthenticationOutcomeMapperTests();
        MatchInvitationClientTests invitationTests =
            new MatchInvitationClientTests();
        ServerConnectionFactoryTests connectionFactoryTests =
            new ServerConnectionFactoryTests();
        Action[] tests =
        {
            RemoteMatchClientTests.Callbacks_StaleVersionsAndDuplicateMessages_PreserveConfirmedState,
            RemoteMatchClientTests.Faults_SupportedCultures_ResolveLocalizedRecoveryAndRejectionMessages,
            ClientArchitectureTests.Assemblies_RuntimeReferences_ExcludeServerAndPersistence,
            FailureDiagnosticsTests.ForLogging_ThrownFailure_PreservesOnlySafeDiagnostics,
            FriendshipSnapshotTests.Refresh_ConcurrentConnection_PreservesLatestPresence,
            FriendshipSnapshotTests.Refresh_ConcurrentRemoval_DoesNotRestoreFriend,
            FriendshipSnapshotTests.Notifications_LastChangeWins_AndReadsAreIsolated,
            FriendshipLocalizationTests.Presence_SupportedCultures_ContainStatusAndRecoveryMessages,
            FriendshipConnectionTests.GeneratedProxy_ReceivesPresenceAndPropagatesTypedSessionFault,
            FriendshipConnectionTests.Keepalive_CloseWhileRefreshPending_DoesNotReopenChannel,
            mapperTests.Map_SuccessfulOutcome_PreservesSession,
            mapperTests.Map_ExpiredSession_RejectsResponse,
            mapperTests.Map_MissingToken_RejectsResponse,
            mapperTests.SessionMessages_SupportedCultures_AreAvailable,
            invitationTests.RemoteMatchMessages_FriendshipFaultAccountUnavailable_ResolvesExpectedResourceKey,
            invitationTests.RemoteMatchMessages_FriendshipFaultFriendshipNotFound_ResolvesExpectedResourceKey,
            invitationTests.RemoteMatchMessages_FriendshipFaultInvalidAccountId_ResolvesExpectedResourceKey,
            invitationTests.RemoteMatchMessages_FriendshipFaultInactiveAccount_ResolvesExpectedResourceKey,
            invitationTests.RemoteMatchMessages_MatchFaultUnauthorized_ResolvesExpectedResourceKey,
            invitationTests.RemoteMatchMessages_MatchFaultInactiveAccount_ResolvesExpectedResourceKey,
            invitationTests.RemoteMatchMessages_MatchFaultMatchNotWaiting_ResolvesExpectedResourceKey,
            invitationTests.RemoteMatchMessages_DeliveryFailureOrTimeout_ResolvesConnectionFailedKey,
            invitationTests.LocalizationResources_InvitationKeys_ArePresentInAllSupportedCultures,
            invitationTests.TurnTimePolicy_ValidRange_ParsesConfiguredSeconds,
            invitationTests.TurnTimePolicy_OutOfRangeOrNonNumeric_RejectsInput,
            invitationTests.RemoteMatchClient_InviteAsync_CommunicationException_AbortsWithoutAutoRetry,
            connectionFactoryTests.GetServerHost_VariableAbsent_ReturnsLocalhost,
            connectionFactoryTests.GetServerHost_EnvironmentVariableProvided_ReturnsConfiguredHost,
            connectionFactoryTests.CreateEndpointAddress_ValidHost_RewritesAddressPreservingPortAndPath,
            connectionFactoryTests.CreateMatchEndpoints_CustomHost_OverridesAllThreeEndpoints,
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
