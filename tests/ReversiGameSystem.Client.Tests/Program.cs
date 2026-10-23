using System;

namespace ReversiGameSystem.Client.Tests;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--session-integration")
        {
            return Run(new Action[]
            {
                SessionConnectionTests.GuestLifecycle_ThroughGeneratedProxy
            });
        }
        if (args.Length != 0)
        {
            Console.Error.WriteLine("Usage: Client.Tests.exe [--session-integration]");
            return 2;
        }
        AuthenticationOutcomeMapperTests mapperTests =
            new AuthenticationOutcomeMapperTests();
        Action[] tests =
        {
            ClientArchitectureTests.Assemblies_RuntimeReferences_ExcludeServerAndPersistence,
            mapperTests.Map_SuccessfulOutcome_PreservesSession,
            mapperTests.Map_ExpiredSession_RejectsResponse,
            mapperTests.Map_MissingToken_RejectsResponse,
            mapperTests.SessionMessages_SupportedCultures_AreAvailable
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
