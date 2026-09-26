using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using ReversiGameSystem.Contracts.Accounts;
using ReversiGameSystem.Contracts.Leaderboards;
using ReversiGameSystem.Contracts.Media;
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
            PreferredLocaleCode = "en-US",
            ProfileImage = new ProfileImageUploadDto
            {
                Content = new byte[] { 1, 2, 3 },
                MediaType = "image/png"
            }
        };

        CreateAccountRequest result = RoundTrip(request);

        TestAssert.AreEqual(request.Username, result.Username);
        TestAssert.AreEqual(request.EmailAddress, result.EmailAddress);
        TestAssert.AreEqual(request.Password, result.Password);
        TestAssert.AreEqual(request.PreferredLocaleCode, result.PreferredLocaleCode);
        AssertNotNull(result.ProfileImage);
        TestAssert.AreEqual(request.ProfileImage.MediaType, result.ProfileImage!.MediaType);
        TestAssert.AreEqual(request.ProfileImage.Content.Length, result.ProfileImage.Content.Length);
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

    internal void PlayerProfileResponse_RoundTrip_PreservesStatistics()
    {
        PlayerProfileResponse response = new PlayerProfileResponse
        {
            ResultCode = PlayerProfileResultCode.Success,
            Profile = new PlayerProfileDto
            {
                AccountId = 42,
                Username = "player_one",
                ProfileImageUri = "https://example.com/profiles/42.png",
                Statistics = new PlayerStatisticsDto
                {
                    MatchesPlayed = 10,
                    Wins = 6,
                    Losses = 3,
                    Draws = 1,
                    WinRate = 0.6
                }
            }
        };

        PlayerProfileResponse result = RoundTrip(response);

        TestAssert.AreEqual(PlayerProfileResultCode.Success, result.ResultCode);
        AssertNotNull(result.Profile);
        TestAssert.AreEqual(response.Profile.AccountId, result.Profile!.AccountId);
        TestAssert.AreEqual(response.Profile.ProfileImageUri, result.Profile.ProfileImageUri);
        TestAssert.AreEqual(
            response.Profile.Statistics.MatchesPlayed,
            result.Profile.Statistics.MatchesPlayed);
        TestAssert.AreEqual(response.Profile.Statistics.Wins, result.Profile.Statistics.Wins);
        TestAssert.AreEqual(response.Profile.Statistics.Losses, result.Profile.Statistics.Losses);
        TestAssert.AreEqual(response.Profile.Statistics.Draws, result.Profile.Statistics.Draws);
        TestAssert.AreEqual(response.Profile.Statistics.WinRate, result.Profile.Statistics.WinRate);
    }

    internal void LeaderboardResponse_RoundTrip_PreservesEntries()
    {
        LeaderboardResponse response = new LeaderboardResponse
        {
            ResultCode = LeaderboardResultCode.Success,
            Entries = new[]
            {
                new LeaderboardEntryDto
                {
                    Rank = 1,
                    AccountId = 42,
                    Username = "player_one",
                    MatchesPlayed = 10,
                    Wins = 6,
                    WinRate = 0.6
                }
            }
        };

        LeaderboardResponse result = RoundTrip(response);

        TestAssert.AreEqual(LeaderboardResultCode.Success, result.ResultCode);
        TestAssert.AreEqual(1, result.Entries.Length);
        TestAssert.AreEqual(response.Entries[0].Rank, result.Entries[0].Rank);
        TestAssert.AreEqual(response.Entries[0].Username, result.Entries[0].Username);
        TestAssert.AreEqual(response.Entries[0].Wins, result.Entries[0].Wins);
    }

    internal void ServiceContracts_DeclareExpectedOperations()
    {
        AssertServiceContract(typeof(IAccountService), 2);
        AssertServiceContract(typeof(IPlayerService), 2);
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
