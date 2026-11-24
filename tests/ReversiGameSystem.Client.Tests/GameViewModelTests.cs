using System;
using System.Threading;
using ReversiGameSystem.Chat;
using ReversiGameSystem.Features.Gameplay;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class GameViewModelTests
{
    private const long MatchId = 19;
    private static readonly DateTime MuteExpiration =
        new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void UpdateClock_ActiveMute_MarksPlayerMuted()
    {
        using GameplayClient gameplay = MatchClientsTests.CreateGameplay("unused");
        using MatchChatClient chat = MatchClientsTests.CreateChat("unused");
        chat.OnPlayerMuted(new Proxy.ChatMuteNotification
        {
            MatchId = MatchId,
            EndsAtUtc = MuteExpiration
        });
        GameViewModel model = new GameViewModel(
            gameplay, chat, MatchId, false, new SynchronizationContext());

        model.UpdateClock(MuteExpiration.AddSeconds(-1));

        Assert.AreEqual(true, model.IsMuted);
    }

    [TestMethod]
    public void UpdateClock_ExpiredMute_ClearsMute()
    {
        using GameplayClient gameplay = MatchClientsTests.CreateGameplay("unused");
        using MatchChatClient chat = MatchClientsTests.CreateChat("unused");
        chat.OnPlayerMuted(new Proxy.ChatMuteNotification
        {
            MatchId = MatchId,
            EndsAtUtc = MuteExpiration
        });
        GameViewModel model = new GameViewModel(
            gameplay, chat, MatchId, false, new SynchronizationContext());
        model.UpdateClock(MuteExpiration.AddSeconds(-1));

        model.UpdateClock(MuteExpiration);

        Assert.AreEqual(false, model.IsMuted);
    }

    [TestMethod]
    public void SendChatMessage_MutedPlayer_ReturnsPlayerMuted()
    {
        using GameplayClient gameplay = MatchClientsTests.CreateGameplay("unused");
        using MatchChatClient chat = MatchClientsTests.CreateChat("unused");
        chat.OnPlayerMuted(new Proxy.ChatMuteNotification
        {
            MatchId = MatchId,
            EndsAtUtc = DateTime.UtcNow.AddMinutes(1)
        });
        GameViewModel model = new GameViewModel(
            gameplay, chat, MatchId, false, new SynchronizationContext());
        model.MessageText = "A message while muted";

        ChatStatus status = model.SendChatMessage().GetAwaiter().GetResult();

        Assert.AreEqual(ChatStatus.PlayerMuted, status);
    }
}
