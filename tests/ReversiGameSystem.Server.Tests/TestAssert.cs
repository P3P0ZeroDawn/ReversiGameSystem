using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Server.Tests;

internal static class TestAssert
{
    internal static void AreEqual<TValue>(TValue expected, TValue actual)
    {
        if (!EqualityComparer<TValue>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"Expected {expected}, but received {actual}.");
        }
    }

    internal static TReference IsNotNull<TReference>(TReference? value)
        where TReference : class
    {
        if (value == null)
        {
            throw new InvalidOperationException("Expected a non-null value.");
        }

        return value;
    }

    internal static void IsTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
