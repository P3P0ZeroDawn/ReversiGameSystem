using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class ChatServiceTests
{
    [TestMethod]
    public void Send_EmptyMessage_DoesNotStoreMessage()
    {
        // Arrange
        ChatStore store = new ChatStore();
        ChatService service = new ChatService(CreateActiveSession(), store);

        // Act
        ChatSendResult result = service.Send("   ", out ChatMessage? message, out _);

        // Assert
        Assert.AreEqual(ChatSendResult.EmptyMessage, result);
        Assert.AreEqual(true, message == null);
        Assert.AreEqual(0, GetMessages(service).Count);
    }

    [TestMethod]
    public void Send_ValidMessage_TrimsAndStoresMessage()
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
        Assert.AreEqual(ChatSendResult.Success, result);
        Assert.AreEqual(true, message != null);
        ChatMessage actualMessage = TestAssert.IsNotNull(message);
        Assert.AreEqual("Pedro", actualMessage.SenderUsername);
        Assert.AreEqual("Good move!", actualMessage.Content);
        Assert.AreEqual(now, actualMessage.SentAtUtc);
        Assert.AreEqual(1, GetMessages(service).Count);
    }

    [TestMethod]
    public void Send_MessageAboveLimit_DoesNotStoreMessage()
    {
        // Arrange
        ChatStore store = new ChatStore();
        ChatService service = new ChatService(CreateActiveSession(), store);
        string content = new string('a', ChatStore.MaximumMessageLength + 1);

        // Act
        ChatSendResult result = service.Send(content, out _, out _);

        // Assert
        Assert.AreEqual(ChatSendResult.MessageTooLong, result);
        Assert.AreEqual(0, GetMessages(service).Count);
    }

    [TestMethod]
    public void Send_InactiveConversation_ReturnsUnauthorized()
    {
        // Arrange
        MatchSession session = new MatchSession("WAIT01", 40, "Pedro", null);
        ChatService service = new ChatService(session, new ChatStore());

        // Act
        ChatSendResult result = service.Send("Hello", out _, out _);
        ChatReadResult readResult = service.TryGetMessages(
            out IReadOnlyList<ChatMessage> messages);

        // Assert
        Assert.AreEqual(ChatSendResult.Unauthorized, result);
        Assert.AreEqual(ChatReadResult.Unauthorized, readResult);
        Assert.AreEqual(0, messages.Count);
    }

    [TestMethod]
    public void Send_GuestSession_ReturnsUnauthorized()
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
        Assert.AreEqual(ChatSendResult.Unauthorized, result);
        Assert.AreEqual(ChatReadResult.Unauthorized, readResult);
        Assert.AreEqual(0, messages.Count);
    }

    [TestMethod]
    public void Send_ProhibitedSpanishContent_RecordsInfringement()
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
        Assert.AreEqual(ChatSendResult.MessageBlocked, result);
        Assert.AreEqual(true, message == null);
        Assert.AreEqual(1, store.GetInfringementCount("Pedro"));
        Assert.AreEqual(0, GetMessages(service).Count);
    }

    [TestMethod]
    public void Send_ProhibitedSubstringInsideWord_AllowsMessage()
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
        Assert.AreEqual(ChatSendResult.Success, result);
        Assert.AreEqual(true, message != null);
        Assert.AreEqual(0, store.GetInfringementCount("Pedro"));
    }

    [TestMethod]
    public void Send_ThirdRecentInfringement_AppliesMute()
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
        Assert.AreEqual(ChatSendResult.MessageBlocked, first);
        Assert.AreEqual(ChatSendResult.MessageBlocked, second);
        Assert.AreEqual(ChatSendResult.PlayerMuted, third);
        Assert.AreEqual(
            now.Add(ChatStore.MuteDuration),
            TestAssert.HasValue(muteEndsAtUtc));
        Assert.AreEqual(3, store.GetInfringementCount("Pedro"));
        Assert.AreEqual(0, GetMessages(service).Count);
    }

    [TestMethod]
    public void Send_AfterMuteExpires_StoresSafeMessage()
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
        Assert.AreEqual(ChatSendResult.PlayerMuted, mutedResult);
        Assert.AreEqual(ChatSendResult.Success, successResult);
        Assert.AreEqual(true, message != null);
        Assert.AreEqual(1, GetMessages(service).Count);
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
        Assert.AreEqual(ChatReadResult.Success, result);
        return messages;
    }
}
