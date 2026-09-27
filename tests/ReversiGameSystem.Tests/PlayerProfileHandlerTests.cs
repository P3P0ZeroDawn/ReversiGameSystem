using System;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;

namespace ReversiGameSystem.Tests;

internal sealed class PlayerProfileHandlerTests
{
    internal void ExecuteAsync_CurrentPlayer_ReturnsCalculatedStatistics()
    {
        PlayerProfileSessionStoreStub sessionStore = new PlayerProfileSessionStoreStub
        {
            Session = CreateSession(42)
        };
        PlayerProfileStoreStub profileStore = new PlayerProfileStoreStub
        {
            Result = new PlayerProfileStoreResult(
                PlayerProfileStoreOutcome.Found,
                new PlayerProfileData(
                    42,
                    "PlayerOne",
                    "profile-images/player.png",
                    7,
                    3,
                    2))
        };
        GetPlayerProfileHandler handler = new GetPlayerProfileHandler(sessionStore, profileStore);

        PlayerProfileResult result = handler.ExecuteAsync(
            new PlayerProfileQuery("session-token", null))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(PlayerProfileOutcome.Success, result.Outcome);
        TestAssert.AreEqual("session-token", sessionStore.SessionToken);
        TestAssert.AreEqual<long?>(42, profileStore.AccountId);
        TestAssert.AreEqual("PlayerOne", result.Profile?.Username);
        TestAssert.AreEqual("profile-images/player.png", result.Profile?.ProfileImageUri);
        TestAssert.AreEqual(12, result.Profile?.Statistics.MatchesPlayed);
        TestAssert.AreEqual(7, result.Profile?.Statistics.Wins);
        TestAssert.AreEqual(3, result.Profile?.Statistics.Losses);
        TestAssert.AreEqual(2, result.Profile?.Statistics.Draws);
        TestAssert.AreEqual(7d / 12d, result.Profile?.Statistics.WinRate);
    }

    internal void ExecuteAsync_PublicProfile_UsesRequestedAccount()
    {
        PlayerProfileStoreStub profileStore = new PlayerProfileStoreStub
        {
            Result = new PlayerProfileStoreResult(
                PlayerProfileStoreOutcome.Found,
                new PlayerProfileData(99, "OtherPlayer", null, 0, 0, 0))
        };
        GetPlayerProfileHandler handler = new GetPlayerProfileHandler(
            new PlayerProfileSessionStoreStub { Session = CreateSession(42) },
            profileStore);

        PlayerProfileResult result = handler.ExecuteAsync(
            new PlayerProfileQuery("session-token", 99))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(PlayerProfileOutcome.Success, result.Outcome);
        TestAssert.AreEqual<long?>(99, profileStore.AccountId);
        TestAssert.AreEqual(0d, result.Profile?.Statistics.WinRate);
    }

    internal void ExecuteAsync_MissingOrInvalidInput_ReturnsInvalidRequest()
    {
        GetPlayerProfileHandler handler = new GetPlayerProfileHandler(
            new PlayerProfileSessionStoreStub(),
            new PlayerProfileStoreStub());

        PlayerProfileResult missingToken = handler.ExecuteAsync(
            new PlayerProfileQuery("", null)).GetAwaiter().GetResult();
        PlayerProfileResult invalidAccount = handler.ExecuteAsync(
            new PlayerProfileQuery("session-token", 0)).GetAwaiter().GetResult();

        TestAssert.AreEqual(PlayerProfileOutcome.InvalidRequest, missingToken.Outcome);
        TestAssert.AreEqual(PlayerProfileOutcome.InvalidRequest, invalidAccount.Outcome);
    }

    internal void ExecuteAsync_UnknownSession_ReturnsInvalidSession()
    {
        PlayerProfileStoreStub profileStore = new PlayerProfileStoreStub();
        GetPlayerProfileHandler handler = new GetPlayerProfileHandler(
            new PlayerProfileSessionStoreStub(),
            profileStore);

        PlayerProfileResult result = handler.ExecuteAsync(
            new PlayerProfileQuery("unknown-token", null))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(PlayerProfileOutcome.InvalidSession, result.Outcome);
        TestAssert.AreEqual<long?>(null, profileStore.AccountId);
    }

    internal void ExecuteAsync_MissingAccount_ReturnsAccountNotFound()
    {
        GetPlayerProfileHandler handler = new GetPlayerProfileHandler(
            new PlayerProfileSessionStoreStub { Session = CreateSession(42) },
            new PlayerProfileStoreStub
            {
                Result = new PlayerProfileStoreResult(PlayerProfileStoreOutcome.NotFound)
            });

        PlayerProfileResult result = handler.ExecuteAsync(
            new PlayerProfileQuery("session-token", null))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(PlayerProfileOutcome.AccountNotFound, result.Outcome);
    }

    internal void ExecuteAsync_UnavailableStore_ReturnsServiceUnavailable()
    {
        GetPlayerProfileHandler handler = new GetPlayerProfileHandler(
            new PlayerProfileSessionStoreStub { Session = CreateSession(42) },
            new PlayerProfileStoreStub
            {
                Result = new PlayerProfileStoreResult(PlayerProfileStoreOutcome.Unavailable)
            });

        PlayerProfileResult result = handler.ExecuteAsync(
            new PlayerProfileQuery("session-token", null))
            .GetAwaiter()
            .GetResult();

        TestAssert.AreEqual(PlayerProfileOutcome.ServiceUnavailable, result.Outcome);
    }

    private static AuthenticatedSessionRecord CreateSession(long accountId)
    {
        return new AuthenticatedSessionRecord(
            "session-token",
            accountId,
            "PlayerOne",
            "player@example.com",
            DateTime.UtcNow.AddHours(8));
    }
}

internal sealed class PlayerProfileSessionStoreStub : IAuthenticatedSessionStore
{
    internal AuthenticatedSessionRecord? Session { get; set; }

    internal string? SessionToken { get; private set; }

    public Task<AuthenticatedSessionRecord> CreateAsync(
        long accountId,
        string username,
        string emailAddress)
    {
        throw new NotSupportedException();
    }

    public Task<AuthenticatedSessionRecord?> FindAsync(string sessionToken)
    {
        SessionToken = sessionToken;
        return Task.FromResult(Session);
    }
}

internal sealed class PlayerProfileStoreStub : IPlayerProfileStore
{
    internal long? AccountId { get; private set; }

    internal PlayerProfileStoreResult Result { get; set; } =
        new PlayerProfileStoreResult(PlayerProfileStoreOutcome.NotFound);

    public Task<PlayerProfileStoreResult> GetAsync(long accountId)
    {
        AccountId = accountId;
        return Task.FromResult(Result);
    }
}
