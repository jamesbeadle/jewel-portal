using System.Security.Cryptography;

namespace Jewel.JPMS.Api.Features.Progress.Photos;

/// <summary>SHA-256 of the file as received, lower-case hex — the one definition of "the same image".</summary>
internal static class ProgressPhotoContentHash
{
    public static string Of(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
