using System;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.ServiceApi;
using ReversiGameSystem.Server.Tests.TestDoubles;
using WcfMatchChatService = ReversiGameSystem.Server.ServiceApi.MatchChatService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.MatchChatServiceTests;

public abstract class MatchChatServiceTestBase : IDisposable
{
    private readonly MatchCallbackSubscriptions<IMatchChatServiceCallback> _callbackSubscriptions;

    protected MatchChatServiceTestBase()
    {
        Clock = new UtcClockStub();
        Sessions = new SessionStore(Clock);
        Repository = new ChatRepositoryStub();
        FirstSession = TestSessionHelper.CreateSession(Sessions, 1L, "PlayerOne");
        ApplicationService = new ChatApplicationService(Sessions, Repository, Clock);
        _callbackSubscriptions = new MatchCallbackSubscriptions<IMatchChatServiceCallback>(
            (_, _) => true,
            NullLogger.Instance);
        Service = new WcfMatchChatService(ApplicationService, _callbackSubscriptions);
    }

    private protected UtcClockStub Clock { get; }

    protected SessionStore Sessions { get; }

    private protected ChatRepositoryStub Repository { get; }

    protected AuthenticatedSession FirstSession { get; }

    protected ChatApplicationService ApplicationService { get; }

    protected WcfMatchChatService Service { get; }

    public void Dispose()
    {
        _callbackSubscriptions.Dispose();
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
