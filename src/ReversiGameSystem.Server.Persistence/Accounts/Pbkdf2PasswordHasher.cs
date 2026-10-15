using System;
using System.Security.Cryptography;
using System.Text;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Persistence.Accounts;

/// <summary>
/// Represents pbkdf2 password hasher.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Algorithm = "PBKDF2-SHA256";
    private const int IterationCount = 100000;
    private const int MaximumAcceptedIterationCount = 1000000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    /// <summary>
    /// Performs the hash operation.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <returns>The result of the operation.</returns>
    public string Hash(string password)
    {
        if (password == null)
        {
            throw new ArgumentNullException(nameof(password));
        }

        byte[] salt = new byte[SaltSize];
        using (RandomNumberGenerator random = RandomNumberGenerator.Create())
        {
            random.GetBytes(salt);
        }

        byte[] hash = Derive(password, salt, IterationCount);
        return string.Join(
            "$",
            Algorithm,
            IterationCount.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    /// <summary>
    /// Performs the verify operation.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <param name="encodedHash">The encoded hash.</param>
    /// <returns>The result of the operation.</returns>
    public bool Verify(string password, string encodedHash)
    {
        if (password == null || string.IsNullOrWhiteSpace(encodedHash))
        {
            return false;
        }

        if (!TryParse(encodedHash, out PasswordHashComponents? components)
            || components == null)
        {
            return false;
        }

        byte[] actualHash = Derive(password, components.Salt, components.Iterations);
        return FixedTimeEquals(components.Hash, actualHash);
    }

    private static bool TryParse(
        string encodedHash,
        out PasswordHashComponents? components)
    {
        components = null;
        string[] parts = encodedHash.Split('$');
        if (!HasValidHeader(parts, out int iterations))
        {
            return false;
        }

        bool isExtracted;
        try
        {
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] hash = Convert.FromBase64String(parts[3]);
            if (salt.Length != SaltSize || hash.Length != HashSize)
            {
                isExtracted = false;
            }
            else
            {
                components = new PasswordHashComponents(iterations, salt, hash);
                isExtracted = true;
            }
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            isExtracted = false;
        }

        return isExtracted;
    }

    private static bool HasValidHeader(string[] parts, out int iterations)
    {
        iterations = 0;
        return parts.Length == 4
            && string.Equals(parts[0], Algorithm, StringComparison.Ordinal)
            && int.TryParse(parts[1], out iterations)
            && iterations >= 1
            && iterations <= MaximumAcceptedIterationCount;
    }

    private static byte[] Derive(string password, byte[] salt, int iterations)
    {
        string normalizedPassword = password.Normalize(NormalizationForm.FormC);
        using (Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(
            normalizedPassword,
            salt,
            iterations,
            HashAlgorithmName.SHA256))
        {
            return deriveBytes.GetBytes(HashSize);
        }
    }

    private static bool FixedTimeEquals(byte[] expected, byte[] actual)
    {
        if (expected.Length != actual.Length)
        {
            return false;
        }

        int difference = 0;
        for (int index = 0; index < expected.Length; index++)
        {
            difference |= expected[index] ^ actual[index];
        }

        return difference == 0;
    }

    private sealed class PasswordHashComponents
    {
        internal PasswordHashComponents(int iterations, byte[] salt, byte[] hash)
        {
            Iterations = iterations;
            Salt = salt;
            Hash = hash;
        }

        internal int Iterations { get; }

        internal byte[] Salt { get; }

        internal byte[] Hash { get; }
    }
}
