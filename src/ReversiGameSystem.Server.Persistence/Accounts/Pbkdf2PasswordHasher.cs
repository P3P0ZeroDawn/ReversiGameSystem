using System;
using System.Security.Cryptography;
using System.Text;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Persistence.Accounts;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Algorithm = "PBKDF2-SHA256";
    private const int IterationCount = 100000;
    private const int MaximumAcceptedIterationCount = 1000000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

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

        byte[] hash = Derive(password, salt, IterationCount, HashSize);
        return string.Join(
            "$",
            Algorithm,
            IterationCount.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string encodedHash)
    {
        if (password == null || string.IsNullOrWhiteSpace(encodedHash))
        {
            return false;
        }

        string[] parts = encodedHash.Split('$');
        if (parts.Length != 4
            || !string.Equals(parts[0], Algorithm, StringComparison.Ordinal)
            || !int.TryParse(parts[1], out int iterations)
            || iterations < 1
            || iterations > MaximumAcceptedIterationCount)
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expectedHash = Convert.FromBase64String(parts[3]);
            if (salt.Length != SaltSize || expectedHash.Length != HashSize)
            {
                return false;
            }

            byte[] actualHash = Derive(password, salt, iterations, expectedHash.Length);
            return FixedTimeEquals(expectedHash, actualHash);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static byte[] Derive(string password, byte[] salt, int iterations, int size)
    {
        string normalizedPassword = password.Normalize(NormalizationForm.FormC);
        using (Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(
            normalizedPassword,
            salt,
            iterations,
            HashAlgorithmName.SHA256))
        {
            return deriveBytes.GetBytes(size);
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
}
