using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Statistics;
using ReversiGameSystem.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class ChatReadServiceTests
{
    [TestMethod]
    public void TryGetMessages_AuthorizedPlayer_SortsChronologically()
    {
        // Arrange
        DateTime now = new DateTime(
            2026,
            9,
            19,
            13,
            0,
            0,
            DateTimeKind.Utc);
        ChatStore store = new ChatStore(() => now);
        ChatService service = new ChatService(
            CreateSession("READ01", DiscColor.Black),
            store);
        service.Send("Later", out _, out _);
        now = now.AddHours(-1);
        service.Send("Earlier", out _, out _);

        // Act
        ChatReadResult result = service.TryGetMessages(
            out IReadOnlyList<ChatMessage> messages);

        // Assert
        Assert.AreEqual(ChatReadResult.Success, result);
        Assert.AreEqual(2, messages.Count);
        Assert.AreEqual("Earlier", messages[0].Content);
        Assert.AreEqual("Later", messages[1].Content);
    }

    [TestMethod]
    public void TryGetMessages_EmptyConversation_ReturnsEmpty()
    {
        // Arrange
        ChatService service = new ChatService(
            CreateSession("EMPTY1", DiscColor.Black),
            new ChatStore());

        // Act
        ChatReadResult result = service.TryGetMessages(
            out IReadOnlyList<ChatMessage> messages);

        // Assert
        Assert.AreEqual(ChatReadResult.Success, result);
        Assert.AreEqual(0, messages.Count);
    }

    [TestMethod]
    public void MessageReceived_OpenAuthorizedChat_AddsOnlyConversationMessage()
    {
        // Arrange
        ChatStore store = new ChatStore();
        ChatService reader = new ChatService(
            CreateSession("LIVE01", DiscColor.Black),
            store);
        ChatService participant = new ChatService(
            CreateSession("LIVE01", DiscColor.White),
            store);
        ChatService otherConversation = new ChatService(
            CreateSession("OTHER1", DiscColor.White),
            store);
        List<ChatMessage> received = new List<ChatMessage>();
        void OnMessage(ChatMessage message)
        {
            if (reader.CanReadMessage(message))
            {
                received.Add(message);
            }
        }

        // Act
        reader.MessageReceived += OnMessage;
        participant.Send("Hello Pedro", out _, out _);
        otherConversation.Send("Unrelated", out _, out _);
        reader.MessageReceived -= OnMessage;

        // Assert
        Assert.AreEqual(1, received.Count);
        Assert.AreEqual("Ana", received[0].SenderUsername);
        Assert.AreEqual("Hello Pedro", received[0].Content);
    }

    private static MatchSession CreateSession(string code, DiscColor localColor)
    {
        MatchSession session = new MatchSession(
            code,
            40,
            "Pedro",
            null,
            localColor);
        session.JoinAsSecondPlayer("Ana");
        return session;
    }
}
