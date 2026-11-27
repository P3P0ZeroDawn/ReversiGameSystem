using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MimeKit;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Matches;
using AppMatchService = ReversiGameSystem.Server.Application.Matches.MatchApplicationService;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Persistence.Mail;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application;

/// <summary>
/// Unit test suite for private match invitations across application and mail layers.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class MatchInvitationTests
{
    private const string ExpectedMandatoryLegend =
        "«Puedes unirte con este código mientras el creador siga esperando»";
    private const string AlphaUsername = "AlphaPlayer";
    private const string BetaUsername = "BetaPlayer";
    private const string BetaEmail = "beta@example.com";
    private const string GammaEmail = "gamma@example.com";
    private const string WaitingState = "Waiting";
    private const string InProgressState = "InProgress";

    [TestMethod]
    public void Invite_ConnectedFriend_DeliversEmailAndReturnsWaitingSnapshot()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        _ = CreatePlayer(context, 20, BetaUsername);
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, BetaEmail);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        int expectedEmailCount = 1;

        Match result = TestAssert.IsNotNull(handler.InviteToMatch(
            creator.SessionToken,
            20,
            new MatchSettings { TurnDurationSeconds = 40 }).Value);

        Assert.AreEqual(WaitingState, result.State);
        Assert.IsTrue(result.IsPrivate);
        Assert.AreEqual(expectedEmailCount, mailer.SentEmails.Count);
        EmailMessage sent = mailer.SentEmails[0];
        Assert.AreEqual(BetaEmail, sent.RecipientAddress);
        Assert.IsTrue(
            sent.Body.Contains(AlphaUsername),
            "Email body should contain inviter username.");
        Assert.IsTrue(
            sent.Body.Contains(result.Code ?? string.Empty),
            "Email body should contain match code.");
    }

    [TestMethod]
    public void Invite_DisconnectedFriend_DeliversEmailAndReturnsWaitingSnapshot()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 30, GammaEmail);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        int expectedEmailCount = 1;

        Match result = TestAssert.IsNotNull(handler.InviteToMatch(
            creator.SessionToken,
            30,
            new MatchSettings { TurnDurationSeconds = 60 }).Value);

        Assert.AreEqual(WaitingState, result.State);
        Assert.IsTrue(result.IsPrivate);
        Assert.AreEqual(expectedEmailCount, mailer.SentEmails.Count);
        Assert.AreEqual(GammaEmail, mailer.SentEmails[0].RecipientAddress);
    }

    [TestMethod]
    public void Invite_GuestSession_ThrowsUnauthorizedRejection()
    {
        SessionTestContext context = new SessionTestContext();
        GuestSession guest = TestAssert.IsNotNull(
            context.ApplicationService.OpenGuestSession("GuestAlpha").Value);
        InvitationMockStore store = new InvitationMockStore();
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        MatchStatus expectedStatus = MatchStatus.Unauthorized;
        int expectedEmailCount = 0;

        MatchResult<Match> result = handler.InviteToMatch(
            guest.SessionToken,
            20,
            new MatchSettings());

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.AreEqual(expectedEmailCount, mailer.SentEmails.Count);
    }

    [TestMethod]
    public void Invite_SelfInvitation_ThrowsUnauthorizedRejection()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        InvitationMockStore store = new InvitationMockStore();
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        MatchStatus expectedStatus = MatchStatus.Unauthorized;
        int expectedEmailCount = 0;

        MatchResult<Match> result = handler.InviteToMatch(
            creator.SessionToken,
            10,
            new MatchSettings());

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.AreEqual(expectedEmailCount, mailer.SentEmails.Count);
    }

    [TestMethod]
    public void Invite_NegativeOrZeroFriendAccountId_ThrowsInvalidAccountIdRejection()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        InvitationMockStore store = new InvitationMockStore();
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        MatchStatus expectedStatus = MatchStatus.InvalidFriendAccountId;

        MatchStatus zeroStatus = handler.InviteToMatch(
            creator.SessionToken,
            0,
            new MatchSettings()).Status;
        MatchStatus negativeStatus = handler.InviteToMatch(
            creator.SessionToken,
            -5,
            new MatchSettings()).Status;

        Assert.AreEqual(expectedStatus, zeroStatus);
        Assert.AreEqual(expectedStatus, negativeStatus);
    }

    [TestMethod]
    public void Invite_AccountUnavailable_ThrowsAccountUnavailableRejection()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        InvitationMockStore store = new InvitationMockStore();
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        MatchStatus expectedStatus = MatchStatus.FriendAccountUnavailable;
        int expectedEmailCount = 0;

        MatchResult<Match> result = handler.InviteToMatch(
            creator.SessionToken,
            999,
            new MatchSettings());

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.AreEqual(expectedEmailCount, mailer.SentEmails.Count);
    }

    [TestMethod]
    public void Invite_FriendshipNotFound_ThrowsFriendshipNotFoundRejection()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterAccount(50, "inactive@example.com", true);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        MatchStatus expectedStatus = MatchStatus.FriendshipNotFound;
        int expectedEmailCount = 0;

        MatchResult<Match> result = handler.InviteToMatch(
            creator.SessionToken,
            50,
            new MatchSettings());

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.AreEqual(expectedEmailCount, mailer.SentEmails.Count);
    }

    [TestMethod]
    public void Invite_InactiveCaller_ThrowsInactiveAccountRejection()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession inactiveCaller = TestSessionHelper.CreateSession(
            context.Sessions,
            10,
            "InactiveAlpha");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, BetaEmail);
        store.RegisterAccount(10, "alpha@example.com", false);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        MatchStatus expectedStatus = MatchStatus.InactiveAccount;
        int expectedEmailCount = 0;

        MatchResult<Match> result = handler.InviteToMatch(
            inactiveCaller.SessionToken,
            20,
            new MatchSettings());

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.AreEqual(expectedEmailCount, mailer.SentEmails.Count);
    }

    [TestMethod]
    public void Invite_InvalidTurnDuration_ThrowsInvalidTurnDurationRejection()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        InvitationMockStore store = new InvitationMockStore();
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        MatchStatus expectedStatus = MatchStatus.InvalidTurnDuration;

        MatchStatus belowMin = handler.InviteToMatch(
            creator.SessionToken,
            20,
            new MatchSettings { TurnDurationSeconds = 9 }).Status;
        MatchStatus aboveMax = handler.InviteToMatch(
            creator.SessionToken,
            20,
            new MatchSettings { TurnDurationSeconds = 121 }).Status;

        Assert.AreEqual(expectedStatus, belowMin);
        Assert.AreEqual(expectedStatus, aboveMax);
    }

    [TestMethod]
    public void Invite_InvalidOpponentKind_ThrowsInvalidConfigurationRejection()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        InvitationMockStore store = new InvitationMockStore();
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        MatchStatus expectedStatus = MatchStatus.InvalidConfiguration;

        MatchResult<Match> result = handler.InviteToMatch(
            creator.SessionToken,
            20,
            new MatchSettings { OpponentKind = "System" });

        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void Invite_CountsTowardStatisticsTrue_ThrowsInvalidConfigurationRejection()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        InvitationMockStore store = new InvitationMockStore();
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        MatchStatus expectedStatus = MatchStatus.InvalidConfiguration;

        MatchResult<Match> result = handler.InviteToMatch(
            creator.SessionToken,
            20,
            new MatchSettings { CountsTowardStatistics = true });

        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void Invite_DeliveryFailure_CancelsPreparedMatchAndThrowsException()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, BetaEmail);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender { FailSend = true };
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        int expectedCancelCount = 1;
        int expectedConfirmedCount = 0;

        Assert.Throws<InvalidOperationException>(() =>
            handler.InviteToMatch(creator.SessionToken, 20, new MatchSettings()));

        Assert.AreEqual(expectedCancelCount, store.CancelCount);
        Assert.AreEqual(expectedConfirmedCount, store.ConfirmedCount);
    }

    [TestMethod]
    public void Invite_DeliveryFailureWhenCompensationFails_PreservesUnconfirmedMatch()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        InvitationMockStore store = new InvitationMockStore { FailCancel = true };
        store.RegisterFriendship(10, 20, BetaEmail);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender { FailSend = true };
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        int expectedRowCount = 1;

        Assert.Throws<AggregateException>(() =>
            handler.InviteToMatch(creator.SessionToken, 20, new MatchSettings()));

        Assert.AreEqual(expectedRowCount, store.Rows.Count);
        MockMatchRow row = store.Rows.Values.First();
        Assert.IsFalse(row.IsConfirmed, "Match must not be confirmed.");
    }

    [TestMethod]
    public void Invite_CreatorLeavesBeforeConfirmation_CancelsMatchAndThrowsMatchNotWaiting()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, BetaEmail);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        mailer.BeforeSendCallback = () =>
        {
            context.Sessions.RevokeSession(creator.SessionToken);
        };
        MatchStatus expectedStatus = MatchStatus.MatchNotWaiting;
        int expectedCancelCount = 1;

        MatchResult<Match> result = handler.InviteToMatch(
            creator.SessionToken,
            20,
            new MatchSettings());

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.AreEqual(expectedCancelCount, store.CancelCount);
    }

    [TestMethod]
    public void GetAvailableMatches_PrivateMatch_ExcludesPrivateMatchesFromLobby()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        AuthenticatedSession friend = CreatePlayer(context, 20, BetaUsername);
        AuthenticatedSession stranger = CreatePlayer(context, 99, "StrangerPlayer");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, BetaEmail);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        _ = handler.InviteToMatch(creator.SessionToken, 20, new MatchSettings()).Value;
        AuthenticatedSession otherCreator = CreatePlayer(context, 88, "OtherCreator");
        _ = handler.CreateMatch(otherCreator.SessionToken, new MatchSettings()).Value;
        int expectedCount = 1;

        Match[] forCreator = TestAssert.IsNotNull(
            handler.GetAvailableMatches(creator.SessionToken).Value);
        Match[] forFriend = TestAssert.IsNotNull(
            handler.GetAvailableMatches(friend.SessionToken).Value);
        Match[] forStranger = TestAssert.IsNotNull(
            handler.GetAvailableMatches(stranger.SessionToken).Value);

        Assert.AreEqual(expectedCount, forCreator.Length);
        Assert.IsFalse(forCreator[0].IsPrivate);
        Assert.AreEqual(expectedCount, forFriend.Length);
        Assert.IsFalse(forFriend[0].IsPrivate);
        Assert.AreEqual(expectedCount, forStranger.Length);
        Assert.IsFalse(forStranger[0].IsPrivate);
    }

    [TestMethod]
    public void Join_PrivateMatchForInvitedAccount_SucceedsAndStartsMatch()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        AuthenticatedSession friend = CreatePlayer(context, 20, BetaUsername);
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, BetaEmail);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        Match waiting = TestAssert.IsNotNull(handler.InviteToMatch(
            creator.SessionToken,
            20,
            new MatchSettings()).Value);
        int expectedParticipantCount = 2;
        long expectedSecondAccountId = 20L;

        Match joined = TestAssert.IsNotNull(
            handler.JoinMatch(friend.SessionToken, waiting.Code ?? string.Empty).Value);

        Assert.AreEqual(InProgressState, joined.State);
        Assert.AreEqual(expectedParticipantCount, joined.Participants.Length);
        Assert.AreEqual(expectedSecondAccountId, joined.Participants[1].AccountId);
    }

    [TestMethod]
    public void Join_PrivateMatchForOtherAccount_ThrowsUnauthorizedRejection()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        _ = CreatePlayer(context, 20, BetaUsername);
        AuthenticatedSession stranger = CreatePlayer(context, 99, "StrangerPlayer");
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, BetaEmail);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        Match waiting = TestAssert.IsNotNull(handler.InviteToMatch(
            creator.SessionToken,
            20,
            new MatchSettings()).Value);
        MatchStatus expectedStatus = MatchStatus.Unauthorized;

        MatchResult<Match> result = handler.JoinMatch(
            stranger.SessionToken,
            waiting.Code ?? string.Empty);

        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void Join_PrivateMatchForGuest_ThrowsUnauthorizedRejection()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, BetaEmail);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        Match waiting = TestAssert.IsNotNull(handler.InviteToMatch(
            creator.SessionToken,
            20,
            new MatchSettings()).Value);
        MatchStatus expectedStatus = MatchStatus.Unauthorized;

        MatchStatus status = store.ValidateGuestJoin(waiting.Code ?? string.Empty);

        Assert.AreEqual(expectedStatus, status);
    }

    [TestMethod]
    public void Join_UnconfirmedPrivateMatch_ThrowsMatchCodeUnavailableRejection()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession friend = CreatePlayer(context, 20, BetaUsername);
        InvitationMockStore store = new InvitationMockStore();
        Match unconfirmed = new Match
        {
            Id = 55,
            Code = "AB2324",
            State = WaitingState,
            IsPrivate = true,
            Participants = new[]
            {
                new MatchParticipant
                {
                    AccountId = 10,
                    SeatNumber = 1,
                    DisplayName = "Alpha"
                }
            }
        };
        store.Rows[55] = new MockMatchRow
        {
            Match = unconfirmed,
            InvitedAccountId = 20,
            IsConfirmed = false
        };
        MatchStatus expectedStatus = MatchStatus.MatchCodeUnavailable;

        MatchResult<Match> result = store.JoinMatch(friend, "AB2324", DateTime.UtcNow);

        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void Join_CancelledPrivateMatch_ThrowsMatchCodeUnavailableRejection()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession friend = CreatePlayer(context, 20, BetaUsername);
        InvitationMockStore store = new InvitationMockStore();
        Match cancelled = new Match
        {
            Id = 56,
            Code = "AB2325",
            State = "Cancelled",
            IsPrivate = true,
            Participants = new[]
            {
                new MatchParticipant
                {
                    AccountId = 10,
                    SeatNumber = 1,
                    DisplayName = "Alpha"
                }
            }
        };
        store.Rows[56] = new MockMatchRow
        {
            Match = cancelled,
            InvitedAccountId = 20,
            IsConfirmed = true
        };
        MatchStatus expectedStatus = MatchStatus.MatchCodeUnavailable;

        MatchResult<Match> result = store.JoinMatch(friend, "AB2325", DateTime.UtcNow);

        Assert.AreEqual(expectedStatus, result.Status);
    }

    [TestMethod]
    public void Join_ConcurrentSameInvitedAccount_RegistersOnlyOneSecondSeat()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        AuthenticatedSession friend = CreatePlayer(context, 20, BetaUsername);
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, BetaEmail);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        Match waiting = TestAssert.IsNotNull(handler.InviteToMatch(
            creator.SessionToken,
            20,
            new MatchSettings()).Value);
        int expectedSuccesses = 1;

        Task<bool>[] attempts = Enumerable.Range(0, 3).Select(_ => Task.Run(() =>
        {
            MatchResult<Match> result = handler.JoinMatch(
                friend.SessionToken,
                waiting.Code ?? string.Empty);
            return result.Status == MatchStatus.Success;
        })).ToArray();
        Task.WaitAll(attempts);

        int successes = attempts.Count(task => task.Result);
        Assert.AreEqual(expectedSuccesses, successes);
    }

    [TestMethod]
    public void CancelWaitingForSession_PrivateMatchWaiting_CancelsMatchAndRemovesLobby()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creator = CreatePlayer(context, 10, AlphaUsername);
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, BetaEmail);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        Match waiting = TestAssert.IsNotNull(handler.InviteToMatch(
            creator.SessionToken,
            20,
            new MatchSettings()).Value);
        int expectedCancelledCount = 1;
        int expectedRemainingCount = 0;

        long[] cancelled = handler.CancelWaitingForSession(creator.SessionToken);

        Assert.AreEqual(expectedCancelledCount, cancelled.Length);
        Assert.AreEqual(waiting.Id, cancelled[0]);
        Assert.AreEqual(
            expectedRemainingCount,
            handler.CancelWaitingForSession(creator.SessionToken).Length);
    }

    [TestMethod]
    public void CancelWaitingForSession_DifferentSessionForSameAccount_PreservesIsolation()
    {
        SessionTestContext context = new SessionTestContext();
        AuthenticatedSession creatorSession1 = CreatePlayer(context, 10, AlphaUsername);
        InvitationMockStore store = new InvitationMockStore();
        store.RegisterFriendship(10, 20, BetaEmail);
        InMemoryInvitationSender mailer = new InMemoryInvitationSender();
        AppMatchService handler = new AppMatchService(
            context.Sessions,
            store,
            context,
            mailer);
        Match waiting = TestAssert.IsNotNull(handler.InviteToMatch(
            creatorSession1.SessionToken,
            20,
            new MatchSettings()).Value);
        AuthenticatedSession creatorSession2 = CreatePlayer(context, 10, AlphaUsername);
        int expectedCancelledCount = 1;
        int expectedRemainingCount = 0;

        long[] cancelled = handler.CancelWaitingForSession(creatorSession1.SessionToken);

        Assert.AreEqual(expectedCancelledCount, cancelled.Length);
        Assert.AreEqual(waiting.Id, cancelled[0]);
        Assert.AreEqual(
            expectedRemainingCount,
            handler.CancelWaitingForSession(creatorSession2.SessionToken).Length);
    }

    [TestMethod]
    public void EmailSender_CreateMessage_ContainsMandatoryLegendAndMatchCode()
    {
        WithTestMailSettings(() =>
        {
            MailSettings settings = MailSettings.Load();
            SmtpEmailSender sender = new SmtpEmailSender(settings);
            HtmlEmailTemplateRenderer renderer = new HtmlEmailTemplateRenderer();
            RenderedEmailTemplate rendered = renderer.Render(
                EmailTemplateNames.MatchInvitation,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "Locale", "es-MX" },
                    { "CreatorDisplayName", AlphaUsername },
                    { "MatchCode", "REV999" }
                });
            EmailMessage email = new EmailMessage(
                BetaEmail,
                rendered.Subject,
                rendered.HtmlBody,
                isHtml: true);

            MethodInfo method = TestAssert.IsNotNull(
                typeof(SmtpEmailSender).GetMethod(
                    "CreateMimeMessage",
                    BindingFlags.NonPublic | BindingFlags.Instance));
            object? rawResult = method.Invoke(sender, new object[] { email });
            MimeMessage message = (MimeMessage)TestAssert.IsNotNull(rawResult);
            string body = TestAssert.IsNotNull(message.HtmlBody);

            Assert.IsTrue(
                body.Contains(ExpectedMandatoryLegend),
                "Body must contain mandatory legend.");
            Assert.IsTrue(body.Contains("REV999"), "Body must contain the match code.");
            Assert.IsTrue(
                body.Contains(AlphaUsername),
                "Body must contain creator display name.");
        });
    }

    private static void WithTestMailSettings(Action action)
    {
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            ["REVERSI_MAIL_HOST"] = "127.0.0.1",
            ["REVERSI_MAIL_PORT"] = "587",
            ["REVERSI_MAIL_SECURITY"] = "StartTls",
            ["REVERSI_MAIL_USERNAME"] = "test-user",
            ["REVERSI_MAIL_PASSWORD"] = "test-password",
            ["REVERSI_MAIL_FROM_ADDRESS"] = "system@reversi.example.com",
            ["REVERSI_MAIL_FROM_NAME"] = "Reversi System"
        };
        Dictionary<string, string?> previous = new Dictionary<string, string?>();
        try
        {
            foreach (KeyValuePair<string, string> entry in values)
            {
                previous.Add(entry.Key, Environment.GetEnvironmentVariable(entry.Key));
                Environment.SetEnvironmentVariable(entry.Key, entry.Value);
            }

            action();
        }
        finally
        {
            foreach (KeyValuePair<string, string?> entry in previous)
            {
                Environment.SetEnvironmentVariable(entry.Key, entry.Value);
            }
        }
    }

    private static AuthenticatedSession CreatePlayer(
        SessionTestContext context,
        long accountId,
        string username)
    {
        return TestSessionHelper.CreateSession(context.Sessions, accountId, username);
    }

    private sealed class InMemoryInvitationSender : ITemplatedEmailSender, IEmailSender
    {
        private readonly TemplatedEmailSender _templatedSender;

        internal InMemoryInvitationSender()
        {
            _templatedSender = new TemplatedEmailSender(
                new HtmlEmailTemplateRenderer(),
                this);
        }

        internal List<EmailMessage> SentEmails { get; } = new List<EmailMessage>();

        internal bool FailSend { get; set; }

        internal Action? BeforeSendCallback { get; set; }

        public void SendTemplatedEmail(
            string recipientAddress,
            string templateName,
            IReadOnlyDictionary<string, string> templateValues)
        {
            _templatedSender.SendTemplatedEmail(
                recipientAddress,
                templateName,
                templateValues);
        }

        public void Send(EmailMessage email)
        {
            BeforeSendCallback?.Invoke();
            if (FailSend)
            {
                throw new InvalidOperationException("Simulated provider outage.");
            }

            SentEmails.Add(email);
        }
    }

    internal sealed class MockMatchRow
    {
        internal Match Match { get; set; } = new Match();

        internal long InvitedAccountId { get; set; }

        internal bool IsConfirmed { get; set; }
    }

    private sealed class InvitationMockStore : IMatchRepository
    {
        private readonly object _sync = new object();
        private long _nextId = 100;

        internal Dictionary<long, MockMatchRow> Rows { get; } =
            new Dictionary<long, MockMatchRow>();

        internal Dictionary<long, (string Email, bool IsActive)> Accounts { get; } =
            new Dictionary<long, (string Email, bool IsActive)>();

        internal HashSet<(long A, long B)> Friendships { get; } = new HashSet<(long A, long B)>();

        internal int CancelCount { get; private set; }

        internal int ConfirmedCount { get; private set; }

        internal bool FailCancel { get; set; }

        public MatchResult<Match> StartSystemMatch(long accountId, long matchId, DateTime now)
        {
            throw new NotSupportedException();
        }

        public MatchStatus EvaluateLobbyAccess(long accountId)
        {
            if (!Accounts.TryGetValue(accountId, out (string Email, bool IsActive) account)
                || !account.IsActive)
            {
                return MatchStatus.InactiveAccount;
            }

            return MatchStatus.Success;
        }

        internal void RegisterFriendship(long accountA, long accountB, string friendEmail)
        {
            Accounts[accountA] = ("creator@example.com", true);
            Accounts[accountB] = (friendEmail, true);
            Friendships.Add((accountA, accountB));
            Friendships.Add((accountB, accountA));
        }

        internal void RegisterAccount(long accountId, string email, bool isActive)
        {
            Accounts[accountId] = (email, isActive);
        }

        public MatchResult<Match> CreateMatch(
            AuthenticatedSession player,
            MatchSettings settings,
            DateTime now)
        {
            lock (_sync)
            {
                long id = ++_nextId;
                string code = "AB"
                    + (id % 1000).ToString("D4").Replace('0', '2').Replace('1', '3');
                Match match = new Match
                {
                    Id = id,
                    Code = code,
                    State = WaitingState,
                    TurnDurationSeconds = settings.TurnDurationSeconds,
                    IsPrivate = false,
                    Participants = new[]
                    {
                        new MatchParticipant
                        {
                            AccountId = player.AccountId,
                            SeatNumber = 1,
                            DisplayName = player.Username
                        }
                    }
                };
                Rows[id] = new MockMatchRow { Match = match, IsConfirmed = true };
                return new MatchResult<Match>(match);
            }
        }

        public MatchResult<Match[]> GetAvailableMatches(long accountId)
        {
            lock (_sync)
            {
                Match[] matches = Rows.Values
                    .Where(row => !row.Match.IsPrivate && row.Match.State == WaitingState)
                    .Select(row => row.Match)
                    .ToArray();
                return new MatchResult<Match[]>(matches);
            }
        }

        public MatchResult<Match> JoinMatch(AuthenticatedSession player, string code, DateTime now)
        {
            lock (_sync)
            {
                MockMatchRow? row = Rows.Values.FirstOrDefault(r => r.Match.Code == code);
                if (row == null || row.Match.State != WaitingState)
                {
                    return new MatchResult<Match>(MatchStatus.MatchCodeUnavailable);
                }

                if (row.Match.IsPrivate && !row.IsConfirmed)
                {
                    return new MatchResult<Match>(MatchStatus.MatchCodeUnavailable);
                }

                if (row.Match.IsPrivate && row.InvitedAccountId != player.AccountId)
                {
                    return new MatchResult<Match>(MatchStatus.Unauthorized);
                }

                if (row.Match.Participants[0].AccountId == player.AccountId)
                {
                    return new MatchResult<Match>(MatchStatus.CannotJoinOwnMatch);
                }

                row.Match.State = InProgressState;
                row.Match.Participants = new[]
                {
                    row.Match.Participants[0],
                    new MatchParticipant
                    {
                        AccountId = player.AccountId,
                        SeatNumber = 2,
                        DisplayName = player.Username
                    }
                };
                return new MatchResult<Match>(row.Match);
            }
        }

        public MatchResult<MatchInvitationPreparation> PreparePrivateInvitation(
            AuthenticatedSession creator,
            long friendAccountId,
            MatchSettings settings,
            DateTime now)
        {
            lock (_sync)
            {
                if (Accounts.TryGetValue(
                    creator.AccountId,
                    out (string Email, bool IsActive) creatorInfo) && !creatorInfo.IsActive)
                {
                    return new MatchResult<MatchInvitationPreparation>(
                        MatchStatus.InactiveAccount);
                }

                if (friendAccountId <= 0)
                {
                    return new MatchResult<MatchInvitationPreparation>(
                        MatchStatus.FriendAccountUnavailable);
                }

                if (friendAccountId == creator.AccountId)
                {
                    return new MatchResult<MatchInvitationPreparation>(MatchStatus.Unauthorized);
                }

                if (!Accounts.TryGetValue(
                    friendAccountId,
                    out (string Email, bool IsActive) friendInfo) || !friendInfo.IsActive)
                {
                    return new MatchResult<MatchInvitationPreparation>(
                        MatchStatus.FriendAccountUnavailable);
                }

                if (!Friendships.Contains((creator.AccountId, friendAccountId)))
                {
                    return new MatchResult<MatchInvitationPreparation>(
                        MatchStatus.FriendshipNotFound);
                }

                long id = ++_nextId;
                string code = "PR"
                    + (id % 1000).ToString("D4").Replace('0', '2').Replace('1', '3');
                Match match = new Match
                {
                    Id = id,
                    Code = code,
                    State = WaitingState,
                    TurnDurationSeconds = settings.TurnDurationSeconds,
                    IsPrivate = true,
                    Participants = new[]
                    {
                        new MatchParticipant
                        {
                            AccountId = creator.AccountId,
                            SeatNumber = 1,
                            DisplayName = creator.Username
                        }
                    }
                };
                Rows[id] = new MockMatchRow
                {
                    Match = match,
                    InvitedAccountId = friendAccountId,
                    IsConfirmed = false
                };

                MatchInvitationPreparation preparation = new MatchInvitationPreparation(
                    match,
                    friendInfo.Email);
                return new MatchResult<MatchInvitationPreparation>(preparation);
            }
        }

        public MatchResult<Match> MarkInvitationDelivered(
            long accountId,
            long matchId,
            DateTime now)
        {
            lock (_sync)
            {
                if (!Rows.TryGetValue(matchId, out MockMatchRow? row)
                    || row.Match.State != WaitingState)
                {
                    return new MatchResult<Match>(MatchStatus.MatchNotWaiting);
                }

                row.IsConfirmed = true;
                ConfirmedCount++;
                return new MatchResult<Match>(row.Match);
            }
        }

        public MatchStatus ValidateGuestJoin(string matchCode)
        {
            lock (_sync)
            {
                MockMatchRow? row = Rows.Values.FirstOrDefault(r => r.Match.Code == matchCode);
                if (row != null && row.Match.IsPrivate)
                {
                    return MatchStatus.Unauthorized;
                }

                return MatchStatus.Success;
            }
        }

        public MatchStatus CancelMatch(long accountId, long matchId)
        {
            lock (_sync)
            {
                CancelCount++;
                if (FailCancel)
                {
                    throw new InvalidOperationException("Simulated cancel failure.");
                }

                if (Rows.TryGetValue(matchId, out MockMatchRow? row))
                {
                    row.Match.State = "Cancelled";
                }

                return MatchStatus.Success;
            }
        }

        public MatchResult<Match> LoadParticipantMatch(long accountId, long matchId)
        {
            lock (_sync)
            {
                if (!Rows.TryGetValue(matchId, out MockMatchRow? row))
                {
                    return new MatchResult<Match>(MatchStatus.MatchNotFound);
                }

                return new MatchResult<Match>(row.Match);
            }
        }

        public MatchStatus SaveMove(
            long matchId,
            MatchMove move,
            int consecutiveTimeouts,
            int nextTurnSeatNumber,
            DateTime turnStartedAtUtc)
        {
            throw new NotSupportedException();
        }

        public MatchStatus SaveMoveAndConclude(
            long matchId,
            MatchMove move,
            int consecutiveTimeouts,
            string endReason,
            int? winnerSeatNumber,
            int blackPieceCount,
            int whitePieceCount,
            DateTime endedAtUtc)
        {
            throw new NotSupportedException();
        }

        public MatchStatus ConcludeMatch(
            long matchId,
            string endReason,
            int? winnerSeatNumber,
            int? abandoningSeatNumber,
            int blackPieceCount,
            int whitePieceCount,
            DateTime endedAtUtc)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match[]> GetMatchHistory(long accountId)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match> GetForObservation(long accountId, long matchId)
        {
            throw new NotSupportedException();
        }

        public MatchResult<Match> ChangeTurnDuration(long accountId, long matchId, int seconds)
        {
            throw new NotSupportedException();
        }
    }
}
