using System;
using System.Linq;
using System.IO;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Persistence.Chat;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Domain.Matches;
using ReversiGameSystem.Server.Persistence.Matches;
using ReversiGameSystem.Server.Persistence.Model;
using ReversiGameSystem.Server.ServiceApi;
using ReversiGameSystem.Server.Tests.IntegrationTests.Services;
using WireGameplay = ReversiGameSystem.Server.Contracts.Services.IGameplayMatchService;
using WireCallback = ReversiGameSystem.Server.Contracts.Callbacks.IGameplayMatchServiceCallback;
using WireState = ReversiGameSystem.Server.Contracts.Matches.MatchStateSnapshot;
using MatchParticipant = ReversiGameSystem.Server.Application.Matches.MatchParticipant;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
[TestCategory("Database")]
public sealed class GuestMatchIntegrationTests : RepositoryIntegrationTestBase
{
    private const int CreatorSeat = 1;
    private const int OpponentSeat = 2;
    private static LocalDbTestDatabase? _database;
    private TestUtcClock _clock = null!;
    private SessionStore _sessions = null!;
    private MatchRepository _repository = null!;
    private MatchApplicationService _lobby = null!;
    private GameplayMatchApplicationService _gameplay = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        _database = InitializeDatabase();
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        CleanupDatabase(_database);
    }

    [TestInitialize]
    public void InitializeTest()
    {
        _database!.ResetMutableTables();
        _clock = new TestUtcClock();
        _sessions = new SessionStore(_clock);
        _repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
        _lobby = new MatchApplicationService(_sessions, _repository, _clock);
        _gameplay = new GameplayMatchApplicationService(_sessions, _repository, _clock);
        _lobby.MatchStarted += _gameplay.TrackStartedMatch;
    }

    [TestMethod]
    public void CreateMatch_GuestSession_DoesNotCreateAccount()
    {
        GuestSession guest = AddGuest("Creator");

        CreateLobby(guest.SessionToken);

        using ReversiGameSystemEntities context = CreateFreshContext();
        Assert.AreEqual(0, context.Account.Count());
    }

    [TestMethod]
    public void CreateMatch_GuestSession_PersistsGuestWithoutAccountIdentifier()
    {
        GuestSession guest = AddGuest("Creator");

        Match match = CreateLobby(guest.SessionToken);

        using ReversiGameSystemEntities context = CreateFreshContext();
        ReversiGameSystem.Server.Persistence.Model.MatchParticipant participant =
            context.MatchParticipant.Single(row => row.matchId == match.Id);
        Assert.AreEqual("GUEST", participant.participantTypeCode);
        Assert.IsNull(participant.accountId);
    }

    [TestMethod]
    public void PlacePiece_TwoGuestSessions_AssignsDifferentTurns()
    {
        GuestSession creator = AddGuest("Creator");
        GuestSession opponent = AddGuest("Opponent");
        Match match = StartMatch(creator.SessionToken, opponent.SessionToken);
        PlayLegalMove(creator.SessionToken, match.Id);

        MatchStatus status = PlayLegalMove(opponent.SessionToken, match.Id);

        Assert.AreEqual(MatchStatus.Success, status);
        Assert.AreEqual(OpponentSeat,
            _gameplay.GetAuthorizedParticipant(opponent.SessionToken, match.Id).Value!.SeatNumber);
    }

    [TestMethod]
    public void GetMatchState_UnrelatedGuest_CannotReadAnotherMatch()
    {
        GuestSession creator = AddGuest("Creator");
        GuestSession opponent = AddGuest("Opponent");
        GuestSession outsider = AddGuest("Outsider");
        Match match = StartMatch(creator.SessionToken, opponent.SessionToken);

        MatchResult<Match> state = _gameplay.GetMatchState(outsider.SessionToken, match.Id);

        Assert.AreEqual(MatchStatus.Unauthorized, state.Status);
    }

    [TestMethod]
    public void GetMatchState_ReusedGuestName_DoesNotInheritOldSeat()
    {
        GuestSession creator = AddGuest("Creator");
        GuestSession opponent = AddGuest("Opponent");
        Match match = StartMatch(creator.SessionToken, opponent.SessionToken);
        _sessions.RevokeSession(creator.SessionToken);
        GuestSession replacement = AddGuest("Creator");

        MatchResult<Match> state = _gameplay.GetMatchState(replacement.SessionToken, match.Id);

        Assert.AreEqual(MatchStatus.Unauthorized, state.Status);
    }

    [TestMethod]
    public void GetAuthorizedParticipant_ExistingGuestSession_RetainsSeatAfterMoves()
    {
        GuestSession creator = AddGuest("Creator");
        GuestSession opponent = AddGuest("Opponent");
        Match match = StartMatch(creator.SessionToken, opponent.SessionToken);
        PlayLegalMove(creator.SessionToken, match.Id);

        MatchParticipant participant = _gameplay.GetAuthorizedParticipant(
            creator.SessionToken, match.Id).Value!;

        Assert.AreEqual(CreatorSeat, participant.SeatNumber);
    }

    [TestMethod]
    public void ForfeitDisconnectedParticipant_GuestOpponent_AwardsCreatorVictory()
    {
        GuestSession creator = AddGuest("Creator");
        GuestSession opponent = AddGuest("Opponent");
        Match match = StartMatch(creator.SessionToken, opponent.SessionToken);

        Match finished = _gameplay.ForfeitDisconnectedParticipant(match.Id, OpponentSeat).Value!;

        Assert.AreEqual(CreatorSeat, finished.WinnerSeatNumber);
    }

    [TestMethod]
    public void GuestParticipantMigration_UnfilteredIndex_AllowsGuestAndSystem()
    {
        GuestSession creator = AddGuest("Creator");
        Match waiting = CreateLobby(creator.SessionToken);
        using (ReversiGameSystemEntities context = CreateFreshContext())
        {
            context.Database.ExecuteSqlCommand(
                "DROP INDEX UQ_MatchParticipant_AccountPerMatch ON dbo.MatchParticipant; "
                + "CREATE UNIQUE INDEX UQ_MatchParticipant_AccountPerMatch "
                + "ON dbo.MatchParticipant (matchId, accountId);");
            string migrationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "migrations", "20261127_GuestMatchParticipants.sql");
            string migration = File.ReadAllText(migrationPath);
            context.Database.ExecuteSqlCommand(migration);
            context.Database.ExecuteSqlCommand(migration);
        }

        MatchResult<Match> result = _lobby.StartSystemMatch(creator.SessionToken, waiting.Id);

        Assert.AreEqual(MatchStatus.Success, result.Status);
    }

    [TestMethod]
    public void StartSystemMatch_GuestCreator_AllowsSystemOpponent()
    {
        GuestSession creator = AddGuest("Creator");
        Match waiting = CreateLobby(creator.SessionToken);

        Match started = _lobby.StartSystemMatch(creator.SessionToken, waiting.Id).Value!;

        Assert.IsTrue(started.Participants.Single(player => player.SeatNumber == OpponentSeat).IsSystem);
        Assert.IsTrue(started.Participants.Single(player => player.SeatNumber == CreatorSeat).IsGuest);
    }

    [TestMethod]
    public void ChangeTurnDuration_GuestCreator_SavesConfiguration()
    {
        const int TurnDuration = 60;
        GuestSession creator = AddGuest("Creator");
        Match waiting = CreateLobby(creator.SessionToken);

        _lobby.ChangeTurnDuration(creator.SessionToken, waiting.Id, TurnDuration);

        using ReversiGameSystemEntities context = CreateFreshContext();
        Assert.AreEqual(TurnDuration, context.GameMatch.Single().turnDurationSeconds);
    }

    [TestMethod]
    public void CancelMatch_UnrelatedGuest_CannotCancelAnotherLobby()
    {
        GuestSession creator = AddGuest("Creator");
        GuestSession outsider = AddGuest("Outsider");
        Match waiting = CreateLobby(creator.SessionToken);

        MatchStatus status = _lobby.CancelMatch(outsider.SessionToken, waiting.Id);

        Assert.AreEqual(MatchStatus.Unauthorized, status);
    }

    [TestMethod]
    public void GetAvailableMatches_GuestSession_ExcludesOwnLobby()
    {
        GuestSession creator = AddGuest("Creator");
        GuestSession other = AddGuest("Other");
        Match waiting = CreateLobby(creator.SessionToken);

        Match[] available = _lobby.GetAvailableMatches(other.SessionToken).Value!;

        Assert.AreEqual(waiting.Id, available.Single().Id);
        Assert.AreEqual(0, _lobby.GetAvailableMatches(creator.SessionToken).Value!.Length);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void JoinMatch_GuestAndRegisteredPlayer_DoesNotCountForStatistics(bool guestCreates)
    {
        const string Username = "RegisteredPlayer";
        long accountId = SeedAccount(Username, "registered@example.test");
        AuthenticatedSession account = new AuthenticatedSession("registered-session", accountId,
            Username, "registered@example.test", "en-US", _clock.UtcNow.AddHours(1));
        _sessions.TryAddAuthenticated(account);
        GuestSession guest = AddGuest("GuestPlayer");
        string creator = guestCreates ? guest.SessionToken : account.SessionToken;
        string opponent = guestCreates ? account.SessionToken : guest.SessionToken;

        Match match = StartMatch(creator, opponent);

        Assert.IsFalse(match.CountsTowardStatistics);
    }

    [TestMethod]
    public void ObserveMatch_GuestParticipant_IsRejected()
    {
        GuestSession creator = AddGuest("Creator");
        GuestSession opponent = AddGuest("Opponent");
        Match match = StartMatch(creator.SessionToken, opponent.SessionToken);

        MatchStatus status = _gameplay.ObserveMatch(creator.SessionToken, match.Id).Status;

        Assert.AreEqual(MatchStatus.Unauthorized, status);
    }

    [TestMethod]
    public void CancelExpiredWaitingSessions_ActiveGuest_KeepsLobby()
    {
        GuestSession creator = AddGuest("Creator");
        CreateLobby(creator.SessionToken);

        long[] cancelled = _lobby.CancelExpiredWaitingSessions();

        Assert.AreEqual(0, cancelled.Length);
    }

    [TestMethod]
    public void ForfeitMatch_GuestCreator_PersistsWinnerBySeat()
    {
        GuestSession creator = AddGuest("Creator");
        GuestSession opponent = AddGuest("Opponent");
        Match match = StartMatch(creator.SessionToken, opponent.SessionToken);
        _gameplay.ForfeitMatch(creator.SessionToken, match.Id);

        _gameplay.SynchronizePendingMatches();

        using ReversiGameSystemEntities context = CreateFreshContext();
        string result = context.MatchParticipant.Single(participant => participant.matchId == match.Id
            && participant.seatNumber == OpponentSeat).resultCode;
        Assert.AreEqual("WIN", result);
    }

    [TestMethod]
    public void SynchronizePendingMatches_TwoGuests_PersistsMovesWithoutAccounts()
    {
        GuestSession creator = AddGuest("Creator");
        GuestSession opponent = AddGuest("Opponent");
        Match match = StartMatch(creator.SessionToken, opponent.SessionToken);
        PlayLegalMove(creator.SessionToken, match.Id);
        PlayLegalMove(opponent.SessionToken, match.Id);

        _gameplay.SynchronizePendingMatches();

        using ReversiGameSystemEntities context = CreateFreshContext();
        Assert.AreEqual(2, context.GameMove.Count(move => move.matchId == match.Id));
    }

    [TestMethod]
    public void OpenMatchChannel_GuestReconnect_RestoresSeatAndConfirmedBoard()
    {
        GuestSession creator = AddGuest("Creator");
        GuestSession opponent = AddGuest("Opponent");
        Match match = StartMatch(creator.SessionToken, opponent.SessionToken);
        GameplayMatchPresence presence = new GameplayMatchPresence();
        MatchCallbackSubscriptions<WireCallback> subscriptions = null!;
        subscriptions = new MatchCallbackSubscriptions<WireCallback>(
            (token, matchId) => _gameplay.GetAuthorizedParticipant(token, matchId).Status
                == MatchStatus.Success,
            (token, matchId) => presence.RemoveConnection(token, matchId,
                subscriptions.HasSubscription), NullLogger.Instance);
        using (subscriptions)
        {
            GameplayMatchService service = new GameplayMatchService(_gameplay, presence, subscriptions);
            using WcfChannelScope<WireGameplay> scope =
                WcfTestHostHelper.OpenDuplexChannel<WireGameplay, WireCallback>(
                    service, "GuestGameplay", new GuestGameplayCallback());
            scope.Channel.OpenMatchChannel(creator.SessionToken, match.Id);
            PlayLegalMove(creator.SessionToken, match.Id);
            ((ICommunicationObject)scope.Channel).Abort();
            WireGameplay replacement = scope.Factory.CreateChannel();
            try
            {
                replacement.OpenMatchChannel(creator.SessionToken, match.Id);
                int? seat = replacement.GetMatchInformation(creator.SessionToken, match.Id)
                    .AssignedSeatNumber;
                WireState state = replacement.GetMatchState(creator.SessionToken, match.Id);

                Assert.AreEqual(CreatorSeat, seat);
                Assert.AreEqual(1L, state.MatchStateVersion);
            }
            finally
            {
                ((ICommunicationObject)replacement).Abort();
            }
        }
    }

    [CallbackBehavior(UseSynchronizationContext = false)]
    private sealed class GuestGameplayCallback : WireCallback
    {
        public void OnMatchStateChanged(WireState state)
        {
        }

        public void OnParticipantConnectionChanged(long matchId, int seatNumber, bool isConnected)
        {
        }
    }

    [TestMethod]
    public void EvaluateChatAccess_RegisteredPlayerAgainstGuest_IsRejected()
    {
        const string Username = "ChatPlayer";
        long accountId = SeedAccount(Username, "chatplayer@example.test");
        AuthenticatedSession account = new AuthenticatedSession("chat-session", accountId,
            Username, "chatplayer@example.test", "en-US", _clock.UtcNow.AddHours(1));
        _sessions.TryAddAuthenticated(account);
        GuestSession guest = AddGuest("GuestPlayer");
        Match match = StartMatch(account.SessionToken, guest.SessionToken);
        ChatRepository chat = new ChatRepository(_clock);

        ChatStatus status = chat.EvaluateParticipantAccess(accountId, match.Id);

        Assert.AreEqual(ChatStatus.Unauthorized, status);
    }

    private GuestSession AddGuest(string name)
    {
        GuestSession guest = new GuestSession(Guid.NewGuid().ToString("N"), name,
            _clock.UtcNow.AddHours(1));
        _sessions.TryReserveGuest(guest);
        return guest;
    }

    private Match CreateLobby(string token)
    {
        return _lobby.CreateMatch(token, new MatchSettings()).Value
            ?? throw new InvalidOperationException("The guest lobby could not be created.");
    }

    private Match StartMatch(string creator, string opponent)
    {
        Match waiting = CreateLobby(creator);
        return _lobby.JoinMatch(opponent, waiting.Code!).Value
            ?? throw new InvalidOperationException("The opponent could not join the lobby.");
    }

    private MatchStatus PlayLegalMove(string token, long matchId)
    {
        Match state = _gameplay.GetMatchState(token, matchId).Value!;
        int seat = _gameplay.GetAuthorizedParticipant(token, matchId).Value!.SeatNumber;
        for (int row = 0; row < ReversiEngine.BoardSize; row++)
        {
            for (int column = 0; column < ReversiEngine.BoardSize; column++)
            {
                if (ReversiEngine.IsLegalMove(state.Board, row, column, seat))
                {
                    return _gameplay.PlacePiece(token, new MatchPlacement(
                        matchId, state.MatchStateVersion, row, column)).Status;
                }
            }
        }
        throw new InvalidOperationException("The participant has no legal move.");
    }
}
