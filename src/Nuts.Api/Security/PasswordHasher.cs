using System.Security.Cryptography;

namespace Nuts.Api.Security;

/// <summary>
/// PBKDF2-SHA256 password hasher with 100 000 iterations, 16-byte salt, 32-byte hash.
/// Output format: "base64(salt):base64(hash)". Used for both admin and user passwords.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize   = 16;
    private const int HashSize   = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public static string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored))
            return false;

        var parts = stored.Split(':', 2);
        if (parts.Length != 2)
            return false;

        byte[] salt, expected;
        try
        {
            salt     = Convert.FromBase64String(parts[0]);
            expected = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length != SaltSize || expected.Length != HashSize)
            return false;

        var computed = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
        return CryptographicOperations.FixedTimeEquals(expected, computed);
    }
}