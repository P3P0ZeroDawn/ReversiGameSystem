using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using ReversiGameSystem.Contracts.Accounts;
using ReversiGameSystem.Contracts.Players;
using ReversiGameSystem.Contracts.Services;

namespace ReversiGameSystem.Tests;

internal sealed class ContractSerializationTests
{
    internal void CreateAccountRequest_RoundTrip_PreservesFields()
    {
        CreateAccountRequest request = new CreateAccountRequest
        {
            Username = "player_one",
            EmailAddress = "player@example.com",
            Password = "ValidPassword1!",
            PreferredLocaleCode = "en-US"
        };

        CreateAccountRequest result = RoundTrip(request);

        TestAssert.AreEqual(request.Username, result.Username);
        TestAssert.AreEqual(request.EmailAddress, result.EmailAddress);
        TestAssert.AreEqual(request.Password, result.Password);
        TestAssert.AreEqual(request.PreferredLocaleCode, result.PreferredLocaleCode);
    }

    internal void LoginResponse_RoundTrip_PreservesSession()
    {
        LoginResponse response = new LoginResponse
        {
            ResultCode = LoginResultCode.Success,
            Session = new AuthenticatedSessionDto
            {
                SessionToken = "session-token",
                AccountId = 42,
                Username = "player_one",
                EmailAddress = "player@example.com"
            }
        };

        LoginResponse result = RoundTrip(response);

        TestAssert.AreEqual(LoginResultCode.Success, result.ResultCode);
        AssertNotNull(result.Session);
        TestAssert.AreEqual(response.Session.AccountId, result.Session!.AccountId);
        TestAssert.AreEqual(response.Session.SessionToken, result.Session.SessionToken);
        TestAssert.AreEqual(response.Session.Username, result.Session.Username);
        TestAssert.AreEqual(response.Session.EmailAddress, result.Session.EmailAddress);
    }

    internal void PlayerSummaryResponse_RoundTrip_PreservesStatistics()
    {
        PlayerSummaryResponse response = new PlayerSummaryResponse
        {
            ResultCode = PlayerSummaryResultCode.Success,
            Summary = new PlayerSummaryDto
            {
                AccountId = 42,
                Username = "player_one",
                MatchesPlayed = 10,
                Wins = 6,
                Losses = 3,
                Draws = 1,
                WinRate = 0.6
            }
        };

        PlayerSummaryResponse result = RoundTrip(response);

        TestAssert.AreEqual(PlayerSummaryResultCode.Success, result.ResultCode);
        AssertNotNull(result.Summary);
        TestAssert.AreEqual(response.Summary.AccountId, result.Summary!.AccountId);
        TestAssert.AreEqual(response.Summary.MatchesPlayed, result.Summary.MatchesPlayed);
        TestAssert.AreEqual(response.Summary.Wins, result.Summary.Wins);
        TestAssert.AreEqual(response.Summary.Losses, result.Summary.Losses);
        TestAssert.AreEqual(response.Summary.Draws, result.Summary.Draws);
        TestAssert.AreEqual(response.Summary.WinRate, result.Summary.WinRate);
    }

    internal void ServiceContracts_DeclareExpectedOperations()
    {
        AssertServiceContract(typeof(IAccountService), 2);
        AssertServiceContract(typeof(IPlayerService), 1);
    }

    private static TContract RoundTrip<TContract>(TContract value)
    {
        DataContractSerializer serializer = new DataContractSerializer(typeof(TContract));
        using MemoryStream stream = new MemoryStream();
        serializer.WriteObject(stream, value);
        stream.Position = 0;
        object? result = serializer.ReadObject(stream);
        AssertNotNull(result);
        return (TContract)result!;
    }

    private static void AssertServiceContract(Type contractType, int operationCount)
    {
        bool hasServiceContract = contractType
            .GetCustomAttributes(typeof(ServiceContractAttribute), false)
            .Any();
        int actualOperationCount = contractType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Count(method => method
                .GetCustomAttributes(typeof(OperationContractAttribute), false)
                .Any());

        TestAssert.AreEqual(true, hasServiceContract);
        TestAssert.AreEqual(operationCount, actualOperationCount);
    }

    private static void AssertNotNull(object? value)
    {
        if (value == null)
        {
            throw new InvalidOperationException("Expected a non-null value.");
        }
    }
}
