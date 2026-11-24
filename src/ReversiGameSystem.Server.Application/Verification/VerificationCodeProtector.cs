using System;
using System.Security.Cryptography;
using System.Text;

namespace ReversiGameSystem.Server.Application.Verification;

public sealed class VerificationCodeProtector
{
    private readonly byte[] _key;

    public VerificationCodeProtector(byte[] key)
    {
        if (key == null || key.Length < 32)
        {
            throw new ArgumentException("A key of at least 32 bytes is required.", nameof(key));
        }
        _key = (byte[])key.Clone();
    }

    public byte[] Protect(string identityKey, string code)
    {
        using (HMACSHA256 authentication = new HMACSHA256(_key))
        {
            return authentication.ComputeHash(Encoding.UTF8.GetBytes(identityKey + ":" + code));
        }
    }
}
