using System;
using System.Reflection;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal static class ClientArchitectureTests
{
    internal static void Assemblies_RuntimeReferences_ExcludeServerAndPersistence()
    {
        Assembly client = typeof(EmailPolicy).Assembly;
        TestAssert.AreEqual("ReversiGameSystem.Client.Presentation", client.GetName().Name);
        AssertReferences(client);
        AssertReferences(typeof(ClientArchitectureTests).Assembly);
    }

    private static void AssertReferences(Assembly assembly)
    {
        foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
        {
            string name = reference.Name ?? string.Empty;
            if (name.StartsWith("ReversiGameSystem.Server", StringComparison.Ordinal)
                || name == "ReversiGameSystem.Domain"
                || name == "ReversiGameSystem.Contracts"
                || name == "ReversiGameSystem.LocalRuntime"
                || name.StartsWith("EntityFramework", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Forbidden client dependency: {assembly.GetName().Name} -> {name}.");
            }
        }
    }
}
