using System;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Leaderboards;
using ReversiGameSystem.Server.Contracts.Players;

namespace ReversiGameSystem.Server.Tests.UnitTests.Contracts;

/// <summary>
/// Verifies retained account access contracts and the revised profile and leaderboard data.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class ContractSerializationTests
{
    private const long SampleAccountId = 42L;
    private const string SampleUsername = "player_one";
    private const string SampleSessionToken = "session-token";
    private const string EnglishLocaleCode = "en-US";
    private const string PngMediaType = "image/png";

    [TestMethod]
    public void AuthenticatedSession_RoundTrip_PreservesAccountAndExpiry()
    {
        AuthenticatedSession original = CreateAuthenticatedSession();

        AuthenticatedSession session = RoundTrip(original);

        Assert.AreEqual(SampleAccountId, session.Account.AccountId);
        Assert.AreEqual(SampleUsername, session.Account.Username);
        Assert.AreEqual(SampleSessionToken, session.SessionToken);
        Assert.AreEqual(EnglishLocaleCode, session.PreferredLocaleCode);
        Assert.AreEqual(original.ExpiresAtUtc, session.ExpiresAtUtc);
    }

    [TestMethod]
    public void AccountRegistrationCredentials_RoundTrip_PreservesProfileImage()
    {
        AccountRegistrationCredentials credentials = CreateRegistrationCredentials();
        int expectedContentLength = 3;

        AccountRegistrationCredentials result = RoundTrip(credentials);

        ProfileImageContent image = TestAssert.IsNotNull(result.ProfileImage);
        Assert.AreEqual(credentials.Username, result.Username);
        Assert.AreEqual(credentials.EmailAddress, result.EmailAddress);
        Assert.AreEqual(credentials.PreferredLocaleCode, result.PreferredLocaleCode);
        Assert.AreEqual(PngMediaType, image.MediaType);
        Assert.AreEqual(expectedContentLength, image.Content.Length);
    }

    [TestMethod]
    public void PlayerProfile_RoundTrip_PreservesStatistics()
    {
        PlayerProfile profile = CreatePlayerProfile();
        int expectedMatchesPlayed = 10;
        int expectedWins = 6;
        double expectedWinRate = 0.6;

        PlayerProfile result = RoundTrip(profile);

        Assert.AreEqual(SampleAccountId, result.Account.AccountId);
        Assert.IsTrue(result.HasProfileImage);
        Assert.AreEqual(expectedMatchesPlayed, result.Statistics.MatchesPlayed);
        Assert.AreEqual(expectedWins, result.Statistics.Wins);
        Assert.AreEqual(expectedWinRate, result.Statistics.WinRate);
    }

    [TestMethod]
    public void LeaderboardEntry_RoundTrip_PreservesPositions()
    {
        LeaderboardEntry[] standings = CreateLeaderboardEntries();
        int expectedEntryCount = 1;
        int expectedPosition = 1;
        int expectedWins = 6;

        LeaderboardEntry[] result = RoundTrip(standings);

        Assert.AreEqual(expectedEntryCount, result.Length);
        Assert.AreEqual(expectedPosition, result[0].Position);
        Assert.AreEqual(SampleUsername, result[0].Account.Username);
        Assert.AreEqual(expectedWins, result[0].Wins);
    }

    [TestMethod]
    public void ServiceFault_RoundTrip_PreservesDiagnosticData()
    {
        ServiceFault fault = new ServiceFault
        {
            FailureCode = "SERVER_UNAVAILABLE",
            CorrelationId = Guid.NewGuid().ToString("D"),
            IsTransient = true
        };

        ServiceFault result = RoundTrip(fault);

        Assert.AreEqual(fault.FailureCode, result.FailureCode);
        Assert.AreEqual(fault.CorrelationId, result.CorrelationId);
        Assert.AreEqual(fault.IsTransient, result.IsTransient);
    }

    private static AuthenticatedSession CreateAuthenticatedSession()
    {
        return new AuthenticatedSession
        {
            SessionToken = SampleSessionToken,
            Account = CreateAccountReference(),
            EmailAddress = "player@example.com",
            PreferredLocaleCode = EnglishLocaleCode,
            ExpiresAtUtc = new DateTime(2026, 10, 15, 18, 0, 0, DateTimeKind.Utc)
        };
    }

    private static AccountRegistrationCredentials CreateRegistrationCredentials()
    {
        return new AccountRegistrationCredentials
        {
            Username = SampleUsername,
            EmailAddress = "player@example.com",
            Password = "ValidPassword1!",
            PreferredLocaleCode = EnglishLocaleCode,
            ProfileImage = new ProfileImageContent
            {
                MediaType = PngMediaType,
                Content = new byte[] { 1, 2, 3 }
            }
        };
    }

    private static PlayerProfile CreatePlayerProfile()
    {
        return new PlayerProfile
        {
            Account = CreateAccountReference(),
            HasProfileImage = true,
            Statistics = new PlayerStatistics
            {
                MatchesPlayed = 10,
                Wins = 6,
                Losses = 3,
                Draws = 1,
                WinRate = 0.6
            }
        };
    }

    private static LeaderboardEntry[] CreateLeaderboardEntries()
    {
        return new[]
        {
            new LeaderboardEntry
            {
                Position = 1,
                Account = CreateAccountReference(),
                HasProfileImage = true,
                MatchesPlayed = 10,
                Wins = 6,
                WinRate = 0.6
            }
        };
    }

    private static AccountReference CreateAccountReference()
    {
        return new AccountReference
        {
            AccountId = SampleAccountId,
            Username = SampleUsername
        };
    }

    internal static TContract RoundTrip<TContract>(TContract value)
    {
        DataContractSerializer serializer = new DataContractSerializer(typeof(TContract));
        using MemoryStream stream = new MemoryStream();
        serializer.WriteObject(stream, value);
        stream.Position = 0;
        object? result = serializer.ReadObject(stream);
        return (TContract)TestAssert.IsNotNull(result);
    }
}
