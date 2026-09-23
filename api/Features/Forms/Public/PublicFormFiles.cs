using System.Text.RegularExpressions;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Public;

/// <summary>
/// What a stranger's file is held to (api/forms-intake.js op:'file'): a capped size, a sanitised
/// name, a known extension or ".bin", and a session id the page made up — a form's files arrive
/// before the form does, so the session is the only thing tying them together until it is sent.
/// </summary>
internal static class PublicFormFiles
{
    private const int LongestFileName = 90;
    private const string Fallback = "file";
    private static readonly Regex UnsafeCharacters = new("[^A-Za-z0-9._ -]");
    private static readonly Regex SessionId =
        new($"^[a-f0-9]{{{PublicFormLimits.ShortestSessionId},{PublicFormLimits.LongestSessionId}}}$");

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".heic"] = "image/heic",
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    public static bool IsASession(string? sessionId) => sessionId is not null && SessionId.IsMatch(sessionId);

    public static string SafeName(string? posted)
    {
        var bare = UnsafeCharacters.Replace(Path.GetFileName(posted ?? ""), "_").Trim();
        var capped = bare.Length > LongestFileName ? bare[..LongestFileName] : bare;
        var name = capped.Length > 0 ? capped : Fallback;
        var isAKnownKind = ContentTypes.ContainsKey(Path.GetExtension(name));
        return isAKnownKind ? name : name + ".bin";
    }

    public static string ContentTypeOf(string fileName) =>
        ContentTypes.GetValueOrDefault(Path.GetExtension(fileName), "application/octet-stream");

    public static byte[] Decoded(string? base64)
    {
        var encoded = base64 ?? "";
        var isTooLong = encoded.Length == 0 || encoded.Length > PublicFormLimits.LongestEncodedUpload;
        if (isTooLong) throw new PublicFormRefusal("File too large.");
        var bytes = TryDecode(encoded) ?? throw new PublicFormRefusal("Bad file data.");
        var isTooBig = bytes.Length == 0 || bytes.Length > PublicFormLimits.LargestUploadBytes;
        return isTooBig ? throw new PublicFormRefusal("File too large.") : bytes;
    }

    private static byte[]? TryDecode(string encoded)
    {
        try { return Convert.FromBase64String(encoded); }
        catch (FormatException) { return null; }
    }
}
