using System.Security.Cryptography;
using System.Text;

namespace Jewel.JPMS.Api.Features.UsefulInformation;

/// <summary>
/// Encrypts a site credential for the database and decrypts it for a reveal — AES-256-GCM under
/// the configured key, a fresh nonce per value, the tag authenticating it. The stored form is one
/// base64 string: nonce (12 bytes) + tag (16 bytes) + ciphertext.
/// </summary>
public sealed class SecretProtector
{
    private const int NonceLength = 12;
    private const int TagLength = 16;

    private readonly UsefulInformationOptions options;

    public SecretProtector(UsefulInformationOptions options) { this.options = options; }

    // Read on use, not at construction, so an unconfigured portal still builds every endpoint
    // and answers the 400 its gate gives — never a 500 from the container.
    private byte[] Key
    {
        get
        {
            if (!options.CanHoldSecrets)
                throw new InvalidOperationException("No credential key is configured (UsefulInformation__SecretKey).");
            return Convert.FromBase64String(options.SecretKey!);
        }
    }

    public string Protect(string secret)
    {
        var plaintext = Encoding.UTF8.GetBytes(secret);
        var nonce = RandomNumberGenerator.GetBytes(NonceLength);
        var tag = new byte[TagLength];
        var ciphertext = new byte[plaintext.Length];
        using var aes = new AesGcm(Key, TagLength);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return Convert.ToBase64String(nonce.Concat(tag).Concat(ciphertext).ToArray());
    }

    public string Unprotect(string stored)
    {
        var bytes = Convert.FromBase64String(stored);
        var nonce = bytes[..NonceLength];
        var tag = bytes[NonceLength..(NonceLength + TagLength)];
        var ciphertext = bytes[(NonceLength + TagLength)..];
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(Key, TagLength);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
}
