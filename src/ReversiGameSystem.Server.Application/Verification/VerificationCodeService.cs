using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Verification;

/// <summary>Coordinates delivery and durable single-use verification.</summary>
public sealed class VerificationCodeService
{
    private static readonly object[] RequestGates = CreateRequestGates();
    private readonly VerificationCodeIssuer _issuer;
    private readonly IVerificationCodeRepository _repository;
    private readonly IUtcClock _clock;

    /// <summary>Creates the reusable verification workflow.</summary>
    /// <param name="issuer">The delivery and protection service.</param>
    /// <param name="repository">The durable verification store.</param>
    /// <param name="clock">The authoritative UTC clock.</param>
    public VerificationCodeService(VerificationCodeIssuer issuer,
        IVerificationCodeRepository repository, IUtcClock clock)
    {
        _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Replaces a pending code only after delivery has been accepted.</summary>
    /// <param name="identity">The authorized action and recipient binding.</param>
    /// <returns>The accepted code's UTC expiry.</returns>
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

    /// <summary>Attempts one durable single-use verification.</summary>
    /// <param name="identity">The authorized action and recipient binding.</param>
    /// <param name="code">The candidate supplied by the user.</param>
    /// <returns>The verification outcome.</returns>
    public VerificationResult Consume(VerificationIdentity identity, string code)
    {
        string candidate = code ?? string.Empty;
        if (candidate.Length > 6)
        {
            candidate = string.Empty;
        }
        return _repository.Consume(new VerificationAttempt
        {
            IdentityKey = identity.Key, Digest = _issuer.Protect(identity, candidate),
            NowUtc = _clock.UtcNow
        });
    }

    private static object[] CreateRequestGates()
    {
        object[] gates = new object[32];
        for (int index = 0; index < gates.Length; index++)
        {
            gates[index] = new object();
        }
        return gates;
    }
}
