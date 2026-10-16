using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Client.Tests;

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
}
