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
        return new Action[]
        {
            serializationTests.AuthenticationCredentials_RoundTrip_PreservesCredentials,
            serializationTests.AuthenticationOutcome_RoundTrip_PreservesSession,
            serializationTests.AccountRegistrationCredentials_RoundTrip_PreservesProfileImage,
            serializationTests.PlayerProfileLookupOutcome_RoundTrip_PreservesStatistics,
            serializationTests.LeaderboardStandings_RoundTrip_PreservesPositions,
            serializationTests.ServiceFault_RoundTrip_PreservesDiagnosticData,
            architectureTests.ServiceContracts_DeclaredOperations_UseControlledFaults,
            architectureTests.ContractTypes_PublicNames_AvoidProhibitedNames,
            architectureTests.ContractAssembly_ReferencedAssemblies_ExcludePersistence
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
