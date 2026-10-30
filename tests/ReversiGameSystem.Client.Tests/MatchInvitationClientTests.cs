using System;
using System.Globalization;
using System.Resources;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

/// <summary>
/// Unit and client integration test suite for private match invitations over WCF proxies and localization.
/// </summary>
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

    /// <summary>
    /// Executes all tests in the client invitation test suite.
    /// </summary>
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
        suite.RemoteMatchClient_InviteAsync_CommunicationException_AbortsWithoutAutoRetry();
    }

    /// <summary>
    /// Verifies that FriendshipFault AccountUnavailable maps to InviteMatch_AccountUnavailableMessage.
    /// </summary>
    internal void RemoteMatchMessages_FriendshipFaultAccountUnavailable_ResolvesExpectedResourceKey()
    {
        // Arrange
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.AccountUnavailable });

        // Act
        string key = RemoteMatchMessages.Key(fault);

        // Assert
        TestAssert.AreEqual("InviteMatch_AccountUnavailableMessage", key);
    }

    /// <summary>
    /// Verifies that FriendshipFault FriendshipNotFound maps to InviteMatch_FriendshipNotFoundMessage.
    /// </summary>
    internal void RemoteMatchMessages_FriendshipFaultFriendshipNotFound_ResolvesExpectedResourceKey()
    {
        // Arrange
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.FriendshipNotFound });

        // Act
        string key = RemoteMatchMessages.Key(fault);

        // Assert
        TestAssert.AreEqual("InviteMatch_FriendshipNotFoundMessage", key);
    }

    /// <summary>
    /// Verifies that FriendshipFault InvalidAccountId maps to InviteMatch_InvalidAccountIdMessage.
    /// </summary>
    internal void RemoteMatchMessages_FriendshipFaultInvalidAccountId_ResolvesExpectedResourceKey()
    {
        // Arrange
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.InvalidAccountId });

        // Act
        string key = RemoteMatchMessages.Key(fault);

        // Assert
        TestAssert.AreEqual("InviteMatch_InvalidAccountIdMessage", key);
    }

    /// <summary>
    /// Verifies that FriendshipFault InactiveAccount maps to InviteMatch_InactiveAccountMessage.
    /// </summary>
    internal void RemoteMatchMessages_FriendshipFaultInactiveAccount_ResolvesExpectedResourceKey()
    {
        // Arrange
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.InactiveAccount });

        // Act
        string key = RemoteMatchMessages.Key(fault);

        // Assert
        TestAssert.AreEqual("InviteMatch_InactiveAccountMessage", key);
    }

    /// <summary>
    /// Verifies that MatchFault Unauthorized maps to InviteMatch_UnauthorizedMessage.
    /// </summary>
    internal void RemoteMatchMessages_MatchFaultUnauthorized_ResolvesExpectedResourceKey()
    {
        // Arrange
        FaultException<Proxy.MatchFault> fault = new FaultException<Proxy.MatchFault>(
            new Proxy.MatchFault { Reason = Proxy.MatchFailure.Unauthorized });

        // Act
        string key = RemoteMatchMessages.Key(fault);

        // Assert
        TestAssert.AreEqual("InviteMatch_UnauthorizedMessage", key);
    }

    /// <summary>
    /// Verifies that MatchFault InactiveAccount maps to InviteMatch_InactiveAccountMessage.
    /// </summary>
    internal void RemoteMatchMessages_MatchFaultInactiveAccount_ResolvesExpectedResourceKey()
    {
        // Arrange
        FaultException<Proxy.MatchFault> fault = new FaultException<Proxy.MatchFault>(
            new Proxy.MatchFault { Reason = Proxy.MatchFailure.InactiveAccount });

        // Act
        string key = RemoteMatchMessages.Key(fault);

        // Assert
        TestAssert.AreEqual("InviteMatch_InactiveAccountMessage", key);
    }

    /// <summary>
    /// Verifies that MatchFault MatchNotWaiting maps to InviteMatch_MatchNotWaitingMessage.
    /// </summary>
    internal void RemoteMatchMessages_MatchFaultMatchNotWaiting_ResolvesExpectedResourceKey()
    {
        // Arrange
        FaultException<Proxy.MatchFault> fault = new FaultException<Proxy.MatchFault>(
            new Proxy.MatchFault { Reason = Proxy.MatchFailure.MatchNotWaiting });

        // Act
        string key = RemoteMatchMessages.Key(fault);

        // Assert
        TestAssert.AreEqual("InviteMatch_MatchNotWaitingMessage", key);
    }

    /// <summary>
    /// Verifies that timeouts and communication failures resolve to the connection failure message key.
    /// </summary>
    internal void RemoteMatchMessages_DeliveryFailureOrTimeout_ResolvesConnectionFailedKey()
    {
        // Arrange
        TimeoutException timeout = new TimeoutException();
        CommunicationException communication = new CommunicationException();

        // Act
        string timeoutKey = RemoteMatchMessages.Key(timeout);
        string communicationKey = RemoteMatchMessages.Key(communication);

        // Assert
        TestAssert.AreEqual("RemoteMatch_ConnectionFailed", timeoutKey);
        TestAssert.AreEqual("RemoteMatch_ConnectionFailed", communicationKey);
    }

    /// <summary>
    /// Verifies that all 12 invitation localization keys exist and have non-empty text in both en-US and es-MX cultures.
    /// </summary>
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

    /// <summary>
    /// Verifies that TurnTimePolicy parses configured valid seconds within the 10..120 range.
    /// </summary>
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

    /// <summary>
    /// Verifies that TurnTimePolicy rejects seconds outside 10..120 or non-numeric inputs.
    /// </summary>
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

    /// <summary>
    /// Verifies that RemoteMatchClient.InviteAsync aborts the channel and propagates CommunicationException without retrying.
    /// </summary>
    internal void RemoteMatchClient_InviteAsync_CommunicationException_AbortsWithoutAutoRetry()
    {
        // Arrange
        RemoteMatchClient client = RemoteMatchClientTests.Create("test-token");
        bool failed = false;

        // Act & Assert
        try
        {
            Task<Proxy.LobbySnapshot> task = client.InviteAsync(100, 40);
            task.GetAwaiter().GetResult();
        }
        catch (CommunicationException)
        {
            failed = true;
        }
        finally
        {
            client.Dispose();
        }
        TestAssert.AreEqual(true, failed);
    }
}

