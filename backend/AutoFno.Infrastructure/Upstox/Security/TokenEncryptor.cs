using System.Security.Cryptography;
using System.Text;

namespace AutoFno.Infrastructure.Upstox.Security;

public static class TokenEncryptor
{
    // 32-byte secret key for AES-256
    private static readonly byte[] Key = SHA256.HashData(Encoding.UTF8.GetBytes("AutoTrade_FnO_UltraSecretEncryptionKey_2026!#$"));

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = Key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to ciphertext
        var combined = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, combined, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(combined);
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        try
        {
            var combined = Convert.FromBase64String(cipherText);
            if (combined.Length < 16) return cipherText; // Not encrypted

            using var aes = Aes.Create();
            aes.Key = Key;

            var iv = new byte[16];
            var cipherBytes = new byte[combined.Length - 16];
            Buffer.BlockCopy(combined, 0, iv, 0, 16);
            Buffer.BlockCopy(combined, 16, cipherBytes, 0, cipherBytes.Length);

            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return cipherText; // Fallback if plain
        }
    }
}
