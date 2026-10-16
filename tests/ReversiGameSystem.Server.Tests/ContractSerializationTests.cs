using System;
using System.IO;
using System.Runtime.Serialization;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Leaderboards;
using ReversiGameSystem.Server.Contracts.Players;

namespace ReversiGameSystem.Server.Tests;

internal sealed class ContractSerializationTests
{
    internal void AuthenticationCredentials_RoundTrip_PreservesCredentials()
    {
        // Arrange
        AuthenticationCredentials credentials = new AuthenticationCredentials
        {
            LoginIdentifier = "player@example.com",
            Password = "ValidPassword1!"
        };

        // Act
        AuthenticationCredentials result = RoundTrip(credentials);

        // Assert
        TestAssert.AreEqual(credentials.LoginIdentifier, result.LoginIdentifier);
        TestAssert.AreEqual(credentials.Password, result.Password);
    }

    internal void AuthenticationOutcome_RoundTrip_PreservesSession()
    {
        // Arrange
        AuthenticationOutcome outcome = CreateAuthenticationOutcome();

        // Act
        AuthenticationOutcome result = RoundTrip(outcome);

        // Assert
        TestAssert.AreEqual(AuthenticationStatus.Success, result.Status);
        AuthenticatedSession session = TestAssert.IsNotNull(result.Session);
        TestAssert.AreEqual(42L, session.Account.AccountId);
        TestAssert.AreEqual("player_one", session.Account.Username);
        TestAssert.AreEqual("session-token", session.SessionToken);
        TestAssert.AreEqual("en-US", session.PreferredLocaleCode);
        TestAssert.AreEqual(outcome.Session?.ExpiresAtUtc, session.ExpiresAtUtc);
    }

    internal void AccountRegistrationCredentials_RoundTrip_PreservesProfileImage()
    {
        // Arrange
        AccountRegistrationCredentials credentials = CreateRegistrationCredentials();

        // Act
        AccountRegistrationCredentials result = RoundTrip(credentials);

        // Assert
        TestAssert.AreEqual(credentials.Username, result.Username);
        TestAssert.AreEqual(credentials.EmailAddress, result.EmailAddress);
        TestAssert.AreEqual(credentials.PreferredLocaleCode, result.PreferredLocaleCode);
        ProfileImageContent image = TestAssert.IsNotNull(result.ProfileImage);
        TestAssert.AreEqual("image/png", image.MediaType);
        TestAssert.AreEqual(3, image.Content.Length);
    }

    internal void PlayerProfileLookupOutcome_RoundTrip_PreservesStatistics()
    {
        // Arrange
        PlayerProfileLookupOutcome outcome = CreatePlayerProfileOutcome();

        // Act
        PlayerProfileLookupOutcome result = RoundTrip(outcome);

        // Assert
        TestAssert.AreEqual(PlayerProfileStatus.Success, result.Status);
        PlayerProfileSnapshot profile = TestAssert.IsNotNull(result.Profile);
        TestAssert.AreEqual(42L, profile.Account.AccountId);
        TestAssert.AreEqual(true, profile.HasProfileImage);
        TestAssert.AreEqual(10, profile.Statistics.MatchesPlayed);
        TestAssert.AreEqual(6, profile.Statistics.Wins);
        TestAssert.AreEqual(0.6, profile.Statistics.WinRate);
    }

    internal void LeaderboardStandings_RoundTrip_PreservesPositions()
    {
        // Arrange
        LeaderboardStandings standings = CreateLeaderboardStandings();

        // Act
        LeaderboardStandings result = RoundTrip(standings);

        // Assert
        TestAssert.AreEqual(LeaderboardStatus.Success, result.Status);
        TestAssert.AreEqual(standings.GeneratedAtUtc, result.GeneratedAtUtc);
        TestAssert.AreEqual(1, result.Positions.Length);
        TestAssert.AreEqual(1, result.Positions[0].Position);
        TestAssert.AreEqual("player_one", result.Positions[0].Account.Username);
        TestAssert.AreEqual(6, result.Positions[0].Wins);
    }

    internal void ServiceFault_RoundTrip_PreservesDiagnosticData()
    {
        // Arrange
        ServiceFault fault = new ServiceFault
        {
            FailureCode = "SERVER_UNAVAILABLE",
            CorrelationId = Guid.NewGuid().ToString("D"),
            IsTransient = true
        };

        // Act
        ServiceFault result = RoundTrip(fault);

        // Assert
        TestAssert.AreEqual(fault.FailureCode, result.FailureCode);
        TestAssert.AreEqual(fault.CorrelationId, result.CorrelationId);
        TestAssert.AreEqual(fault.IsTransient, result.IsTransient);
    }

    private static AuthenticationOutcome CreateAuthenticationOutcome()
    {
        return new AuthenticationOutcome
        {
            Status = AuthenticationStatus.Success,
            Session = new AuthenticatedSession
            {
                SessionToken = "session-token",
                Account = CreateAccountReference(),
                EmailAddress = "player@example.com",
                PreferredLocaleCode = "en-US",
                ExpiresAtUtc = new DateTime(2026, 10, 15, 18, 0, 0, DateTimeKind.Utc)
            }
        };
    }

    private static AccountRegistrationCredentials CreateRegistrationCredentials()
    {
        return new AccountRegistrationCredentials
        {
            Username = "player_one",
            EmailAddress = "player@example.com",
            Password = "ValidPassword1!",
            PreferredLocaleCode = "en-US",
            ProfileImage = new ProfileImageContent
            {
                MediaType = "image/png",
                Content = new byte[] { 1, 2, 3 }
            }
        };
    }

    private static PlayerProfileLookupOutcome CreatePlayerProfileOutcome()
    {
        return new PlayerProfileLookupOutcome
        {
            Status = PlayerProfileStatus.Success,
            Profile = new PlayerProfileSnapshot
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
            }
        };
    }

    private static LeaderboardStandings CreateLeaderboardStandings()
    {
        return new LeaderboardStandings
        {
            Status = LeaderboardStatus.Success,
            GeneratedAtUtc = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc),
            Positions = new[]
            {
                new LeaderboardPosition
                {
                    Position = 1,
                    Account = CreateAccountReference(),
                    HasProfileImage = true,
                    MatchesPlayed = 10,
                    Wins = 6,
                    WinRate = 0.6
                }
            }
        };
    }

    private static AccountReference CreateAccountReference()
    {
        return new AccountReference
        {
            AccountId = 42,
            Username = "player_one"
        };
    }

    private static TContract RoundTrip<TContract>(TContract value)
    {
        DataContractSerializer serializer = new DataContractSerializer(typeof(TContract));
        using MemoryStream stream = new MemoryStream();
        serializer.WriteObject(stream, value);
        stream.Position = 0;
        object? result = serializer.ReadObject(stream);
        return (TContract)TestAssert.IsNotNull(result);
    }
}
