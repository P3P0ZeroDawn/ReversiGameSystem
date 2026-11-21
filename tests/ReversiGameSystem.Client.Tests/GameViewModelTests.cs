using System;
using System.Threading;
using ReversiGameSystem.Chat;
using ReversiGameSystem.Features.Gameplay;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

internal static class GameViewModelTests
{
    private const long MatchId = 19;
    private static readonly DateTime MuteExpiration =
        new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

    internal static void UpdateClock_ActiveMute_MarksPlayerMuted()
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

        TestAssert.AreEqual(true, model.IsMuted);
    }

    internal static void UpdateClock_ExpiredMute_ClearsMute()
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

        TestAssert.AreEqual(false, model.IsMuted);
    }

    internal static void SendMessage_MutedPlayer_ReturnsPlayerMuted()
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

        ChatStatus status = model.SendMessage().GetAwaiter().GetResult();

        TestAssert.AreEqual(ChatStatus.PlayerMuted, status);
    }
}
