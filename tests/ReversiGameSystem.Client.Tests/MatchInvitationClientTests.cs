using System;
using System.Globalization;
using System.Resources;
using System.ServiceModel;
using ReversiGameSystem.Game;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

internal static class MatchInvitationClientTests
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
        "RemoteMatch_InactiveAccount",
        "RemoteMatch_Unauthorized",
        "RemoteMatch_NotWaiting",
        "InviteMatch_DeliveryFailedTitle",
        "InviteMatch_DeliveryFailedMessage"
    };

    internal static void RunAll()
    {
        RemoteMatchMessages_FriendshipFaultAccountUnavailable_ResolvesExpectedResourceKey();
        RemoteMatchMessages_FriendshipFaultFriendshipNotFound_ResolvesExpectedResourceKey();
        RemoteMatchMessages_FriendshipFaultInvalidAccountId_ResolvesExpectedResourceKey();
        RemoteMatchMessages_FriendshipFaultInactiveAccount_ResolvesExpectedResourceKey();
        RemoteMatchMessages_MatchFaultUnauthorized_ResolvesExpectedResourceKey();
        RemoteMatchMessages_MatchFaultInactiveAccount_ResolvesExpectedResourceKey();
        RemoteMatchMessages_MatchFaultMatchNotWaiting_ResolvesExpectedResourceKey();
        RemoteMatchMessages_ServiceUnavailable_ResolvesConnectionFailedKey();
        LocalizationResources_InvitationKeys_ArePresentInAllSupportedCultures();
        TurnTimePolicy_ValidRange_ParsesConfiguredSeconds();
        TurnTimePolicy_OutOfRangeOrNonNumeric_RejectsInput();
        MatchLobbyClient_InviteToMatch_CommunicationException_AbortsWithoutAutoRetry();
    }

    internal static void RemoteMatchMessages_FriendshipFaultAccountUnavailable_ResolvesExpectedResourceKey()
    {
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.AccountUnavailable });

        string key = RemoteMatchMessages.GetMessageResourceKey(
            MatchContractMapper.MapFailure(fault));

        TestAssert.AreEqual("InviteMatch_AccountUnavailableMessage", key);
    }

    internal static void RemoteMatchMessages_FriendshipFaultFriendshipNotFound_ResolvesExpectedResourceKey()
    {
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.FriendshipNotFound });

        string key = RemoteMatchMessages.GetMessageResourceKey(
            MatchContractMapper.MapFailure(fault));

        TestAssert.AreEqual("InviteMatch_FriendshipNotFoundMessage", key);
    }

    internal static void RemoteMatchMessages_FriendshipFaultInvalidAccountId_ResolvesExpectedResourceKey()
    {
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.InvalidAccountId });

        string key = RemoteMatchMessages.GetMessageResourceKey(
            MatchContractMapper.MapFailure(fault));

        TestAssert.AreEqual("InviteMatch_InvalidAccountIdMessage", key);
    }

    internal static void RemoteMatchMessages_FriendshipFaultInactiveAccount_ResolvesExpectedResourceKey()
    {
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.InactiveAccount });

        string key = RemoteMatchMessages.GetMessageResourceKey(
            MatchContractMapper.MapFailure(fault));

        TestAssert.AreEqual("RemoteMatch_InactiveAccount", key);
    }

    internal static void RemoteMatchMessages_MatchFaultUnauthorized_ResolvesExpectedResourceKey()
    {
        FaultException<Proxy.MatchFault> fault = new FaultException<Proxy.MatchFault>(
            new Proxy.MatchFault { Reason = Proxy.MatchFailure.Unauthorized });

        string key = RemoteMatchMessages.GetMessageResourceKey(
            MatchContractMapper.MapFailure(fault));

        TestAssert.AreEqual("RemoteMatch_Unauthorized", key);
    }

    internal static void RemoteMatchMessages_MatchFaultInactiveAccount_ResolvesExpectedResourceKey()
    {
        FaultException<Proxy.MatchFault> fault = new FaultException<Proxy.MatchFault>(
            new Proxy.MatchFault { Reason = Proxy.MatchFailure.InactiveAccount });

        string key = RemoteMatchMessages.GetMessageResourceKey(
            MatchContractMapper.MapFailure(fault));

        TestAssert.AreEqual("RemoteMatch_InactiveAccount", key);
    }

    internal static void RemoteMatchMessages_MatchFaultMatchNotWaiting_ResolvesExpectedResourceKey()
    {
        FaultException<Proxy.MatchFault> fault = new FaultException<Proxy.MatchFault>(
            new Proxy.MatchFault { Reason = Proxy.MatchFailure.MatchNotWaiting });

        string key = RemoteMatchMessages.GetMessageResourceKey(
            MatchContractMapper.MapFailure(fault));

        TestAssert.AreEqual("RemoteMatch_NotWaiting", key);
    }

    internal static void RemoteMatchMessages_ServiceUnavailable_ResolvesConnectionFailedKey()
    {

        string resourceKey = RemoteMatchMessages.GetMessageResourceKey(
            MatchStatus.ServiceUnavailable);

        TestAssert.AreEqual("RemoteMatch_ConnectionFailed", resourceKey);
    }

    internal static void LocalizationResources_InvitationKeys_ArePresentInAllSupportedCultures()
    {
        ResourceManager resources = new ResourceManager(
            "ReversiGameSystem.Properties.Resources", typeof(ReversiGameSystem.App).Assembly);
        string[] cultures = new[] { "en-US", "es-MX" };

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

    internal static void TurnTimePolicy_ValidRange_ParsesConfiguredSeconds()
    {
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

    internal static void TurnTimePolicy_OutOfRangeOrNonNumeric_RejectsInput()
    {
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

    internal static void MatchLobbyClient_InviteToMatch_CommunicationException_AbortsWithoutAutoRetry()
    {
        MatchLobbyClient client = MatchClientsTests.CreateLobby("test-token");
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

