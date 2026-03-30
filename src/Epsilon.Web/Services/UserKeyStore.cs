using System.Security.Cryptography;
using System.Text;
using Epsilon.Web.Data;
using Epsilon.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Epsilon.Web.Services;

public class UserKeyStore
{
    private readonly AppDbContext _db;
    private readonly byte[] _masterKey;

    public UserKeyStore(AppDbContext db, IConfiguration config)
    {
        _db = db;
        var keyStr = Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
            ?? config["Encryption:MasterKey"]
            ?? throw new InvalidOperationException("Encryption:MasterKey not configured");
        _masterKey = Convert.FromBase64String(keyStr);
    }

    public async Task<string?> GetKeyAsync(Guid userId, string providerId)
    {
        var entry = await _db.UserApiKeys
            .FirstOrDefaultAsync(k => k.UserId == userId && k.ProviderId == providerId);
        if (entry == null) return null;
        return Decrypt(entry.EncryptedKey);
    }

    public async Task SaveKeyAsync(Guid userId, string providerId, string plainKey)
    {
        var entry = await _db.UserApiKeys
            .FirstOrDefaultAsync(k => k.UserId == userId && k.ProviderId == providerId);

        var encrypted = Encrypt(plainKey);

        if (entry != null)
        {
            entry.EncryptedKey = encrypted;
            entry.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.UserApiKeys.Add(new UserApiKey
            {
                UserId = userId,
                ProviderId = providerId,
                EncryptedKey = encrypted,
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteKeyAsync(Guid userId, string providerId)
    {
        var entry = await _db.UserApiKeys
            .FirstOrDefaultAsync(k => k.UserId == userId && k.ProviderId == providerId);
        if (entry != null)
        {
            _db.UserApiKeys.Remove(entry);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<(string ProviderId, DateTime UpdatedAt)>> ListConfiguredAsync(Guid userId)
    {
        return await _db.UserApiKeys
            .Where(k => k.UserId == userId)
            .Select(k => new ValueTuple<string, DateTime>(k.ProviderId, k.UpdatedAt))
            .ToListAsync();
    }

    private string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _masterKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    private string Decrypt(string encrypted)
    {
        var data = Convert.FromBase64String(encrypted);

        using var aes = Aes.Create();
        aes.Key = _masterKey;

        var iv = new byte[16];
        Buffer.BlockCopy(data, 0, iv, 0, 16);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = new byte[data.Length - 16];
        Buffer.BlockCopy(data, 16, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
