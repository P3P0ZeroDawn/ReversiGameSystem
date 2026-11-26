using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Application.Verification;

public sealed class VerificationCodeService
{
    private const int VerificationCodeLength = 6;
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);
    private readonly VerificationCodeProtector _protector;
    private readonly IVerificationCodeRepository _repository;
    private readonly ITemplatedEmailSender _emailSender;
    private readonly IUtcClock _clock;

    public VerificationCodeService(
        VerificationCodeProtector protector,
        IVerificationCodeRepository repository,
        ITemplatedEmailSender emailSender,
        IUtcClock clock)
    {
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public DateTime CalculatePlausibleExpirationUtc()
    {
        DateTime expirationUtc = _clock.UtcNow.Add(CodeLifetime);
        return expirationUtc;
    }

    public DateTime IssueVerificationCode(VerificationIdentity identity)
    {
        if (identity == null)
        {
            throw new ArgumentNullException(nameof(identity));
        }

        string verificationCode = GenerateSixDigitCode();
        DateTime createdAtUtc = _clock.UtcNow;
        DateTime expiresAtUtc = createdAtUtc.Add(CodeLifetime);
        PendingVerificationCode pendingCode = new PendingVerificationCode
        {
            IdentityKey = identity.Key,
            RecipientAddress = identity.RecipientAddress,
            Purpose = identity.Purpose,
            CreatedAtUtc = createdAtUtc,
            Digest = _protector.ComputeCodeDigest(identity.Key, verificationCode),
            ExpiresAtUtc = expiresAtUtc
        };

        SendVerificationEmail(identity, verificationCode, expiresAtUtc);
        _repository.SavePendingCode(pendingCode);
        return expiresAtUtc;
    }

    public VerificationStatus VerifyCode(VerificationIdentity identity, string submittedCode)
    {
        if (identity == null)
        {
            throw new ArgumentNullException(nameof(identity));
        }

        string normalizedCandidateCode = submittedCode ?? string.Empty;
        if (normalizedCandidateCode.Length > VerificationCodeLength)
        {
            normalizedCandidateCode = string.Empty;
        }

        byte[] candidateDigest = _protector.ComputeCodeDigest(identity.Key, normalizedCandidateCode);
        DateTime evaluationTimeUtc = _clock.UtcNow;
        VerificationStatus status = _repository.ProcessVerificationAttempt(
            identity.Key,
            storedCode => EvaluateAttempt(storedCode, candidateDigest, evaluationTimeUtc));
        return status;
    }

    private void SendVerificationEmail(
        VerificationIdentity identity,
        string verificationCode,
        DateTime expiresAtUtc)
    {
        string templateName = GetTemplateName(identity.Purpose);
        string formattedExpiration = expiresAtUtc
            .ToUniversalTime()
            .ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
        Dictionary<string, string> templateValues = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Locale", identity.PreferredLocaleCode },
            { "VerificationCode", verificationCode },
            { "ExpiresAtUtc", formattedExpiration }
        };

        _emailSender.SendTemplatedEmail(
            identity.RecipientAddress,
            templateName,
            templateValues);
    }

    private static VerificationEvaluationResult EvaluateAttempt(
        StoredVerificationCode storedCode,
        byte[] candidateDigest,
        DateTime evaluationTimeUtc)
    {
        if (!storedCode.Exists
            || storedCode.UsedAtUtc.HasValue
            || storedCode.InvalidatedAtUtc.HasValue
            || storedCode.FailedAttempts >= MaxFailedAttempts)
        {
            return new VerificationEvaluationResult(VerificationStatus.Invalid);
        }

        if (storedCode.ExpiresAtUtc <= evaluationTimeUtc)
        {
            return new VerificationEvaluationResult(VerificationStatus.Expired);
        }

        if (AreDigestsEqual(storedCode.Digest, candidateDigest))
        {
            VerificationEvaluationResult acceptedResult =
                new VerificationEvaluationResult(evaluationTimeUtc);
            return acceptedResult;
        }

        int updatedFailedAttempts = storedCode.FailedAttempts + 1;
        DateTime? invalidatedAtUtc = updatedFailedAttempts >= MaxFailedAttempts
            ? evaluationTimeUtc
            : (DateTime?)null;
        VerificationEvaluationResult failedResult = new VerificationEvaluationResult(
            updatedFailedAttempts,
            invalidatedAtUtc);
        return failedResult;
    }

    private static bool AreDigestsEqual(byte[] storedDigest, byte[] candidateDigest)
    {
        if (storedDigest.Length != candidateDigest.Length)
        {
            return false;
        }

        int difference = 0;
        for (int index = 0; index < storedDigest.Length; index++)
        {
            difference |= storedDigest[index] ^ candidateDigest[index];
        }

        bool isMatch = difference == 0;
        return isMatch;
    }

    private static string GetTemplateName(VerificationPurpose purpose) => purpose switch
    {
        VerificationPurpose.Login => EmailTemplateNames.VerificationLogin,
        VerificationPurpose.Registration => EmailTemplateNames.VerificationRegistration,
        VerificationPurpose.PasswordReset => EmailTemplateNames.VerificationPasswordReset,
        _ => throw new ArgumentOutOfRangeException(
            nameof(purpose),
            "Unsupported verification purpose.")
    };

    private static string GenerateSixDigitCode()
    {
        const uint Range = 1000000;
        const uint Limit = uint.MaxValue - (uint.MaxValue % Range);
        byte[] bytes = new byte[4];
        using (RandomNumberGenerator random = RandomNumberGenerator.Create())
        {
            uint value;
            do
            {
                random.GetBytes(bytes);
                value = BitConverter.ToUInt32(bytes, 0);
            }
            while (value >= Limit);

            string formattedCode = (value % Range).ToString("D6", CultureInfo.InvariantCulture);
            return formattedCode;
        }
    }
}
