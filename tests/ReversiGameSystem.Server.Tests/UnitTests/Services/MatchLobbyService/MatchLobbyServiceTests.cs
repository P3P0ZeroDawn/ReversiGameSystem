using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.MatchLobbyService;

[TestClass]
[TestCategory("Unit")]
public sealed class MatchLobbyServiceTests : MatchLobbyServiceTestBase
{
    [TestMethod]
    public void Constructor_NullApplicationService_ThrowsArgumentNullException()
    {
        Action action = () => _ = new Server.ServiceApi.MatchLobbyService(null!, Subscriptions);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullCallbackSubscriptions_ThrowsArgumentNullException()
    {
        Action action = () => _ = new Server.ServiceApi.MatchLobbyService(
            ApplicationService,
            null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    [DataRow(null, SessionFailure.MissingSessionToken)]
    [DataRow("invalid-token", SessionFailure.InvalidSession)]
    public void OpenLobbyChannel_RejectedSession_ThrowsSessionFault(
        string? sessionToken,
        SessionFailure expectedFailure)
    {
        Action action = () => Service.OpenLobbyChannel(sessionToken!);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }

    [TestMethod]
    public void CreateMatch_NullConfiguration_ThrowsInvalidConfigurationFault()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        MatchFailure expectedFailure = MatchFailure.InvalidConfiguration;
        Action action = () => Service.CreateMatch(token, null!);

        FaultException<MatchFault> exception =
            Assert.Throws<FaultException<MatchFault>>(action);
        MatchFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }

    [TestMethod]
    public void CreateMatch_ValidConfiguration_ReturnsMappedLobbySnapshot()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        long expectedMatchId = 44L;
        string expectedCode = "ABC234";
        Matches.CreateMatchResult = new MatchResult<Match>(new Match
        {
            Id = expectedMatchId,
            Code = expectedCode,
            State = "Waiting",
            TurnDurationSeconds = 40
        });
        MatchConfiguration configuration = new MatchConfiguration
        {
            OpponentKind = OpponentKind.Human,
            TurnDurationSeconds = 40,
            CountsTowardStatistics = false
        };

        LobbySnapshot actualSnapshot = Service.CreateMatch(token, configuration);
        long actualMatchId = actualSnapshot.MatchId;
        string? actualCode = actualSnapshot.MatchCode;

        Assert.AreEqual(expectedMatchId, actualMatchId);
        Assert.AreEqual(expectedCode, actualCode);
    }

    [TestMethod]
    public void InviteToMatch_FriendshipNotFound_ThrowsFriendshipFault()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        Matches.PreparePrivateInvitationResult =
            new MatchResult<MatchInvitationPreparation>(MatchStatus.FriendshipNotFound);
        FriendshipFailure expectedFailure = FriendshipFailure.FriendshipNotFound;
        MatchConfiguration configuration = new MatchConfiguration
        {
            OpponentKind = OpponentKind.Human,
            TurnDurationSeconds = 40
        };
        Action action = () => Service.InviteToMatch(token, 20L, configuration);

        FaultException<FriendshipFault> exception =
            Assert.Throws<FaultException<FriendshipFault>>(action);
        FriendshipFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }

    [TestMethod]
    public void GetAvailableMatches_ValidSession_ReturnsMappedAvailableMatches()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        string expectedCode = "XYZ789";
        Matches.AvailableMatchesResult = new MatchResult<Match[]>(new Match[]
        {
            new Match
            {
                Id = 55L,
                Code = expectedCode,
                TurnDurationSeconds = 30,
                Participants = new MatchParticipant[]
                {
                    new MatchParticipant
                    {
                        SeatNumber = 1,
                        AccountId = 20L,
                        DisplayName = "HostPlayer"
                    }
                }
            }
        });
        int expectedCount = 1;

        AvailableMatch[] actualMatches = Service.GetAvailableMatches(token);
        int actualCount = actualMatches.Length;
        string actualCode = actualMatches[0].MatchCode;

        Assert.AreEqual(expectedCount, actualCount);
        Assert.AreEqual(expectedCode, actualCode);
    }

    [TestMethod]
    public void JoinMatch_InvalidMatchCode_ThrowsMatchFault()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        MatchFailure expectedFailure = MatchFailure.InvalidMatchCode;
        Action action = () => Service.JoinMatch(token, "bad");

        FaultException<MatchFault> exception =
            Assert.Throws<FaultException<MatchFault>>(action);
        MatchFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }

    [TestMethod]
    public void StartSystemMatch_ValidSession_ReturnsMappedStateSnapshot()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        long expectedMatchId = 66L;
        Matches.StartSystemMatchResult = new MatchResult<Match>(new Match
        {
            Id = expectedMatchId,
            State = "InProgress"
        });
        MatchState expectedState = MatchState.InProgress;

        MatchStateSnapshot actualSnapshot = Service.StartSystemMatch(token, expectedMatchId);
        long actualMatchId = actualSnapshot.MatchId;
        MatchState actualState = actualSnapshot.State;

        Assert.AreEqual(expectedMatchId, actualMatchId);
        Assert.AreEqual(expectedState, actualState);
    }

    [TestMethod]
    public void CancelMatch_ValidMatch_DelegatesToApplicationService()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        long expectedMatchId = 77L;
        int expectedCalls = 1;

        Service.CancelMatch(token, expectedMatchId);
        int actualCalls = Matches.CancelMatchCalls;
        long actualMatchId = Matches.LastMatchId;

        Assert.AreEqual(expectedCalls, actualCalls);
        Assert.AreEqual(expectedMatchId, actualMatchId);
    }

    [TestMethod]
    public void ChangeTurnDuration_InvalidDuration_ThrowsInvalidTurnDurationFault()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        MatchFailure expectedFailure = MatchFailure.InvalidTurnDuration;
        Action action = () => Service.ChangeTurnDuration(token, 50L, 5);

        FaultException<MatchFault> exception =
            Assert.Throws<FaultException<MatchFault>>(action);
        MatchFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }

    [TestMethod]
    public void ChangeTurnDuration_ValidDuration_ReturnsUpdatedLobbySnapshot()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        long expectedMatchId = 50L;
        int expectedDuration = 90;
        Matches.ChangeTurnDurationResult = new MatchResult<Match>(new Match
        {
            Id = expectedMatchId,
            Code = "ABC234",
            State = "Waiting",
            TurnDurationSeconds = expectedDuration
        });

        LobbySnapshot actualSnapshot =
            Service.ChangeTurnDuration(token, expectedMatchId, expectedDuration);
        int actualDuration = actualSnapshot.Configuration.TurnDurationSeconds;

        Assert.AreEqual(expectedDuration, actualDuration);
    }
}
