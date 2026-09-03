using System.IO.Compression;

namespace Jewel.JPMS.Features.Drawings;

// A zip dropped on the drawings uploader is unpacked IN THE BROWSER into one file per entry, so
// each drawing inside lands on the register by itself — a zip registered whole is a drawing
// nobody can preview, approve or revise. The screen mirrors the Control Centre's archive
// extraction (api ArchiveEntryScreen): tooling junk is skipped silently, everything else comes
// out, nested zips are opened too (one level). Entries are held in memory as IBrowserFile so the
// upload path treats them exactly like a picked file.
public static class DrawingArchiveExpander
{
    public const int MaximumEntries = 200;
    public const long MaximumEntryBytes = 200L * 1024 * 1024;
    public const long MaximumTotalBytes = 1024L * 1024 * 1024;

    public static bool LooksLikeZip(string fileName, string? contentType)
    {
        if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return true;
        var type = contentType ?? "";
        return type.Equals("application/zip", StringComparison.OrdinalIgnoreCase)
            || type.Equals("application/x-zip-compressed", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Unpacks <paramref name="archive"/> into in-memory files. Throws with a plain-English
    /// message when the zip is unreadable, empty, or over the limits.</summary>
    public static async Task<List<IBrowserFile>> ExpandAsync(IBrowserFile archive, CancellationToken cancellationToken)
    {
        if (archive.Size > MaximumTotalBytes)
            throw new InvalidOperationException($"“{archive.Name}” is larger than 1 GB — extract it locally and drop the files in instead.");

        var buffer = new MemoryStream();
        await using (var stream = archive.OpenReadStream(MaximumTotalBytes, cancellationToken))
            await stream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var files = new List<IBrowserFile>();
        Expand(buffer, archive.Name, files, depth: 0);
        if (files.Count == 0)
            throw new InvalidOperationException($"“{archive.Name}” contains no files to add.");
        if (files.Count > MaximumEntries)
            throw new InvalidOperationException($"“{archive.Name}” holds {files.Count} files — the most that can be added from one zip is {MaximumEntries}.");
        return files;
    }

    private static void Expand(Stream zipBytes, string archiveName, List<IBrowserFile> files, int depth)
    {
        ZipArchive zip;
        try { zip = new ZipArchive(zipBytes, ZipArchiveMode.Read, leaveOpen: true); }
        catch (InvalidDataException) { throw new InvalidOperationException($"“{archiveName}” is not a readable zip archive."); }

        using (zip)
        {
            var total = 0L;
            foreach (var entry in zip.Entries)
            {
                if (!IsExtractable(entry)) continue;
                if (entry.Length > MaximumEntryBytes)
                    throw new InvalidOperationException($"“{entry.Name}” inside {archiveName} is larger than 200 MB — extract it locally instead.");
                total += entry.Length;
                if (total > MaximumTotalBytes)
                    throw new InvalidOperationException($"“{archiveName}” unpacks to more than 1 GB — extract it locally instead.");

                var bytes = ReadBounded(entry);
                if (LooksLikeZip(entry.Name, null) && depth < 1)
                {
                    // A zip inside the zip (an email's attachments zipped, say) — open that too.
                    Expand(new MemoryStream(bytes, writable: false), entry.Name, files, depth + 1);
                    continue;
                }
                files.Add(new ExtractedBrowserFile(entry.Name, bytes, ContentTypeFor(entry.Name), entry.LastWriteTime));
            }
        }
    }

    private static byte[] ReadBounded(ZipArchiveEntry entry)
    {
        using var content = entry.Open();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = content.Read(chunk, 0, chunk.Length)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > MaximumEntryBytes)  // a crafted header can lie about Length
                throw new InvalidOperationException($"“{entry.Name}” is larger than 200 MB — extract it locally instead.");
        }
        return buffer.ToArray();
    }

    private static bool IsExtractable(ZipArchiveEntry entry)
    {
        if (entry.Name.Length == 0) return false;                       // directory entries
        if (entry.Length == 0) return false;
        if (entry.FullName.StartsWith("__MACOSX/", StringComparison.OrdinalIgnoreCase)) return false;
        if (entry.Name.StartsWith('.')) return false;                   // .DS_Store and friends
        if (entry.Name.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    public static string ContentTypeFor(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
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

    /// <summary>A file unpacked from a zip, held in memory — readable any number of times, unlike
    /// a picked file whose handle dies with the next pick.</summary>
    private sealed class ExtractedBrowserFile : IBrowserFile
    {
        private readonly byte[] bytes;

        public ExtractedBrowserFile(string name, byte[] bytes, string contentType, DateTimeOffset lastModified)
        {
            Name = name;
            this.bytes = bytes;
            ContentType = contentType;
            LastModified = lastModified;
        }

        public string Name { get; }
        public DateTimeOffset LastModified { get; }
        public long Size => bytes.Length;
        public string ContentType { get; }

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            if (bytes.Length > maxAllowedSize)
                throw new IOException($"Supplied file with size {bytes.Length} bytes exceeds the maximum of {maxAllowedSize} bytes.");
            return new MemoryStream(bytes, writable: false);
        }
    }
}
