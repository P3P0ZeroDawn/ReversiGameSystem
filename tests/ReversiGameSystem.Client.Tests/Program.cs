using System;

namespace ReversiGameSystem.Client.Tests;

internal static class Program
{
    private static int Main()
    {
        AuthenticationOutcomeMapperTests mapperTests =
            new AuthenticationOutcomeMapperTests();
        Action[] tests =
        {
            mapperTests.Map_SuccessfulOutcome_PreservesSession,
            mapperTests.Map_PersistenceUnavailable_ReturnsServiceUnavailable
        };

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
