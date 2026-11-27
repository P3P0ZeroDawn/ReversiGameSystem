using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Client.Tests.TestDoubles;
using ReversiGameSystem.Features.Matches;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class MatchHistoryTests
{
    private const long MatchId = 19;
    private const int RecordedColumn = 7;

    [TestMethod]
    public async Task LoadHistory_AndRecording_DoNotSubscribeToGameplay()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();

        await service.Gameplay.LoadMatchHistory();
        await service.Gameplay.LoadMatchDetails(MatchId);

        Assert.AreEqual(0, service.OpenCalls + service.ObserveCalls);
    }

    [TestMethod]
    public async Task LoadMatchHistory_ExpectedFault_ReturnsDomainStatus()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        service.HistoryFailure = Proxy.MatchFailure.InactiveAccount;

        MatchStatus status = await service.Gameplay.LoadMatchHistory();

        Assert.AreEqual(MatchStatus.InactiveAccount, status);
    }

    [TestMethod]
    public async Task LoadMatchDetails_TechnicalFault_PropagatesToErrorBoundary()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        service.DetailsTechnicalFailure = true;

        await Assert.ThrowsExactlyAsync<FaultException<Proxy.ServiceFault>>(
            () => service.Gameplay.LoadMatchDetails(MatchId));
    }

    [TestMethod]
    public async Task LoadMatchDetails_IncompleteFrames_DoesNotInventBoards()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        service.Recording.BoardFrames = Array.Empty<Proxy.BoardCellState[]>();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.Gameplay.LoadMatchDetails(MatchId));
    }

    [TestMethod]
    public async Task LoadHistory_Guest_DoesNotQueryServer()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        ClientSession session = new ClientSession();
        session.StartGuest(new GuestAccountSession("guest-session", "Guest",
            DateTime.UtcNow.AddHours(1)));
        MatchHistoryViewModel model = new MatchHistoryViewModel(session, service.Gameplay);

        await model.LoadHistory();

        Assert.AreEqual(0, service.HistoryCalls);
        model.Deactivate();
    }

    [TestMethod]
    public async Task LoadHistory_SessionChangesDuringRequest_CancelsQuery()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        ClientSession session = ObservableGameplayServiceStub.CreateSession();
        service.BeforeHistoryResponse = session.Clear;
        MatchHistoryViewModel model = new MatchHistoryViewModel(session, service.Gameplay);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => model.LoadHistory());
        model.Deactivate();
    }

    [TestMethod]
    public async Task LoadRecording_SessionChangesDuringRequest_RejectsNavigation()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        ClientSession session = ObservableGameplayServiceStub.CreateSession();
        MatchHistoryViewModel model = new MatchHistoryViewModel(session, service.Gameplay);
        await model.LoadHistory();
        service.BeforeDetailsResponse = session.Clear;

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => model.LoadRecording(MatchId));
        model.Deactivate();
    }

    [TestMethod]
    public async Task History_SessionEnds_RemovesConfirmedRows()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        ClientSession session = ObservableGameplayServiceStub.CreateSession();
        MatchHistoryViewModel model = new MatchHistoryViewModel(session, service.Gameplay);
        await model.LoadHistory();

        session.Clear();

        Assert.AreEqual(0, model.Matches.Length);
    }

    [TestMethod]
    public async Task Replay_NextFrame_UsesServerBoardWithoutRecalculatingMove()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        await service.Gameplay.LoadMatchDetails(MatchId);
        MatchReplayViewModel model = new MatchReplayViewModel(
            service.Gameplay.ReadMatchDetails(MatchId));

        model.NextFrame();

        Assert.AreEqual(DiscColor.White, model.ReadBoard()[0, RecordedColumn]);
    }

    [TestMethod]
    public async Task Replay_PreviousFrame_DoesNotPassInitialBoard()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        await service.Gameplay.LoadMatchDetails(MatchId);
        MatchReplayViewModel model = new MatchReplayViewModel(
            service.Gameplay.ReadMatchDetails(MatchId));

        model.PreviousFrame();

        Assert.AreEqual(0, model.FrameIndex);
    }

    [TestMethod]
    public async Task Replay_FinalFrame_StopsAutomaticPlayback()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        await service.Gameplay.LoadMatchDetails(MatchId);
        MatchReplayViewModel model = new MatchReplayViewModel(
            service.Gameplay.ReadMatchDetails(MatchId));
        model.TogglePlayback();

        model.NextFrame();

        Assert.IsFalse(model.IsPlaying);
    }

    [TestMethod]
    public async Task Replay_NoMoves_DoesNotStartAutomaticPlayback()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        service.Recording.Moves = Array.Empty<Proxy.MoveRecord>();
        service.Recording.BoardFrames = new[] { service.Recording.BoardFrames[0] };
        await service.Gameplay.LoadMatchDetails(MatchId);
        MatchReplayViewModel model = new MatchReplayViewModel(
            service.Gameplay.ReadMatchDetails(MatchId));

        model.TogglePlayback();

        Assert.IsFalse(model.IsPlaying);
    }

    [TestMethod]
    public async Task History_EmptyResponse_ShowsNoPrototypeMatches()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        service.History = Array.Empty<Proxy.MatchSummary>();
        MatchHistoryViewModel model = new MatchHistoryViewModel(
            ObservableGameplayServiceStub.CreateSession(), service.Gameplay);

        await model.LoadHistory();

        Assert.AreEqual(0, model.Matches.Length);
        model.Deactivate();
    }

    [TestMethod]
    public async Task Replay_PlayAfterFinalFrame_StartsFromInitialBoard()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        await service.Gameplay.LoadMatchDetails(MatchId);
        MatchReplayViewModel model = new MatchReplayViewModel(
            service.Gameplay.ReadMatchDetails(MatchId));
        model.NextFrame();

        model.TogglePlayback();

        Assert.AreEqual(0, model.FrameIndex);
    }

    [TestMethod]
    public async Task Replay_SystemParticipant_UsesSelectedLanguage()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        service.Recording.Participants[1].Kind = Proxy.ParticipantKind.System;
        await service.Gameplay.LoadMatchDetails(MatchId);
        MatchReplayViewModel model = new MatchReplayViewModel(
            service.Gameplay.ReadMatchDetails(MatchId));

        string name = model.GetPlayerName(2);

        Assert.AreEqual(LocalizationManager.Instance["Game_SystemPlayerName"], name);
    }

    [TestMethod]
    public async Task History_WinnerUsesSeat_InsteadOfDisplayName()
    {
        using ObservableGameplayServiceStub service = new ObservableGameplayServiceStub();
        service.History[0].Participants[1].DisplayName =
            service.History[0].Participants[0].DisplayName;
        service.History[0].Conclusion!.WinnerSeatNumber = 2;
        MatchHistoryViewModel model = new MatchHistoryViewModel(
            ObservableGameplayServiceStub.CreateSession(), service.Gameplay);

        await model.LoadHistory();

        Assert.AreEqual(LocalizationManager.Instance["MatchHistory_DefeatResult"],
            model.Matches[0].ResultText);
        model.Deactivate();
    }
}
