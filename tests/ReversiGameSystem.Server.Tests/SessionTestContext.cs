using System;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Persistence.Sessions;

namespace ReversiGameSystem.Server.Tests;

internal sealed class SessionTestContext : IUtcClock, IVerificationEmailSender,
    IVerificationCodeRepository, IAccountLoginRepository, IPasswordHasher
{
    internal SessionTestContext()
    {
        Protector = new VerificationCodeProtector(new byte[32]);
        Issuer = new VerificationCodeIssuer(this, Protector, this);
        Codes = new VerificationCodeService(Issuer, this, this);
        Sessions = new RecordingSessionRepository(this);
        ApplicationService = new SessionApplicationService(
            this,
            this,
            Codes,
            Sessions);
    }

    public DateTime UtcNow { get; set; } = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
    internal VerificationCodeProtector Protector { get; }
    internal VerificationCodeIssuer Issuer { get; }
    internal VerificationCodeService Codes { get; }
    internal RecordingSessionRepository Sessions { get; }
    internal SessionApplicationService ApplicationService { get; }
    internal VerificationEmail? Email { get; private set; }
    internal PendingVerificationCode? Pending { get; private set; }
    internal VerificationAttempt? Attempt { get; private set; }
    internal bool FailDelivery { get; set; }
    internal bool Inactive { get; set; }
    internal bool Unavailable { get; set; }
    internal VerificationStatus ConsumeStatus { get; set; } = VerificationStatus.Accepted;

    public void Send(VerificationEmail email)
    {
        if (FailDelivery) { throw new TimeoutException("Private provider diagnostic."); }
        Email = email;
    }

    public void Replace(PendingVerificationCode code) => Pending = code;

    public VerificationStatus Consume(VerificationAttempt attempt)
    {
        Attempt = attempt;
        return ConsumeStatus;
    }

    public Task<AccountLoginLookupResult> FindByLoginAsync(string login)
    {
        if (Unavailable)
        {
            throw new InvalidOperationException("Account lookup is unavailable.");
        }

        LoginAccount account = new LoginAccount(
            42,
            "PlayerOne",
            "player@example.com",
            "test-hash",
            Inactive ? "INACTIVE" : "ACTIVE",
            "en-US");
        return Task.FromResult(AccountLoginLookupResult.CreateFound(account));
    }

    public string Hash(string password) => "test-hash";
    public bool Verify(string password, string encodedHash) => password == "Password1!";
}

internal sealed class RecordingSessionRepository : ISessionRepository
{
    private readonly SessionRepository _inner;

    internal RecordingSessionRepository(IUtcClock clock)
    {
        _inner = new SessionRepository(clock);
    }

    internal int CreatedCount { get; private set; }

    public AuthenticatedSession Create(LoginAccount account)
    {
        CreatedCount++;
        return _inner.Create(account);
    }

    public SessionOperationResult<GuestSession> CreateGuest(string displayName)
    {
        return _inner.CreateGuest(displayName);
    }

    public bool TryGet(string token, out AuthenticatedSession? session)
    {
        return _inner.TryGet(token, out session);
    }

    public bool TryGetGuest(string token, out GuestSession? session)
    {
        return _inner.TryGetGuest(token, out session);
    }

    public void Revoke(string token)
    {
        _inner.Revoke(token);
    }
}
