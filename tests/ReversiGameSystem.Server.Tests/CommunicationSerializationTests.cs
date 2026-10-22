using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Chat;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Friendships;
using ReversiGameSystem.Server.Contracts.Players;

namespace ReversiGameSystem.Server.Tests;

/// <summary>
/// Checks optional values, images, social data and typed business failures in transit.
/// </summary>
internal sealed class CommunicationSerializationTests
{
    private static readonly DateTime TimestampUtc =
        new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    internal void NewAccount_ProfileImage_PreservesContentAndVerificationData()
    {
        // Arrange
        NewAccount account = new NewAccount
        {
            VerificationCode = "123456",
            Username = "player_one",
            EmailAddress = "player@example.com",
            Password = "DisposablePassword1!",
            PreferredLocaleCode = "es-MX",
            ProfileImage = new ImageContent
            {
                MediaType = "image/png",
                Content = new byte[] { 0, 1, 127, 128, 255 }
            }
        };

        // Act
        NewAccount result = ContractSerializationTests.RoundTrip(account);

        // Assert
        TestAssert.AreEqual(account.VerificationCode, result.VerificationCode);
        TestAssert.AreEqual(account.Username, result.Username);
        TestAssert.AreEqual(account.EmailAddress, result.EmailAddress);
        TestAssert.AreEqual(account.Password, result.Password);
        TestAssert.AreEqual(account.PreferredLocaleCode, result.PreferredLocaleCode);
        ImageContent image = TestAssert.IsNotNull(result.ProfileImage);
        TestAssert.AreEqual("image/png", image.MediaType);
        TestAssert.IsTrue(account.ProfileImage.Content.SequenceEqual(image.Content),
            "Image bytes changed during transport.");
    }

    internal void NewAccount_AbsentProfileImage_PreservesNull()
    {
        // Arrange
        NewAccount account = new NewAccount { ProfileImage = null };

        // Act
        NewAccount result = ContractSerializationTests.RoundTrip(account);

        // Assert
        TestAssert.AreEqual<ImageContent?>(null, result.ProfileImage);
    }

    internal void PlayerPreferences_LocalZone_PreservesZoneAndBackground()
    {
        // Arrange
        PlayerPreferences preferences = new PlayerPreferences
        {
            PreferredLocaleCode = "es-MX",
            TimeZoneId = null,
            PieceStyleCode = "PROFILE_PICTURES",
            UseCustomBoardBackground = true
        };

        // Act
        PlayerPreferences result = ContractSerializationTests.RoundTrip(preferences);

        // Assert
        TestAssert.AreEqual<string?>(null, result.TimeZoneId);
        TestAssert.AreEqual("es-MX", result.PreferredLocaleCode);
        TestAssert.AreEqual("PROFILE_PICTURES", result.PieceStyleCode);
        TestAssert.AreEqual(true, result.UseCustomBoardBackground);
    }

    internal void GuestSession_RoundTrip_PreservesConfirmedIdentityAndExpiry()
    {
        // Arrange
        GuestSession session = new GuestSession
        {
            SessionToken = "disposable-guest-session",
            DisplayName = "Guest-player",
            ExpiresAtUtc = TimestampUtc
        };

        // Act
        GuestSession result = ContractSerializationTests.RoundTrip(session);

        // Assert
        TestAssert.AreEqual(session.SessionToken, result.SessionToken);
        TestAssert.AreEqual(session.DisplayName, result.DisplayName);
        TestAssert.AreEqual(TimestampUtc, result.ExpiresAtUtc);
        TestAssert.AreEqual(DateTimeKind.Utc, result.ExpiresAtUtc.Kind);
    }

    internal void FriendRequest_RoundTrip_PreservesRequestAndBothAccounts()
    {
        // Arrange
        FriendRequest request = new FriendRequest
        {
            RequestId = "friend-request-1",
            Sender = new AccountReference { AccountId = 42, Username = "sender" },
            Recipient = new AccountReference { AccountId = 43, Username = "recipient" },
            CreatedAtUtc = TimestampUtc
        };

        // Act
        FriendRequest result = ContractSerializationTests.RoundTrip(request);

        // Assert
        TestAssert.AreEqual(request.RequestId, result.RequestId);
        TestAssert.AreEqual(42L, result.Sender.AccountId);
        TestAssert.AreEqual("sender", result.Sender.Username);
        TestAssert.AreEqual(43L, result.Recipient.AccountId);
        TestAssert.AreEqual("recipient", result.Recipient.Username);
        TestAssert.AreEqual(TimestampUtc, result.CreatedAtUtc);
    }

    internal void MatchChatEntry_RoundTrip_PreservesCompositeIdentityAndUnicode()
    {
        // Arrange
        MatchChatEntry message = new MatchChatEntry
        {
            MatchId = 7,
            MessageNumber = 12,
            SenderSeatNumber = 2,
            Content = "Valid move: ñ 🎮",
            SentAtUtc = TimestampUtc
        };

        // Act
        MatchChatEntry result = ContractSerializationTests.RoundTrip(message);

        // Assert
        TestAssert.AreEqual(7L, result.MatchId);
        TestAssert.AreEqual(12, result.MessageNumber);
        TestAssert.AreEqual(2, result.SenderSeatNumber);
        TestAssert.AreEqual(message.Content, result.Content);
        TestAssert.AreEqual(TimestampUtc, result.SentAtUtc);
    }

    internal void ChatFault_MutedAndBlocked_PreservesOptionalMuteExpiry()
    {
        // Arrange
        ChatFault[] faults = new[]
        {
            new ChatFault { Reason = ChatFailure.PlayerMuted, MuteEndsAtUtc = TimestampUtc },
            new ChatFault { Reason = ChatFailure.MessageBlocked, MuteEndsAtUtc = null }
        };

        // Act
        ChatFault[] result = ContractSerializationTests.RoundTrip(faults);

        // Assert
        TestAssert.AreEqual(ChatFailure.PlayerMuted, result[0].Reason);
        TestAssert.AreEqual<DateTime?>(TimestampUtc, result[0].MuteEndsAtUtc);
        TestAssert.AreEqual(ChatFailure.MessageBlocked, result[1].Reason);
        TestAssert.AreEqual<DateTime?>(null, result[1].MuteEndsAtUtc);
    }

    internal void ChatMuteNotification_RoundTrip_PreservesAccountSanctionWindow()
    {
        // Arrange
        ChatMuteNotification mute = new ChatMuteNotification
        {
            MatchId = 7,
            ReasonCode = "REPEATED_VIOLATIONS",
            StartsAtUtc = TimestampUtc,
            EndsAtUtc = TimestampUtc.AddMinutes(10)
        };

        // Act
        ChatMuteNotification result = ContractSerializationTests.RoundTrip(mute);

        // Assert
        TestAssert.AreEqual(7L, result.MatchId);
        TestAssert.AreEqual(mute.ReasonCode, result.ReasonCode);
        TestAssert.AreEqual(mute.StartsAtUtc, result.StartsAtUtc);
        TestAssert.AreEqual(mute.EndsAtUtc, result.EndsAtUtc);
    }

    internal void BusinessFaults_EveryCatalogReason_RoundTripsTypedReason()
    {
        // Arrange
        Type[] types = new[]
        {
            typeof(SessionFault), typeof(AccountFault), typeof(ProfileFault),
            typeof(PreferencesFault), typeof(LeaderboardFault), typeof(FriendshipFault),
            typeof(MatchFault), typeof(ChatFault)
        };

        // Act
        KeyValuePair<object, object?>[] reasons = types.SelectMany(RoundTripFaultReasons).ToArray();

        // Assert
        foreach (KeyValuePair<object, object?> reason in reasons)
        {
            TestAssert.AreEqual(reason.Key, reason.Value);
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
