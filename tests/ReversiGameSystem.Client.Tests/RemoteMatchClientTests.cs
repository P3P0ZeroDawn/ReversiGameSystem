using System;
using System.Globalization;
using System.Resources;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

internal static class RemoteMatchClientTests
{
    internal static void Callbacks_StaleVersionsAndDuplicateMessages_PreserveConfirmedState()
    {
        using RemoteMatchClient client = Create("unused");
        int stateChanges = 0;
        int messageChanges = 0;
        int presenceChanges = 0;
        client.StateChanged += _ => stateChanges++;
        client.MessagesChanged += _ => messageChanges++;
        client.ParticipantConnectionChanged += _ => presenceChanges++;
        client.OnMatchStarted(new Proxy.MatchStateSnapshot
        {
            MatchId = 7,
            MatchStateVersion = 3,
            Board = Array.Empty<Proxy.BoardCellState>()
        });
        client.OnMatchStateChanged(new Proxy.MatchStateSnapshot
        {
            MatchId = 7,
            MatchStateVersion = 2,
            Board = Array.Empty<Proxy.BoardCellState>()
        });
        client.OnMatchStateChanged(new Proxy.MatchStateSnapshot
        {
            MatchId = 7,
            MatchStateVersion = 3,
            Board = Array.Empty<Proxy.BoardCellState>()
        });
        client.OnChatMessageReceived(new Proxy.MatchChatEntry { MatchId = 7, MessageNumber = 2, Content = "second" });
        client.OnChatMessageReceived(new Proxy.MatchChatEntry { MatchId = 7, MessageNumber = 1, Content = "first" });
        client.OnChatMessageReceived(new Proxy.MatchChatEntry { MatchId = 7, MessageNumber = 2, Content = "second" });
        client.OnChatMessageReceived(new Proxy.MatchChatEntry { MatchId = 8, MessageNumber = 1, Content = "other" });
        client.OnParticipantConnectionChanged(7, 1, true);
        client.OnParticipantConnectionChanged(7, 2, false);
        TestAssert.AreEqual(1, stateChanges);
        TestAssert.AreEqual(3, messageChanges);
        TestAssert.AreEqual(2, presenceChanges);
        TestAssert.AreEqual(true, client.HasMatchState(7));
        MatchStateSnapshot state = client.ReadMatchState(7);
        TestAssert.AreEqual(3L, state.Version);
        TestAssert.AreEqual(2, client.ReadMessages(7).Length);
        TestAssert.AreEqual("first", client.ReadMessages(7)[0].Content);
        TestAssert.AreEqual(1, client.ReadMessages(8).Length);
        TestAssert.AreEqual(
            ParticipantConnectionState.Connected,
            client.ReadParticipantConnection(7, 1));
        TestAssert.AreEqual(
            ParticipantConnectionState.Disconnected,
            client.ReadParticipantConnection(7, 2));
        TestAssert.AreEqual(
            ParticipantConnectionState.Unknown,
            client.ReadParticipantConnection(8, 1));
    }

    internal static void Faults_SupportedCultures_ResolveLocalizedRecoveryAndRejectionMessages()
    {
        ResourceManager resources = new("ReversiGameSystem.Properties.Resources", typeof(ReversiGameSystem.App).Assembly);
        foreach (string culture in new[] { "en-US", "es-MX" })
        {
            foreach (string key in new[]
            {
                "RemoteMatch_MovesUnavailable",
                "RemoteMatch_StateFormat",
                "RemoteMatch_CloseView",
                "RemoteMatch_SessionExpired",
                "RemoteMatch_GuestUnavailable",
                "RemoteMatch_AccessRejected",
                "RemoteMatch_ServerFailure",
                "RemoteMatch_ConnectionFailed",
                "RemoteMatch_NotAvailable",
                "RemoteMatch_Send",
                "Game_PresenceUnknown",
                "Game_PresenceConnected",
                "Game_PresenceDisconnected",
                "Game_LeaveMatchButton",
                "Game_LeaveConfirmationTitle",
                "Game_LeaveConfirmationMessage",
                "Game_ContinueMatchButton",
                "AbandonMatch_ServerConnectionFailedTitle",
                "AbandonMatch_ServerConnectionFailedMessage",
                "MatchHistory_AbandonmentReason",
                "MatchHistory_BackButton",
                "Game_VictoryTitle",
                "Game_VictoryMessage",
                "Game_MatchResultTitle",
                "Game_DrawResultMessage",
                "Game_InactivityResultMessage"
            })
            {
                string? localizedValue = resources.GetString(
                    key,
                    CultureInfo.GetCultureInfo(culture));
                TestAssert.AreEqual(false, string.IsNullOrWhiteSpace(localizedValue));
            }
        }
        TestAssert.AreEqual("JoinMatch_MatchCodeNotAvailableMessage",
            RemoteMatchMessages.Key(MatchStatus.MatchCodeUnavailable));
        TestAssert.AreEqual("RemoteMatch_ConnectionFailed",
            RemoteMatchMessages.Key(MatchStatus.ServiceUnavailable));
    }

    internal static void Callbacks_ConclusionSnapshot_PreservesWinnerAndAbandonmentReason()
    {
        using RemoteMatchClient client = Create("unused");
        client.OnMatchStateChanged(new Proxy.MatchStateSnapshot
        {
            MatchId = 15,
            MatchStateVersion = 4,
            State = Proxy.MatchState.Finished,
            Board = Array.Empty<Proxy.BoardCellState>(),
            BlackPieceCount = 12,
            WhitePieceCount = 18,
            CurrentTurnSeatNumber = null,
            Conclusion = new Proxy.MatchConclusion
            {
                MatchId = 15,
                MatchStateVersion = 4,
                Reason = Proxy.MatchEndReason.Abandonment,
                WinnerSeatNumber = 2,
                AbandoningSeatNumber = 1,
                BlackPieceCount = 12,
                WhitePieceCount = 18,
                EndedAtUtc = new DateTime(2026, 11, 13, 14, 30, 0, DateTimeKind.Utc)
            }
        });

        MatchStateSnapshot snapshot = client.ReadMatchState(15);
        TestAssert.AreEqual(ConfirmedMatchState.Finished, snapshot.State);
        RemoteMatchConclusion conclusion = snapshot.Conclusion
            ?? throw new InvalidOperationException("Expected confirmed match conclusion.");
        TestAssert.AreEqual(RemoteMatchEndReason.Abandonment, conclusion.Reason);
        TestAssert.AreEqual(2, conclusion.WinnerSeatNumber);
    }

    internal static RemoteMatchClient Create(string token) => new(token, new MatchEndpoints
    {
        Lobby = "net.tcp://localhost:18095/ReversiReview/MatchLobby",
        Gameplay = "net.tcp://localhost:18096/ReversiReview/GameplayMatch",
        Chat = "net.tcp://localhost:18097/ReversiReview/MatchChat"
    }, NullLogger<RemoteMatchClient>.Instance);
}
