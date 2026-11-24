using System;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Application.Verification;

public sealed class VerificationCodeService
{
    private const int VerificationCodeLength = 6;
    private const int RequestLockStripeCount = 32;
    private static readonly object[] RequestGates = CreateRequestGates();
    private readonly VerificationCodeIssuer _issuer;
    private readonly IVerificationCodeRepository _repository;
    private readonly IUtcClock _clock;

    public VerificationCodeService(VerificationCodeIssuer issuer,
        IVerificationCodeRepository repository, IUtcClock clock)
    {
        _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public DateTime CalculatePlausibleExpiry()
    {
        DateTime expiryUtc = _clock.UtcNow.AddMinutes(5);
        return expiryUtc;
    }

    public DateTime Request(VerificationIdentity identity)
    {
        // Bound synchronization memory while serializing delivery and replacement per identity.
        int gateIndex = (identity.Key.GetHashCode() & int.MaxValue) % RequestGates.Length;
        lock (RequestGates[gateIndex])
        {
            PendingVerificationCode code = _issuer.Issue(identity);
            _repository.Replace(code);
            return code.ExpiresAtUtc;
        }
    }

    public VerificationStatus Consume(VerificationIdentity identity, string code)
    {
        string candidate = code ?? string.Empty;
        if (candidate.Length > VerificationCodeLength)
        {
            candidate = string.Empty;
        }
        return _repository.Consume(new VerificationAttempt
        {
            IdentityKey = identity.Key,
            Digest = _issuer.Protect(identity, candidate),
            NowUtc = _clock.UtcNow
        });
    }

    private static object[] CreateRequestGates()
    {
        object[] gates = new object[RequestLockStripeCount];
        for (int index = 0; index < gates.Length; index++)
        {
            gates[index] = new object();
        }
        return gates;
    }
}
