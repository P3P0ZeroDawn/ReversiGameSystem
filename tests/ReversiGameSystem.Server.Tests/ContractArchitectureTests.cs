using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.Tests;

internal sealed class ContractArchitectureTests
{
    private static readonly Type[] ServiceContractTypes =
    {
        typeof(IAccountAccessService),
        typeof(IPlayerProfileService),
        typeof(ILeaderboardService)
    };

    private static readonly HashSet<string> PersistenceEntityNames =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Account",
            "AccountSettings",
            "ChatMessage",
            "ChatMute",
            "ChatViolation",
            "Friendship",
            "GameMatch",
            "GameMove",
            "MatchParticipant",
            "ModerationPattern",
            "ModerationRule",
            "SupportedLocale",
            "VerificationCode"
        };

    internal void ServiceContracts_DeclaredOperations_UseControlledFaults()
    {
        // Arrange
        Type expectedFaultType = typeof(ServiceFault);

        // Act and assert
        foreach (Type contractType in ServiceContractTypes)
        {
            ServiceContractAttribute serviceContract = GetServiceContract(contractType);
            TestAssert.AreEqual<Type?>(null, serviceContract.CallbackContract);

            foreach (MethodInfo method in contractType.GetMethods())
            {
                AssertOperationContract(method, expectedFaultType);
            }
        }
    }

    internal void ContractTypes_PublicNames_AvoidProhibitedNames()
    {
        // Arrange
        Type[] contractTypes = typeof(AuthenticationCredentials)
            .Assembly
            .GetExportedTypes();

        // Act and assert
        foreach (Type contractType in contractTypes)
        {
            bool hasProhibitedSuffix = contractType.Name.EndsWith(
                "Request",
                StringComparison.Ordinal)
                || contractType.Name.EndsWith("Response", StringComparison.Ordinal)
                || contractType.Name.EndsWith("Dto", StringComparison.Ordinal);

            TestAssert.IsTrue(
                !hasProhibitedSuffix,
                $"Contract type {contractType.Name} uses a prohibited suffix.");
            TestAssert.IsTrue(
                !PersistenceEntityNames.Contains(contractType.Name),
                $"Contract type {contractType.Name} collides with a persistence entity.");
        }
    }

    internal void ContractAssembly_ReferencedAssemblies_ExcludePersistence()
    {
        // Arrange
        AssemblyName[] references = typeof(AuthenticationCredentials)
            .Assembly
            .GetReferencedAssemblies();

        // Act
        bool referencesPersistence = references.Any(reference => string.Equals(
            reference.Name,
            "ReversiGameSystem.Server.Persistence",
            StringComparison.Ordinal)
            || string.Equals(reference.Name, "EntityFramework", StringComparison.Ordinal));

        // Assert
        TestAssert.IsTrue(
            !referencesPersistence,
            "The WCF contract assembly must not reference persistence infrastructure.");
    }

    private static ServiceContractAttribute GetServiceContract(Type contractType)
    {
        object? attribute = contractType
            .GetCustomAttributes(typeof(ServiceContractAttribute), false)
            .SingleOrDefault();
        return (ServiceContractAttribute)TestAssert.IsNotNull(attribute);
    }

    private static void AssertOperationContract(MethodInfo method, Type expectedFaultType)
    {
        bool hasOperationContract = method
            .GetCustomAttributes(typeof(OperationContractAttribute), false)
            .Any();
        FaultContractAttribute? faultContract = method
            .GetCustomAttributes(typeof(FaultContractAttribute), false)
            .Cast<FaultContractAttribute>()
            .SingleOrDefault();

        TestAssert.IsTrue(
            hasOperationContract,
            $"Operation {method.Name} is missing OperationContractAttribute.");
        FaultContractAttribute actualFault = TestAssert.IsNotNull(faultContract);
        TestAssert.AreEqual(expectedFaultType, actualFault.DetailType);
    }
}
