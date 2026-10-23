using System;
using System.Security.Cryptography;
using System.Text;

namespace ReversiGameSystem.Server.Application.Verification;

/// <summary>Authenticates verification codes without persisting their plaintext.</summary>
public sealed class VerificationCodeProtector
{
    private readonly byte[] _key;

    /// <summary>Creates a protector with a private copy of the server key.</summary>
    /// <param name="key">A secret containing at least 32 bytes.</param>
    /// <exception cref="ArgumentException">The key is missing or too short.</exception>
    public VerificationCodeProtector(byte[] key)
    {
        if (key == null || key.Length < 32)
        {
            throw new ArgumentException("A key of at least 32 bytes is required.", nameof(key));
        }
        _key = (byte[])key.Clone();
    }

    /// <summary>Computes a keyed digest bound to the verification identity.</summary>
    /// <param name="identityKey">The purpose-bound identity key.</param>
    /// <param name="code">The candidate code.</param>
    /// <returns>The authenticated 32-byte digest.</returns>
    public byte[] Protect(string identityKey, string code)
    {
        using (HMACSHA256 authentication = new HMACSHA256(_key))
        {
            return authentication.ComputeHash(Encoding.UTF8.GetBytes(identityKey + ":" + code));
        }
    }
}
