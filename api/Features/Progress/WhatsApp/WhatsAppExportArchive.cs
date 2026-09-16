using System.IO.Compression;
using System.Text;

namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>
/// The export as it arrived: the chat text, and — when it came as WhatsApp's zip — the media
/// files beside it, read one at a time by the name the chat refers to them by. Pasted text has
/// no media; every photograph it names is then "missing".
/// </summary>
public sealed class WhatsAppExportArchive : IDisposable
{
    private const string ChatExtension = ".txt";

    private readonly ZipArchive? zip;
    private readonly Dictionary<string, ZipArchiveEntry> entriesByName;

    private WhatsAppExportArchive(string chatText, ZipArchive? zip, Dictionary<string, ZipArchiveEntry> entriesByName)
    {
        ChatText = chatText;
        this.zip = zip;
        this.entriesByName = entriesByName;
    }

    public string ChatText { get; }

    public bool CarriesMedia => zip is not null;

    public static WhatsAppExportArchive FromText(string chatText) =>
        new(chatText, null, new Dictionary<string, ZipArchiveEntry>());

    /// <summary>Opens WhatsApp's "Export chat" zip: the one .txt inside is the chat, everything
    /// else is media. Throws <see cref="InvalidDataException"/> when there is no chat in it.</summary>
    public static WhatsAppExportArchive FromZip(byte[] bytes)
    {
        var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var entries = zip.Entries
            .Where(entry => entry.Name.Length > 0)
            .GroupBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var chat = entries.Values
            .Where(entry => entry.Name.EndsWith(ChatExtension, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(entry => entry.Length)
            .FirstOrDefault();
        if (chat is null)
        {
            zip.Dispose();
            throw new InvalidDataException("The zip holds no chat text (.txt) — export the chat from WhatsApp with \"Export chat\", which writes the messages and their media together.");
        }

        using var reader = new StreamReader(chat.Open(), Encoding.UTF8);
        return new WhatsAppExportArchive(reader.ReadToEnd(), zip, entries);
    }

    /// <summary>The media file's bytes, or null when the export does not carry it.</summary>
    public byte[]? ReadMedia(string fileName)
    {
        if (!entriesByName.TryGetValue(fileName, out var entry)) return null;
        using var stream = entry.Open();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    public void Dispose() => zip?.Dispose();
}
