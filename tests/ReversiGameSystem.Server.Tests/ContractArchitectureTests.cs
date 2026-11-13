using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.Tests;

/// <summary>
/// Checks the WCF boundary against the approved catalog and migration exception.
/// </summary>
[TestClass]
public sealed class ContractArchitectureTests
{
    private const string ServicesNamespace = "urn:reversi:services:v1";
    private const string DataNamespace = "urn:reversi:contracts:v1";
    private const string DiagnosticsNamespace = "urn:reversi:diagnostics:v1";

    [TestMethod]
    public void ServiceContracts_CatalogSignatures_PreserveWireBoundary()
    {
        // Arrange
        Dictionary<string, string[]> expected = ContractCatalogExpectations.Operations;

        // Act
        Dictionary<string, Type> contracts = expected.Keys.ToDictionary(name => name, FindType);

        // Assert
        foreach (KeyValuePair<string, Type> contract in contracts)
        {
            AssertService(contract.Value);
            string[] actual = contract.Value.GetMethods().Select(FormatMethod).ToArray();
            AssertSetEqual(expected[contract.Key].Select(NormalizeSignature), actual);
            foreach (MethodInfo method in contract.Value.GetMethods())
            {
                AssertOperation(method, contract.Key);
            }
        }
    }

    [TestMethod]
    public void DataContracts_CatalogMembers_PreserveRequiredShape()
    {
        // Arrange
        Dictionary<string, string[]> expected = ContractCatalogExpectations.Members;

        // Act
        Dictionary<string, Type> contracts = expected.Keys.ToDictionary(name => name, FindType);

        // Assert
        foreach (KeyValuePair<string, Type> contract in contracts)
        {
            AssertDataContract(contract.Value);
            AssertMembers(contract.Value, expected[contract.Key]);
        }
    }

    [TestMethod]
    public void Enums_CatalogValues_PreserveSerializedVocabulary()
    {
        // Arrange
        Dictionary<string, string[]> expected = ContractCatalogExpectations.EnumValues;

        // Act
        Dictionary<string, Type> contracts = expected.Keys.ToDictionary(name => name, FindType);

        // Assert
        foreach (KeyValuePair<string, Type> contract in contracts)
        {
            Type type = contract.Value;
            AssertDataContract(type);
            AssertSetEqual(expected[contract.Key], Enum.GetNames(type));
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                EnumMemberAttribute member = GetAttribute<EnumMemberAttribute>(field);
                TestAssert.IsTrue(string.IsNullOrEmpty(member.Value) || member.Value == field.Name,
                    $"Unexpected wire enum value for {type.Name}.{field.Name}.");
            }
        }
    }

    [TestMethod]
    public void ServiceFault_RetainedRegistration_PreservesRequiredTransientMember()
    {
        // Arrange
        Type type = typeof(ServiceFault);

        // Act
        PropertyInfo member = TestAssert.IsNotNull(type.GetProperty("IsTransient"));
        DataMemberAttribute attribute = GetAttribute<DataMemberAttribute>(member);

        // Assert
        TestAssert.AreEqual(typeof(bool), member.PropertyType);
        TestAssert.AreEqual(3, attribute.Order);
        TestAssert.AreEqual(true, attribute.IsRequired);
        TestAssert.AreEqual(DiagnosticsNamespace,
            GetAttribute<DataContractAttribute>(type).Namespace);
    }

    [TestMethod]
    public void ContractAssembly_ReferencedAssemblies_ExcludePersistence()
    {
        // Arrange
        AssemblyName[] references = typeof(AuthenticatedSession)
            .Assembly.GetReferencedAssemblies();

        // Act
        bool hasInfrastructure = references.Any(reference =>
            reference.Name == "ReversiGameSystem.Server.Persistence"
            || reference.Name == "ReversiGameSystem.Server.Domain"
            || reference.Name == "EntityFramework"
            || reference.Name == "PresentationFramework");

        // Assert
        TestAssert.IsTrue(!hasInfrastructure,
            "The contract assembly must not reference domain, persistence, or WPF types.");
    }

    private static void AssertService(Type type)
    {
        ServiceContractAttribute attribute = GetAttribute<ServiceContractAttribute>(type);
        TestAssert.AreEqual(ServicesNamespace, attribute.Namespace);
        TestAssert.IsTrue(string.IsNullOrEmpty(attribute.Name) || attribute.Name == type.Name,
            $"Unexpected service name for {type.Name}.");
        TestAssert.IsTrue(type.IsInterface, $"{type.Name} must be an interface.");
        bool isCallback = type.Name.EndsWith("Callback", StringComparison.Ordinal);
        string callbackName = type.Name + "Callback";
        bool hasCallback = !isCallback
            && ContractCatalogExpectations.Operations.ContainsKey(callbackName);
        Type? callback = hasCallback ? FindType(callbackName) : null;
        TestAssert.AreEqual(callback, attribute.CallbackContract);
        string category = isCallback ? "Callbacks" : "Services";
        TestAssert.AreEqual("ReversiGameSystem.Server.Contracts." + category, type.Namespace);
    }

    private static void AssertOperation(MethodInfo method, string contractName)
    {
        OperationContractAttribute operation = GetAttribute<OperationContractAttribute>(method);
        TestAssert.IsTrue(string.IsNullOrEmpty(operation.Name) || operation.Name == method.Name,
            $"Unexpected operation name for {method.Name}.");
        bool isCallback = contractName.EndsWith("Callback", StringComparison.Ordinal);
        TestAssert.AreEqual(isCallback, operation.IsOneWay);
        FaultContractAttribute[] faults = method.GetCustomAttributes<FaultContractAttribute>()
            .ToArray();
        if (isCallback)
        {
            TestAssert.AreEqual(typeof(void), method.ReturnType);
            TestAssert.AreEqual(0, faults.Length);
            return;
        }

        AssertSetEqual(GetExpectedFaults(contractName, method.Name),
            faults.Select(fault => fault.DetailType.Name));
        foreach (FaultContractAttribute fault in faults)
        {
            string expectedNamespace = fault.DetailType == typeof(ServiceFault)
                ? DiagnosticsNamespace : DataNamespace;
            TestAssert.AreEqual(expectedNamespace, fault.Namespace);
        }
    }

    private static string[] GetExpectedFaults(string contractName, string methodName)
    {
        switch (contractName)
        {
            case "ISessionService":
                return new[] { "SessionFault", "ServiceFault" };
            case "IAccountService":
                return methodName == "ChangePassword" || methodName == "DeactivateAccount"
                    ? new[] { "AccountFault", "SessionFault", "ServiceFault" }
                    : new[] { "AccountFault", "ServiceFault" };
            case "IPlayerProfileService":
                return new[] { "ProfileFault", "SessionFault", "ServiceFault" };
            case "IPlayerPreferencesService":
                return new[] { "PreferencesFault", "SessionFault", "ServiceFault" };
            case "ILeaderboardService":
                return new[] { "LeaderboardFault", "ServiceFault" };
            case "IFriendshipService":
                return new[] { "FriendshipFault", "SessionFault", "ServiceFault" };
            case "IMatchLobbyService" when methodName == "InviteToMatch":
                return new[] { "MatchFault", "FriendshipFault", "SessionFault", "ServiceFault" };
            case "IMatchChatService" when methodName == "SendChatMessage":
                return new[] { "ChatFault", "MatchFault", "SessionFault", "ServiceFault" };
            default:
                return new[] { "MatchFault", "SessionFault", "ServiceFault" };
        }
    }

    private static void AssertDataContract(Type type)
    {
        DataContractAttribute attribute = GetAttribute<DataContractAttribute>(type);
        string expectedNamespace = type == typeof(ServiceFault)
            ? DiagnosticsNamespace : DataNamespace;
        TestAssert.AreEqual(expectedNamespace, attribute.Namespace);
        TestAssert.IsTrue(string.IsNullOrEmpty(attribute.Name) || attribute.Name == type.Name,
            $"Unexpected wire name for {type.Name}.");
    }

    private static void AssertMembers(Type type, string[] expected)
    {
        PropertyInfo[] properties = type.GetProperties();
        string[] actual = properties.Select(property =>
            FormatType(property.PropertyType) + " " + property.Name).ToArray();
        AssertSetEqual(expected.Select(NormalizeSignature), actual);
        for (int index = 0; index < expected.Length; index++)
        {
            string[] parts = expected[index].Split(' ');
            PropertyInfo property = TestAssert.IsNotNull(type.GetProperty(parts[1]));
            DataMemberAttribute member = GetAttribute<DataMemberAttribute>(property);
            TestAssert.AreEqual(index + 1, member.Order);
            TestAssert.AreEqual(!parts[0].EndsWith("?", StringComparison.Ordinal),
                member.IsRequired);
            TestAssert.IsTrue(string.IsNullOrEmpty(member.Name) || member.Name == property.Name,
                $"Unexpected wire member name for {type.Name}.{property.Name}.");
        }
    }

    private static string FormatMethod(MethodInfo method)
    {
        string parameters = string.Join(", ", method.GetParameters().Select(parameter =>
            FormatType(parameter.ParameterType) + " " + parameter.Name));
        return FormatType(method.ReturnType) + " " + method.Name + "(" + parameters + ")";
    }

    private static string FormatType(Type type)
    {
        if (type.IsArray)
        {
            return FormatType(TestAssert.IsNotNull(type.GetElementType())) + "[]";
        }

        Type? underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return FormatType(underlyingType) + "?";
        }

        return type == typeof(void) ? "void" : type == typeof(string) ? "string"
            : type == typeof(long) ? "long" : type == typeof(int) ? "int"
            : type == typeof(bool) ? "bool" : type == typeof(double) ? "double"
            : type == typeof(byte) ? "byte" : type.Name;
    }

    private static string NormalizeSignature(string value)
    {
        return value.Replace("string?", "string").Replace("ImageContent?", "ImageContent")
            .Replace("MoveRecord?", "MoveRecord").Replace("MatchConclusion?", "MatchConclusion")
            .Replace("( ", "(");
    }

    private static Type FindType(string name)
    {
        Type? type = typeof(ServiceFault).Assembly.GetExportedTypes()
            .SingleOrDefault(candidate => candidate.Name == name);
        TestAssert.IsTrue(type != null, $"Catalog contract {name} is missing.");
        return TestAssert.IsNotNull(type);
    }

    private static TAttribute GetAttribute<TAttribute>(MemberInfo member)
        where TAttribute : Attribute
    {
        return TestAssert.IsNotNull(member.GetCustomAttribute<TAttribute>());
    }

    private static void AssertSetEqual(IEnumerable<string> expected, IEnumerable<string> actual)
    {
        string expectedText = string.Join("; ", expected.OrderBy(value => value));
        string actualText = string.Join("; ", actual.OrderBy(value => value));
        TestAssert.AreEqual(expectedText, actualText);
    }
}
