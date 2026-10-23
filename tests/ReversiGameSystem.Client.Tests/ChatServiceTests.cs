using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal sealed class ChatServiceTests
{
    internal void Send_EmptyMessage_DoesNotStoreMessage()
    {
        // Arrange
        ChatStore store = new ChatStore();
        ChatService service = new ChatService(CreateActiveSession(), store);

        // Act
        ChatSendResult result = service.Send("   ", out ChatMessage? message, out _);

        // Assert
        TestAssert.AreEqual(ChatSendResult.EmptyMessage, result);
        TestAssert.AreEqual(true, message == null);
        TestAssert.AreEqual(0, GetMessages(service).Count);
    }

    internal void Send_ValidMessage_TrimsAndStoresMessage()
    {
        // Arrange
        DateTime now = new DateTime(
            2026,
            9,
            19,
            12,
            0,
            0,
            DateTimeKind.Utc);
        ChatStore store = new ChatStore(() => now);
        ChatService service = new ChatService(CreateActiveSession(), store);

        // Act
        ChatSendResult result = service.Send(
            "  Good move!  ",
            out ChatMessage? message,
            out _);

        // Assert
        TestAssert.AreEqual(ChatSendResult.Success, result);
        TestAssert.AreEqual(true, message != null);
        ChatMessage actualMessage = TestAssert.IsNotNull(message);
        TestAssert.AreEqual("Pedro", actualMessage.SenderUsername);
        TestAssert.AreEqual("Good move!", actualMessage.Content);
        TestAssert.AreEqual(now, actualMessage.SentAtUtc);
        TestAssert.AreEqual(1, GetMessages(service).Count);
    }

    internal void Send_MessageAboveLimit_DoesNotStoreMessage()
    {
        // Arrange
        ChatStore store = new ChatStore();
        ChatService service = new ChatService(CreateActiveSession(), store);
        string content = new string('a', ChatStore.MaximumMessageLength + 1);

        // Act
        ChatSendResult result = service.Send(content, out _, out _);

        // Assert
        TestAssert.AreEqual(ChatSendResult.MessageTooLong, result);
        TestAssert.AreEqual(0, GetMessages(service).Count);
    }

    internal void Send_InactiveConversation_ReturnsUnauthorized()
    {
        // Arrange
        MatchSession session = new MatchSession("WAIT01", 40, "Pedro", null);
        ChatService service = new ChatService(session, new ChatStore());

        // Act
        ChatSendResult result = service.Send("Hello", out _, out _);
        ChatReadResult readResult = service.TryGetMessages(
            out IReadOnlyList<ChatMessage> messages);

        // Assert
        TestAssert.AreEqual(ChatSendResult.Unauthorized, result);
        TestAssert.AreEqual(ChatReadResult.Unauthorized, readResult);
        TestAssert.AreEqual(0, messages.Count);
    }

    internal void Send_GuestSession_ReturnsUnauthorized()
    {
        // Arrange
        MatchSession session = new MatchSession(
            "GUEST1",
            40,
            "Guest",
            "Guest");
        session.JoinAsSecondPlayer("Ana");
        ChatService service = new ChatService(session, new ChatStore());

        // Act
        ChatSendResult result = service.Send("Hello", out _, out _);
        ChatReadResult readResult = service.TryGetMessages(
            out IReadOnlyList<ChatMessage> messages);

        // Assert
        TestAssert.AreEqual(ChatSendResult.Unauthorized, result);
        TestAssert.AreEqual(ChatReadResult.Unauthorized, readResult);
        TestAssert.AreEqual(0, messages.Count);
    }

    internal void Send_ProhibitedSpanishContent_RecordsInfringement()
    {
        // Arrange
        ChatStore store = new ChatStore();
        ChatService service = new ChatService(CreateActiveSession(), store);

        // Act
        ChatSendResult result = service.Send(
            "Eres un estúpido",
            out ChatMessage? message,
            out _);

        // Assert
        TestAssert.AreEqual(ChatSendResult.MessageBlocked, result);
        TestAssert.AreEqual(true, message == null);
        TestAssert.AreEqual(1, store.GetInfringementCount("Pedro"));
        TestAssert.AreEqual(0, GetMessages(service).Count);
    }

    internal void Send_ProhibitedSubstringInsideWord_AllowsMessage()
    {
        // Arrange
        ChatStore store = new ChatStore();
        ChatService service = new ChatService(CreateActiveSession(), store);

        // Act
        ChatSendResult result = service.Send(
            "That move was made stupidly",
            out ChatMessage? message,
            out _);

        // Assert
        TestAssert.AreEqual(ChatSendResult.Success, result);
        TestAssert.AreEqual(true, message != null);
        TestAssert.AreEqual(0, store.GetInfringementCount("Pedro"));
    }

    internal void Send_ThirdRecentInfringement_AppliesMute()
    {
        // Arrange
        DateTime now = new DateTime(
            2026,
            9,
            19,
            12,
            0,
            0,
            DateTimeKind.Utc);
        ChatStore store = new ChatStore(() => now);
        ChatService service = new ChatService(CreateActiveSession(), store);

        // Act
        ChatSendResult first = service.Send("idiota", out _, out _);
        ChatSendResult second = service.Send("moron", out _, out _);
        ChatSendResult third = service.Send(
            "callate",
            out _,
            out DateTime? muteEndsAtUtc);

        // Assert
        TestAssert.AreEqual(ChatSendResult.MessageBlocked, first);
        TestAssert.AreEqual(ChatSendResult.MessageBlocked, second);
        TestAssert.AreEqual(ChatSendResult.PlayerMuted, third);
        TestAssert.AreEqual(
            now.Add(ChatStore.MuteDuration),
            TestAssert.HasValue(muteEndsAtUtc));
        TestAssert.AreEqual(3, store.GetInfringementCount("Pedro"));
        TestAssert.AreEqual(0, GetMessages(service).Count);
    }

    internal void Send_AfterMuteExpires_StoresSafeMessage()
    {
        // Arrange
        DateTime now = new DateTime(
            2026,
            9,
            19,
            12,
            0,
            0,
            DateTimeKind.Utc);
        ChatStore store = new ChatStore(() => now);
        ChatService service = new ChatService(CreateActiveSession(), store);
        service.Send("idiota", out _, out _);
        service.Send("moron", out _, out _);
        service.Send("callate", out _, out DateTime? muteEndsAtUtc);

        // Act
        ChatSendResult mutedResult = service.Send("Good move", out _, out _);
        now = TestAssert.HasValue(muteEndsAtUtc).AddSeconds(1);
        ChatSendResult successResult = service.Send(
            "Good move",
            out ChatMessage? message,
            out _);

        // Assert
        TestAssert.AreEqual(ChatSendResult.PlayerMuted, mutedResult);
        TestAssert.AreEqual(ChatSendResult.Success, successResult);
        TestAssert.AreEqual(true, message != null);
        TestAssert.AreEqual(1, GetMessages(service).Count);
    }

    private static MatchSession CreateActiveSession()
    {
        MatchSession session = new MatchSession("CHAT01", 40, "Pedro", null);
        session.JoinAsSecondPlayer("Ana");
        return session;
    }

    private static IReadOnlyList<ChatMessage> GetMessages(ChatService service)
    {
        ChatReadResult result = service.TryGetMessages(
            out IReadOnlyList<ChatMessage> messages);
        TestAssert.AreEqual(ChatReadResult.Success, result);
        return messages;
    }
}
