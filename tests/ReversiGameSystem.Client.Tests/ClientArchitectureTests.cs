using System;
using System.Reflection;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class ClientArchitectureTests
{
    [TestMethod]
    public void Assemblies_RuntimeReferences_ExcludeServerAndPersistence()
    {
        Assembly presentation = typeof(MainWindow).Assembly;
        Assembly communication = typeof(ServerConnectionFactory).Assembly;
        Assembly validation = typeof(EmailPolicy).Assembly;

        Assert.AreEqual(
            "ReversiGameSystem.Client.Presentation",
            presentation.GetName().Name);
        Assert.AreEqual(
            "ReversiGameSystem.Client.Communication",
            communication.GetName().Name);
        Assert.AreEqual(
            "ReversiGameSystem.Client.Validation",
            validation.GetName().Name);

        AssertReferences(presentation);
        AssertReferences(communication);
        AssertReferences(validation);
        AssertReferences(typeof(ClientArchitectureTests).Assembly);

        AssertDoesNotReference(
            communication,
            "ReversiGameSystem.Client.Presentation",
            "ReversiGameSystem.Client.Validation");
        AssertDoesNotReference(
            validation,
            "ReversiGameSystem.Client.Presentation",
            "ReversiGameSystem.Client.Communication");
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

    private static void AssertDoesNotReference(Assembly assembly, params string[] forbiddenNames)
    {
        foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
        {
            string name = reference.Name ?? string.Empty;
            foreach (string forbiddenName in forbiddenNames)
            {
                if (name == forbiddenName)
                {
                    throw new InvalidOperationException(
                        $"Forbidden client dependency: {assembly.GetName().Name} -> {name}.");
                }
            }
        }
    }
}
