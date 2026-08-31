using System.IO.Compression;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

// Decides which entries inside a triage zip are worth extracting, and what content type each lands
// with. Junk the archive tooling itself created (macOS resource forks, thumbnail caches, dot-files)
// is skipped silently; everything else comes out, because the triager decides what to file — this
// screen only removes files nobody would ever file.
public static class ArchiveEntryScreen
{
    public const int MaximumEntries = 100;
    public const long MaximumEntryBytes = 100L * 1024 * 1024;
    public const long MaximumTotalBytes = 500L * 1024 * 1024;

    public static bool IsExtractable(ZipArchiveEntry entry)
    {
        if (entry.Name.Length == 0) return false;                       // directory entries
        if (entry.Length == 0) return false;
        if (entry.FullName.StartsWith("__MACOSX/", StringComparison.OrdinalIgnoreCase)) return false;
        if (entry.Name.StartsWith('.')) return false;                   // .DS_Store and friends
        if (entry.Name.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    // The filing form and preview key off content type, and zip entries carry none — so it is
    // recovered from the extension. Unknown extensions land as octet-stream (downloadable, not
    // previewable), which is honest.
    public static string ContentTypeFor(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".zip" => "application/zip",
            ".dwg" => "application/acad",
            ".dxf" => "image/vnd.dxf",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            _ => "application/octet-stream"
        };
    }

    public static bool LooksLikeZip(string fileName, string contentType)
    {
        if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return true;
        if (contentType.Equals("application/zip", StringComparison.OrdinalIgnoreCase)) return true;
        return contentType.Equals("application/x-zip-compressed", StringComparison.OrdinalIgnoreCase);
    }

    // The caps keep one extraction inside a single request's budget (the SWA gateway allows ~45s)
    // and out of zip-bomb territory; a violation surfaces as the 400 the page shows verbatim.
    // These checks read the archive's DECLARED sizes — ReadEntryBoundedAsync below enforces the
    // same cap on the actual bytes, because a crafted header can lie.
    public static void GuardLimits(IReadOnlyList<ZipArchiveEntry> entries)
    {
        if (entries.Count == 0)
            throw new InvalidOperationException("The archive contains no extractable files.");
        if (entries.Count > MaximumEntries)
            throw new InvalidOperationException($"The archive holds {entries.Count} files — the most that can be extracted in one go is {MaximumEntries}.");
        if (entries.Any(entry => entry.Length > MaximumEntryBytes))
            throw new InvalidOperationException("A file inside the archive is larger than 100 MB — extract it locally instead.");
        if (entries.Sum(entry => entry.Length) > MaximumTotalBytes)
            throw new InvalidOperationException("The archive unpacks to more than 500 MB — extract it locally instead.");
    }

    /// <summary>Decompresses one entry with the per-entry cap enforced on the REAL byte count —
    /// the returned stream is rewound and ready to upload.</summary>
    public static async Task<MemoryStream> ReadEntryBoundedAsync(
        ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
        await using var content = entry.Open();
        var buffer = new MemoryStream();
        var chunk = new byte[81920];
        while (true)
        {
            var bytesRead = await content.ReadAsync(chunk, cancellationToken);
            if (bytesRead == 0) break;
            if (buffer.Length + bytesRead > MaximumEntryBytes)
                throw new InvalidOperationException("A file inside the archive is larger than 100 MB — extract it locally instead.");
            buffer.Write(chunk, 0, bytesRead);
        }
        buffer.Position = 0;
        return buffer;
    }
}
