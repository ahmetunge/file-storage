using System.Security.Cryptography;

namespace FileStorage.ConsoleApp.Extensions;

public static class ByteArrayExtensions
{
    public static string ComputeChecksum(this byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    
    public static bool ValidateChecksum(this byte[] data, string expectedChecksum)
    {
        var actualChecksum = ComputeChecksum(data);
        return actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }
}