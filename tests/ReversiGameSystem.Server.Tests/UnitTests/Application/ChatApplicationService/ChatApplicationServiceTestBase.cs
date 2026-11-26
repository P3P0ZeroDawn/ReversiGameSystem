using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Tests.TestDoubles;
using ChatService = ReversiGameSystem.Server.Application.Chat.ChatApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.ChatApplicationServiceTests;

public abstract class ChatApplicationServiceTestBase
{
    protected ChatApplicationServiceTestBase()
    {
        Clock = new UtcClockStub();
        Sessions = new SessionStore(Clock);
        Repository = new ChatRepositoryStub();
        FirstSession = CreateSession(1L, "PlayerOne");
        Service = new ChatService(Sessions, Repository, Clock);
    }

    private protected UtcClockStub Clock { get; }

    protected SessionStore Sessions { get; }

    private protected ChatRepositoryStub Repository { get; }

    protected AuthenticatedSession FirstSession { get; }

    protected ChatService Service { get; }

    protected AuthenticatedSession CreateSession(long accountId, string username)
    {
        return TestSessionHelper.CreateSession(Sessions, accountId, username);
    }

    protected ChatMessage CreateSampleMessage(
        long matchId = 10L,
        int messageNumber = 3,
        int senderSeatNumber = 1,
        string content = "Hello")
    {
        return new ChatMessage
        {
            MatchId = matchId,
            MessageNumber = messageNumber,
            SenderSeatNumber = senderSeatNumber,
            Content = content,
            SentAtUtc = Clock.UtcNow
        };
    }

    protected ChatMute CreateSampleMute()
    {
        return new ChatMute
        {
            ReasonCode = "REPEATED_VIOLATIONS",
            StartsAtUtc = Clock.UtcNow,
            EndsAtUtc = Clock.UtcNow.AddMinutes(10)
        };
    }
}
