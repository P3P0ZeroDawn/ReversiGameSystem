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
        // Arrange
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

        // Act
        CreateAccountRequest result = RoundTrip(request);

        // Assert
        TestAssert.AreEqual(request.Username, result.Username);
        TestAssert.AreEqual(request.EmailAddress, result.EmailAddress);
        TestAssert.AreEqual(request.Password, result.Password);
        TestAssert.AreEqual(request.PreferredLocaleCode, result.PreferredLocaleCode);
        ProfileImageUploadDto expectedImage = TestAssert.IsNotNull(request.ProfileImage);
        ProfileImageUploadDto actualImage = TestAssert.IsNotNull(result.ProfileImage);
        TestAssert.AreEqual(expectedImage.MediaType, actualImage.MediaType);
        TestAssert.AreEqual(expectedImage.Content.Length, actualImage.Content.Length);
    }

    internal void LoginResponse_RoundTrip_PreservesSession()
    {
        // Arrange
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

        // Act
        LoginResponse result = RoundTrip(response);

        // Assert
        TestAssert.AreEqual(LoginResultCode.Success, result.ResultCode);
        AuthenticatedSessionDto expectedSession = TestAssert.IsNotNull(response.Session);
        AuthenticatedSessionDto actualSession = TestAssert.IsNotNull(result.Session);
        TestAssert.AreEqual(expectedSession.AccountId, actualSession.AccountId);
        TestAssert.AreEqual(expectedSession.SessionToken, actualSession.SessionToken);
        TestAssert.AreEqual(expectedSession.Username, actualSession.Username);
        TestAssert.AreEqual(expectedSession.EmailAddress, actualSession.EmailAddress);
    }

    internal void PlayerProfileResponse_RoundTrip_PreservesStatistics()
    {
        // Arrange
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

        // Act
        PlayerProfileResponse result = RoundTrip(response);

        // Assert
        TestAssert.AreEqual(PlayerProfileResultCode.Success, result.ResultCode);
        PlayerProfileDto expectedProfile = TestAssert.IsNotNull(response.Profile);
        PlayerProfileDto actualProfile = TestAssert.IsNotNull(result.Profile);
        TestAssert.AreEqual(expectedProfile.AccountId, actualProfile.AccountId);
        TestAssert.AreEqual(expectedProfile.ProfileImageUri, actualProfile.ProfileImageUri);
        TestAssert.AreEqual(
            expectedProfile.Statistics.MatchesPlayed,
            actualProfile.Statistics.MatchesPlayed);
        TestAssert.AreEqual(expectedProfile.Statistics.Wins, actualProfile.Statistics.Wins);
        TestAssert.AreEqual(expectedProfile.Statistics.Losses, actualProfile.Statistics.Losses);
        TestAssert.AreEqual(expectedProfile.Statistics.Draws, actualProfile.Statistics.Draws);
        TestAssert.AreEqual(expectedProfile.Statistics.WinRate, actualProfile.Statistics.WinRate);
    }

    internal void LeaderboardResponse_RoundTrip_PreservesEntries()
    {
        // Arrange
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

        // Act
        LeaderboardResponse result = RoundTrip(response);

        // Assert
        TestAssert.AreEqual(LeaderboardResultCode.Success, result.ResultCode);
        TestAssert.AreEqual(1, result.Entries.Length);
        TestAssert.AreEqual(response.Entries[0].Rank, result.Entries[0].Rank);
        TestAssert.AreEqual(response.Entries[0].Username, result.Entries[0].Username);
        TestAssert.AreEqual(response.Entries[0].Wins, result.Entries[0].Wins);
    }

    internal void AccountServiceContract_DeclaredOperations_MatchesExpectedCount()
    {
        // Arrange
        Type contractType = typeof(IAccountService);

        // Act
        bool isValid = HasExpectedOperations(contractType, 2);

        // Assert
        TestAssert.AreEqual(true, isValid);
    }

    internal void PlayerServiceContract_DeclaredOperations_MatchesExpectedCount()
    {
        // Arrange
        Type contractType = typeof(IPlayerService);

        // Act
        bool isValid = HasExpectedOperations(contractType, 2);

        // Assert
        TestAssert.AreEqual(true, isValid);
    }

    private static TContract RoundTrip<TContract>(TContract value)
    {
        DataContractSerializer serializer = new DataContractSerializer(typeof(TContract));
        using MemoryStream stream = new MemoryStream();
        serializer.WriteObject(stream, value);
        stream.Position = 0;
        object? result = serializer.ReadObject(stream);
        object nonNullResult = TestAssert.IsNotNull(result);
        return (TContract)nonNullResult;
    }

    private static bool HasExpectedOperations(Type contractType, int operationCount)
    {
        bool hasServiceContract = contractType
            .GetCustomAttributes(typeof(ServiceContractAttribute), false)
            .Any();
        int actualOperationCount = contractType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Count(method => method
                .GetCustomAttributes(typeof(OperationContractAttribute), false)
                .Any());

        return hasServiceContract && actualOperationCount == operationCount;
    }

}
