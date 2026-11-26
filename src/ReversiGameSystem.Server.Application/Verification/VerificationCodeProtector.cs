using System;
using System.Security.Cryptography;
using System.Text;

namespace ReversiGameSystem.Server.Application.Verification;

public sealed class VerificationCodeProtector
{
    private const int MinimumKeyLengthBytes = 32;
    private readonly byte[] _hmacKey;

    public VerificationCodeProtector(byte[] hmacKey)
    {
        if (hmacKey == null || hmacKey.Length < MinimumKeyLengthBytes)
        {
            throw new ArgumentException(
                "A key of at least 32 bytes is required.",
                nameof(hmacKey));
        }

        _hmacKey = (byte[])hmacKey.Clone();
    }

    public byte[] ComputeCodeDigest(string identityKey, string verificationCode)
    {
        if (string.IsNullOrWhiteSpace(identityKey))
        {
            throw new ArgumentException(
                "A verification identity key is required.",
                nameof(identityKey));
        }

        string candidateCode = verificationCode ?? string.Empty;
        byte[] payload = Encoding.UTF8.GetBytes(identityKey + ":" + candidateCode);
        byte[] digest;
        using (HMACSHA256 authentication = new HMACSHA256(_hmacKey))
        {
            digest = authentication.ComputeHash(payload);
        }

        return digest;
    }
}
