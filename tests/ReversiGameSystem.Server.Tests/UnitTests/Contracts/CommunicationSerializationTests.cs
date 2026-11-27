using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Chat;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Friendships;
using ReversiGameSystem.Server.Contracts.Players;

namespace ReversiGameSystem.Server.Tests.UnitTests.Contracts;

/// <summary>
/// Checks optional values, images, social data and typed business failures in transit.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class CommunicationSerializationTests
{
    private const string PngMediaType = "image/png";
    private const string SpanishLocaleCode = "es-MX";
    private const string ProfilePicturesPieceStyle = "PROFILE_PICTURES";

    private static readonly DateTime TimestampUtc =
        new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void NewAccount_ProfileImage_PreservesContentAndVerificationData()
    {
        NewAccount account = new NewAccount
        {
            VerificationCode = "123456",
            Username = "player_one",
            EmailAddress = "player@example.com",
            Password = "DisposablePassword1!",
            PreferredLocaleCode = SpanishLocaleCode,
            ProfileImage = new ImageContent
            {
                MediaType = PngMediaType,
                Content = new byte[] { 0, 1, 127, 128, 255 }
            }
        };

        NewAccount result = ContractSerializationTests.RoundTrip(account);

        ImageContent image = TestAssert.IsNotNull(result.ProfileImage);
        Assert.AreEqual(account.VerificationCode, result.VerificationCode);
        Assert.AreEqual(account.Username, result.Username);
        Assert.AreEqual(account.EmailAddress, result.EmailAddress);
        Assert.AreEqual(account.Password, result.Password);
        Assert.AreEqual(account.PreferredLocaleCode, result.PreferredLocaleCode);
        Assert.AreEqual(PngMediaType, image.MediaType);
        Assert.IsTrue(
            account.ProfileImage.Content.SequenceEqual(image.Content),
            "Image bytes changed during transport.");
    }

    [TestMethod]
    public void NewAccount_AbsentProfileImage_PreservesNull()
    {
        NewAccount account = new NewAccount { ProfileImage = null };

        NewAccount result = ContractSerializationTests.RoundTrip(account);

        Assert.IsNull(result.ProfileImage);
    }

    [TestMethod]
    public void PlayerPreferences_LocalZone_PreservesZoneAndBackground()
    {
        PlayerPreferences preferences = new PlayerPreferences
        {
            PreferredLocaleCode = SpanishLocaleCode,
            TimeZoneId = null,
            PieceStyleCode = ProfilePicturesPieceStyle,
            UseCustomBoardBackground = true
        };

        PlayerPreferences result = ContractSerializationTests.RoundTrip(preferences);

        Assert.IsNull(result.TimeZoneId);
        Assert.AreEqual(SpanishLocaleCode, result.PreferredLocaleCode);
        Assert.AreEqual(ProfilePicturesPieceStyle, result.PieceStyleCode);
        Assert.IsTrue(result.UseCustomBoardBackground);
    }

    [TestMethod]
    public void GuestSession_RoundTrip_PreservesConfirmedIdentityAndExpiry()
    {
        GuestSession session = new GuestSession
        {
            SessionToken = "disposable-guest-session",
            DisplayName = "Guest-player",
            ExpiresAtUtc = TimestampUtc
        };
        DateTimeKind expectedKind = DateTimeKind.Utc;

        GuestSession result = ContractSerializationTests.RoundTrip(session);

        Assert.AreEqual(session.SessionToken, result.SessionToken);
        Assert.AreEqual(session.DisplayName, result.DisplayName);
        Assert.AreEqual(TimestampUtc, result.ExpiresAtUtc);
        Assert.AreEqual(expectedKind, result.ExpiresAtUtc.Kind);
    }

    [TestMethod]
    public void FriendRequest_RoundTrip_PreservesRequestAndBothAccounts()
    {
        long expectedSenderId = 42L;
        string expectedSenderName = "sender";
        long expectedRecipientId = 43L;
        string expectedRecipientName = "recipient";
        FriendRequest request = new FriendRequest
        {
            RequestId = "friend-request-1",
            Sender = new AccountReference
            {
                AccountId = expectedSenderId,
                Username = expectedSenderName
            },
            Recipient = new AccountReference
            {
                AccountId = expectedRecipientId,
                Username = expectedRecipientName
            },
            CreatedAtUtc = TimestampUtc
        };

        FriendRequest result = ContractSerializationTests.RoundTrip(request);

        Assert.AreEqual(request.RequestId, result.RequestId);
        Assert.AreEqual(expectedSenderId, result.Sender.AccountId);
        Assert.AreEqual(expectedSenderName, result.Sender.Username);
        Assert.AreEqual(expectedRecipientId, result.Recipient.AccountId);
        Assert.AreEqual(expectedRecipientName, result.Recipient.Username);
        Assert.AreEqual(TimestampUtc, result.CreatedAtUtc);
    }

    [TestMethod]
    public void MatchChatEntry_RoundTrip_PreservesCompositeIdentityAndUnicode()
    {
        long expectedMatchId = 7L;
        int expectedMessageNumber = 12;
        int expectedSenderSeat = 2;
        MatchChatEntry message = new MatchChatEntry
        {
            MatchId = expectedMatchId,
            MessageNumber = expectedMessageNumber,
            SenderSeatNumber = expectedSenderSeat,
            Content = "Valid move: ñ 🎮",
            SentAtUtc = TimestampUtc
        };

        MatchChatEntry result = ContractSerializationTests.RoundTrip(message);

        Assert.AreEqual(expectedMatchId, result.MatchId);
        Assert.AreEqual(expectedMessageNumber, result.MessageNumber);
        Assert.AreEqual(expectedSenderSeat, result.SenderSeatNumber);
        Assert.AreEqual(message.Content, result.Content);
        Assert.AreEqual(TimestampUtc, result.SentAtUtc);
    }

    [TestMethod]
    public void ChatFault_MutedAndBlocked_PreservesOptionalMuteExpiry()
    {
        ChatFailure expectedMuted = ChatFailure.PlayerMuted;
        ChatFailure expectedBlocked = ChatFailure.MessageBlocked;
        DateTime? expectedMuteEnd = TimestampUtc;
        ChatFault[] faults = new[]
        {
            new ChatFault { Reason = expectedMuted, MuteEndsAtUtc = TimestampUtc },
            new ChatFault { Reason = expectedBlocked, MuteEndsAtUtc = null }
        };

        ChatFault[] result = ContractSerializationTests.RoundTrip(faults);

        Assert.AreEqual(expectedMuted, result[0].Reason);
        Assert.AreEqual(expectedMuteEnd, result[0].MuteEndsAtUtc);
        Assert.AreEqual(expectedBlocked, result[1].Reason);
        Assert.IsNull(result[1].MuteEndsAtUtc);
    }

    [TestMethod]
    public void ChatMuteNotification_RoundTrip_PreservesAccountSanctionWindow()
    {
        long expectedMatchId = 7L;
        ChatMuteNotification mute = new ChatMuteNotification
        {
            MatchId = expectedMatchId,
            ReasonCode = "REPEATED_VIOLATIONS",
            StartsAtUtc = TimestampUtc,
            EndsAtUtc = TimestampUtc.AddMinutes(10)
        };

        ChatMuteNotification result = ContractSerializationTests.RoundTrip(mute);

        Assert.AreEqual(expectedMatchId, result.MatchId);
        Assert.AreEqual(mute.ReasonCode, result.ReasonCode);
        Assert.AreEqual(mute.StartsAtUtc, result.StartsAtUtc);
        Assert.AreEqual(mute.EndsAtUtc, result.EndsAtUtc);
    }

    [TestMethod]
    public void BusinessFaults_EveryCatalogReason_RoundTripsTypedReason()
    {
        Type[] types = new[]
        {
            typeof(SessionFault), typeof(AccountFault), typeof(ProfileFault),
            typeof(PreferencesFault), typeof(LeaderboardFault), typeof(FriendshipFault),
            typeof(MatchFault), typeof(ChatFault)
        };

        KeyValuePair<object, object?>[] reasons = types.SelectMany(RoundTripFaultReasons).ToArray();

        foreach (KeyValuePair<object, object?> reason in reasons)
        {
            Assert.AreEqual(reason.Key, reason.Value);
        }
    }

    private static KeyValuePair<object, object?>[] RoundTripFaultReasons(Type type)
    {
        List<KeyValuePair<object, object?>> reasons = new List<KeyValuePair<object, object?>>();
        System.Reflection.PropertyInfo reason = TestAssert.IsNotNull(type.GetProperty("Reason"));
        foreach (object expected in Enum.GetValues(reason.PropertyType))
        {
            object fault = TestAssert.IsNotNull(Activator.CreateInstance(type));
            reason.SetValue(fault, expected);
            DataContractSerializer serializer = new DataContractSerializer(type);
            using MemoryStream stream = new MemoryStream();
            serializer.WriteObject(stream, fault);
            stream.Position = 0;
            object result = TestAssert.IsNotNull(serializer.ReadObject(stream));
            reasons.Add(new KeyValuePair<object, object?>(expected, reason.GetValue(result)));
        }

        return reasons.ToArray();
    }
}
