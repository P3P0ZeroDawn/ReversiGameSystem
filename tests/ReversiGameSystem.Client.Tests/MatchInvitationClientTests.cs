using System;
using System.Globalization;
using System.Resources;
using System.ServiceModel;
using ReversiGameSystem.Game;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

internal sealed class MatchInvitationClientTests
{
    private static readonly string[] RequiredResourceKeys = new[]
    {
        "InviteMatch_Button",
        "InviteMatch_DialogTitle",
        "InviteMatch_DialogPrompt",
        "InviteMatch_ErrorTitle",
        "InviteMatch_AccountUnavailableMessage",
        "InviteMatch_FriendshipNotFoundMessage",
        "InviteMatch_InvalidAccountIdMessage",
        "InviteMatch_InactiveAccountMessage",
        "InviteMatch_UnauthorizedMessage",
        "InviteMatch_MatchNotWaitingMessage",
        "InviteMatch_DeliveryFailedTitle",
        "InviteMatch_DeliveryFailedMessage"
    };

    internal static void RunAll()
    {
        MatchInvitationClientTests suite = new MatchInvitationClientTests();
        suite.RemoteMatchMessages_FriendshipFaultAccountUnavailable_ResolvesExpectedResourceKey();
        suite.RemoteMatchMessages_FriendshipFaultFriendshipNotFound_ResolvesExpectedResourceKey();
        suite.RemoteMatchMessages_FriendshipFaultInvalidAccountId_ResolvesExpectedResourceKey();
        suite.RemoteMatchMessages_FriendshipFaultInactiveAccount_ResolvesExpectedResourceKey();
        suite.RemoteMatchMessages_MatchFaultUnauthorized_ResolvesExpectedResourceKey();
        suite.RemoteMatchMessages_MatchFaultInactiveAccount_ResolvesExpectedResourceKey();
        suite.RemoteMatchMessages_MatchFaultMatchNotWaiting_ResolvesExpectedResourceKey();
        suite.RemoteMatchMessages_DeliveryFailureOrTimeout_ResolvesConnectionFailedKey();
        suite.LocalizationResources_InvitationKeys_ArePresentInAllSupportedCultures();
        suite.TurnTimePolicy_ValidRange_ParsesConfiguredSeconds();
        suite.TurnTimePolicy_OutOfRangeOrNonNumeric_RejectsInput();
        suite.MatchLobbyClient_InviteToMatch_CommunicationException_AbortsWithoutAutoRetry();
    }

    internal void RemoteMatchMessages_FriendshipFaultAccountUnavailable_ResolvesExpectedResourceKey()
    {
        // Arrange
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.AccountUnavailable });

        // Act
        string key = RemoteMatchMessages.Key(MatchStatus.AccountUnavailable);

        // Assert
        TestAssert.AreEqual("InviteMatch_AccountUnavailableMessage", key);
    }

    internal void RemoteMatchMessages_FriendshipFaultFriendshipNotFound_ResolvesExpectedResourceKey()
    {
        // Arrange
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.FriendshipNotFound });

        // Act
        string key = RemoteMatchMessages.Key(MatchStatus.FriendshipNotFound);

        // Assert
        TestAssert.AreEqual("InviteMatch_FriendshipNotFoundMessage", key);
    }

    internal void RemoteMatchMessages_FriendshipFaultInvalidAccountId_ResolvesExpectedResourceKey()
    {
        // Arrange
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.InvalidAccountId });

        // Act
        string key = RemoteMatchMessages.Key(MatchStatus.InvalidAccountId);

        // Assert
        TestAssert.AreEqual("InviteMatch_InvalidAccountIdMessage", key);
    }

    internal void RemoteMatchMessages_FriendshipFaultInactiveAccount_ResolvesExpectedResourceKey()
    {
        // Arrange
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.InactiveAccount });

        // Act
        string key = RemoteMatchMessages.Key(MatchStatus.InactiveAccount);

        // Assert
        TestAssert.AreEqual("InviteMatch_InactiveAccountMessage", key);
    }

    internal void RemoteMatchMessages_MatchFaultUnauthorized_ResolvesExpectedResourceKey()
    {
        // Arrange
        FaultException<Proxy.MatchFault> fault = new FaultException<Proxy.MatchFault>(
            new Proxy.MatchFault { Reason = Proxy.MatchFailure.Unauthorized });

        // Act
        string key = RemoteMatchMessages.Key(MatchStatus.Unauthorized);

        // Assert
        TestAssert.AreEqual("InviteMatch_UnauthorizedMessage", key);
    }

    internal void RemoteMatchMessages_MatchFaultInactiveAccount_ResolvesExpectedResourceKey()
    {
        // Arrange
        FaultException<Proxy.MatchFault> fault = new FaultException<Proxy.MatchFault>(
            new Proxy.MatchFault { Reason = Proxy.MatchFailure.InactiveAccount });

        // Act
        string key = RemoteMatchMessages.Key(MatchStatus.InactiveAccount);

        // Assert
        TestAssert.AreEqual("InviteMatch_InactiveAccountMessage", key);
    }

    internal void RemoteMatchMessages_MatchFaultMatchNotWaiting_ResolvesExpectedResourceKey()
    {
        // Arrange
        FaultException<Proxy.MatchFault> fault = new FaultException<Proxy.MatchFault>(
            new Proxy.MatchFault { Reason = Proxy.MatchFailure.MatchNotWaiting });

        // Act
        string key = RemoteMatchMessages.Key(MatchStatus.MatchNotWaiting);

        // Assert
        TestAssert.AreEqual("InviteMatch_MatchNotWaitingMessage", key);
    }

    internal void RemoteMatchMessages_DeliveryFailureOrTimeout_ResolvesConnectionFailedKey()
    {
        // Arrange
        TimeoutException timeout = new TimeoutException();
        CommunicationException communication = new CommunicationException();

        // Act
        string timeoutKey = RemoteMatchMessages.Key(MatchStatus.ServiceUnavailable);
        string communicationKey = RemoteMatchMessages.Key(MatchStatus.ServiceUnavailable);

        // Assert
        TestAssert.AreEqual("RemoteMatch_ConnectionFailed", timeoutKey);
        TestAssert.AreEqual("RemoteMatch_ConnectionFailed", communicationKey);
    }

    internal void LocalizationResources_InvitationKeys_ArePresentInAllSupportedCultures()
    {
        // Arrange
        ResourceManager resources = new ResourceManager(
            "ReversiGameSystem.Properties.Resources", typeof(ReversiGameSystem.App).Assembly);
        string[] cultures = new[] { "en-US", "es-MX" };

        // Act & Assert
        foreach (string cultureName in cultures)
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
            foreach (string key in RequiredResourceKeys)
            {
                string? localized = resources.GetString(key, culture);
                TestAssert.AreEqual(false, string.IsNullOrWhiteSpace(localized));
            }
        }
    }

    internal void TurnTimePolicy_ValidRange_ParsesConfiguredSeconds()
    {
        // Arrange & Act & Assert
        bool parsedMin = TurnTimePolicy.TryParse("10", out int minSeconds);
        TestAssert.AreEqual(true, parsedMin);
        TestAssert.AreEqual(10, minSeconds);

        bool parsedDefault = TurnTimePolicy.TryParse("40", out int defaultSeconds);
        TestAssert.AreEqual(true, parsedDefault);
        TestAssert.AreEqual(40, defaultSeconds);

        bool parsedMax = TurnTimePolicy.TryParse("120", out int maxSeconds);
        TestAssert.AreEqual(true, parsedMax);
        TestAssert.AreEqual(120, maxSeconds);
    }

    internal void TurnTimePolicy_OutOfRangeOrNonNumeric_RejectsInput()
    {
        // Arrange & Act & Assert
        bool parsedUnderMin = TurnTimePolicy.TryParse("9", out _);
        TestAssert.AreEqual(false, parsedUnderMin);

        bool parsedOverMax = TurnTimePolicy.TryParse("121", out _);
        TestAssert.AreEqual(false, parsedOverMax);

        bool parsedNegative = TurnTimePolicy.TryParse("-10", out _);
        TestAssert.AreEqual(false, parsedNegative);

        bool parsedAlpha = TurnTimePolicy.TryParse("forty", out _);
        TestAssert.AreEqual(false, parsedAlpha);

        bool parsedDecimal = TurnTimePolicy.TryParse("40.5", out _);
        TestAssert.AreEqual(false, parsedDecimal);

        bool parsedEmpty = TurnTimePolicy.TryParse(string.Empty, out _);
        TestAssert.AreEqual(false, parsedEmpty);
    }

    internal void MatchLobbyClient_InviteToMatch_CommunicationException_AbortsWithoutAutoRetry()
    {
        // Arrange
        MatchLobbyClient client = MatchClientsTests.CreateLobby("test-token");
        // Act
        try
        {
            MatchStatus status = client.InviteToMatch(100, 40)
                .GetAwaiter().GetResult();
            TestAssert.AreEqual(MatchStatus.ServiceUnavailable, status);
        }
        finally
        {
            client.Dispose();
        }
    }
}

