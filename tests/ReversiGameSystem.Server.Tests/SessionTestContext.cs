using System;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests;

internal sealed class SessionTestContext : IUtcClock, IVerificationEmailSender,
    IVerificationCodeRepository, IAccountLoginStore, IPasswordHasher
{
    internal SessionTestContext()
    {
        Protector = new VerificationCodeProtector(new byte[32]);
        Issuer = new VerificationCodeIssuer(this, Protector, this);
        Codes = new VerificationCodeService(Issuer, this, this);
        Sessions = new RecordingSessionStore(this);
        Registry = new SessionRegistry(Sessions, this);
        Handler = new SessionHandler(new VerifiedLoginService(this, this), Codes, Registry);
    }

    public DateTime UtcNow { get; set; } = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
    internal VerificationCodeProtector Protector { get; }
    internal VerificationCodeIssuer Issuer { get; }
    internal VerificationCodeService Codes { get; }
    internal RecordingSessionStore Sessions { get; }
    internal SessionRegistry Registry { get; }
    internal SessionHandler Handler { get; }
    internal VerificationEmail? Email { get; private set; }
    internal PendingVerificationCode? Pending { get; private set; }
    internal VerificationAttempt? Attempt { get; private set; }
    internal bool FailDelivery { get; set; }
    internal bool Inactive { get; set; }
    internal bool Unavailable { get; set; }
    internal VerificationResult ConsumeResult { get; set; } = VerificationResult.Accepted;

    public void Send(VerificationEmail email)
    {
        if (FailDelivery) { throw new TimeoutException("Private provider diagnostic."); }
        Email = email;
    }

    public void Replace(PendingVerificationCode code) => Pending = code;

    public VerificationResult Consume(VerificationAttempt attempt)
    {
        Attempt = attempt;
        return ConsumeResult;
    }

    public Task<AccountLoginLookupResult> FindByLoginAsync(string login)
    {
        AccountLoginLookupOutcome outcome = Unavailable
            ? AccountLoginLookupOutcome.Unavailable : AccountLoginLookupOutcome.Found;
        return Task.FromResult(new AccountLoginLookupResult(outcome,
            new LoginAccount(42, "PlayerOne", "player@example.com", "test-hash",
                Inactive ? "INACTIVE" : "ACTIVE", "en-US")));
    }

    public string Hash(string password) => "test-hash";
    public bool Verify(string password, string encodedHash) => password == "Password1!";
}

internal sealed class RecordingSessionStore : IAuthenticatedSessionStore
{
    private readonly InMemoryAuthenticatedSessionStore _inner;
    internal RecordingSessionStore(IUtcClock clock) =>
        _inner = new InMemoryAuthenticatedSessionStore(clock);
    internal int CreatedCount { get; private set; }
    public AuthenticatedSessionRecord Create(LoginAccount account)
    {
        CreatedCount++;
        return _inner.Create(account);
    }
    public bool TryGet(string token, out AuthenticatedSessionRecord? session) =>
        _inner.TryGet(token, out session);
    public void Revoke(string token) => _inner.Revoke(token);
}
