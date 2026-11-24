using System;
using System.Globalization;
using System.Resources;
using System.ServiceModel;
using ReversiGameSystem.Game;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class MatchInvitationClientTests
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

    [TestMethod]
    public void RunAll()
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

    [TestMethod]
    public void RemoteMatchMessages_FriendshipFaultAccountUnavailable_ResolvesExpectedResourceKey()
    {
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.AccountUnavailable });

        string key = RemoteMatchMessages.GetMessageResourceKey(
            MatchContractMapper.MapFailure(fault));

        Assert.AreEqual("InviteMatch_AccountUnavailableMessage", key);
    }

    [TestMethod]
    public void RemoteMatchMessages_FriendshipFaultFriendshipNotFound_ResolvesExpectedResourceKey()
    {
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.FriendshipNotFound });

        string key = RemoteMatchMessages.GetMessageResourceKey(
            MatchContractMapper.MapFailure(fault));

        Assert.AreEqual("InviteMatch_FriendshipNotFoundMessage", key);
    }

    [TestMethod]
    public void RemoteMatchMessages_FriendshipFaultInvalidAccountId_ResolvesExpectedResourceKey()
    {
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.InvalidAccountId });

        string key = RemoteMatchMessages.GetMessageResourceKey(
            MatchContractMapper.MapFailure(fault));

        Assert.AreEqual("InviteMatch_InvalidAccountIdMessage", key);
    }

    [TestMethod]
    public void RemoteMatchMessages_FriendshipFaultInactiveAccount_ResolvesExpectedResourceKey()
    {
        FaultException<Proxy.FriendshipFault> fault = new FaultException<Proxy.FriendshipFault>(
            new Proxy.FriendshipFault { Reason = Proxy.FriendshipFailure.InactiveAccount });

        string key = RemoteMatchMessages.GetMessageResourceKey(
            MatchContractMapper.MapFailure(fault));

        Assert.AreEqual("RemoteMatch_InactiveAccount", key);
    }

    [TestMethod]
    public void RemoteMatchMessages_MatchFaultUnauthorized_ResolvesExpectedResourceKey()
    {
        FaultException<Proxy.MatchFault> fault = new FaultException<Proxy.MatchFault>(
            new Proxy.MatchFault { Reason = Proxy.MatchFailure.Unauthorized });

        string key = RemoteMatchMessages.GetMessageResourceKey(
            MatchContractMapper.MapFailure(fault));

        Assert.AreEqual("RemoteMatch_Unauthorized", key);
    }

    [TestMethod]
    public void RemoteMatchMessages_MatchFaultInactiveAccount_ResolvesExpectedResourceKey()
    {
        FaultException<Proxy.MatchFault> fault = new FaultException<Proxy.MatchFault>(
            new Proxy.MatchFault { Reason = Proxy.MatchFailure.InactiveAccount });

        string key = RemoteMatchMessages.GetMessageResourceKey(
            MatchContractMapper.MapFailure(fault));

        Assert.AreEqual("RemoteMatch_InactiveAccount", key);
    }

    [TestMethod]
    public void RemoteMatchMessages_MatchFaultMatchNotWaiting_ResolvesExpectedResourceKey()
    {
        FaultException<Proxy.MatchFault> fault = new FaultException<Proxy.MatchFault>(
            new Proxy.MatchFault { Reason = Proxy.MatchFailure.MatchNotWaiting });

        string key = RemoteMatchMessages.GetMessageResourceKey(
            MatchContractMapper.MapFailure(fault));

        Assert.AreEqual("RemoteMatch_NotWaiting", key);
    }

    [TestMethod]
    public void RemoteMatchMessages_ServiceUnavailable_ResolvesConnectionFailedKey()
    {

        string resourceKey = RemoteMatchMessages.GetMessageResourceKey(
            MatchStatus.ServiceUnavailable);

        Assert.AreEqual("RemoteMatch_ConnectionFailed", resourceKey);
    }

    [TestMethod]
    public void LocalizationResources_InvitationKeys_ArePresentInAllSupportedCultures()
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
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(localized));
            }
        }
    }

    [TestMethod]
    public void TurnTimePolicy_ValidRange_ParsesConfiguredSeconds()
    {
        bool parsedMin = TurnTimePolicy.TryParse("10", out int minSeconds);
        Assert.AreEqual(true, parsedMin);
        Assert.AreEqual(10, minSeconds);

        bool parsedDefault = TurnTimePolicy.TryParse("40", out int defaultSeconds);
        Assert.AreEqual(true, parsedDefault);
        Assert.AreEqual(40, defaultSeconds);

        bool parsedMax = TurnTimePolicy.TryParse("120", out int maxSeconds);
        Assert.AreEqual(true, parsedMax);
        Assert.AreEqual(120, maxSeconds);
    }

    [TestMethod]
    public void TurnTimePolicy_OutOfRangeOrNonNumeric_RejectsInput()
    {
        bool parsedUnderMin = TurnTimePolicy.TryParse("9", out _);
        Assert.AreEqual(false, parsedUnderMin);

        bool parsedOverMax = TurnTimePolicy.TryParse("121", out _);
        Assert.AreEqual(false, parsedOverMax);

        bool parsedNegative = TurnTimePolicy.TryParse("-10", out _);
        Assert.AreEqual(false, parsedNegative);

        bool parsedAlpha = TurnTimePolicy.TryParse("forty", out _);
        Assert.AreEqual(false, parsedAlpha);

        bool parsedDecimal = TurnTimePolicy.TryParse("40.5", out _);
        Assert.AreEqual(false, parsedDecimal);

        bool parsedEmpty = TurnTimePolicy.TryParse(string.Empty, out _);
        Assert.AreEqual(false, parsedEmpty);
    }

    [TestMethod]
    public void MatchLobbyClient_InviteToMatch_CommunicationException_AbortsWithoutAutoRetry()
    {
        MatchLobbyClient client = MatchClientsTests.CreateLobby("test-token");
        try
        {
            MatchStatus status = client.InviteToMatch(100, 40)
                .GetAwaiter().GetResult();
            Assert.AreEqual(MatchStatus.ServiceUnavailable, status);
        }
        finally
        {
            client.Dispose();
        }
    }
}

