using System;

namespace ReversiGameSystem.Server.Tests;

internal static class Program
{
    private static int Main()
    {
        Action[] tests = CreateTests();
        return RunTests(tests);
    }

    private static Action[] CreateTests()
    {
        ContractSerializationTests serializationTests = new ContractSerializationTests();
        ContractArchitectureTests architectureTests = new ContractArchitectureTests();
        MatchSerializationTests matchTests = new MatchSerializationTests();
        CommunicationSerializationTests communicationTests = new CommunicationSerializationTests();
        AccountAccessServiceTests accountAccessTests = new AccountAccessServiceTests();
        return new Action[]
        {
            serializationTests.AuthenticationCredentials_RoundTrip_PreservesCredentials,
            serializationTests.AuthenticationOutcome_RoundTrip_PreservesSession,
            serializationTests.AccountRegistrationCredentials_RoundTrip_PreservesProfileImage,
            serializationTests.PlayerProfile_RoundTrip_PreservesStatistics,
            serializationTests.LeaderboardEntry_RoundTrip_PreservesPositions,
            serializationTests.ServiceFault_RoundTrip_PreservesDiagnosticData,
            architectureTests.ServiceContracts_CatalogSignatures_PreserveWireBoundary,
            architectureTests.DataContracts_CatalogMembers_PreserveRequiredShape,
            architectureTests.Enums_CatalogValues_PreserveSerializedVocabulary,
            architectureTests.ServiceFault_LegacyLogin_PreservesRequiredTransientMember,
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
            accountAccessTests.LoginAsync_ValidCredentials_MapsAuthenticatedSession,
            accountAccessTests.LoginAsync_MissingCredentials_ReturnsInvalidCredentials,
            accountAccessTests.LoginAsync_ThroughWcfChannel_ReturnsAuthenticatedSession
        };
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
