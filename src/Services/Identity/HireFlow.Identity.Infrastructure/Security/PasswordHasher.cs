using System.Security.Cryptography;
using HireFlow.Identity.Application.Interfaces;

namespace HireFlow.Identity.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32;  // 256 bit
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
    private const char SegmentDelimiter = ':';

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            KeySize);

        return string.Join(
            SegmentDelimiter,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash),
            Iterations,
            Algorithm.Name);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        string[] segments = hashedPassword.Split(SegmentDelimiter);
        if (segments.Length != 4)
        {
            return false;
        }

        byte[] salt = Convert.FromBase64String(segments[0]);
        byte[] expectedHash = Convert.FromBase64String(segments[1]);
        if (!int.TryParse(segments[2], out int iterations))
        {
            return false;
        }
        var algorithm = new HashAlgorithmName(segments[3]);

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            algorithm,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
