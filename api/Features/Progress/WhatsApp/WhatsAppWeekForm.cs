using System.Globalization;

namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>
/// The multipart form both intake endpoints take: <c>weekEnding</c> (the Thursday, yyyy-MM-dd),
/// and the export as either <c>chatText</c> or one file — WhatsApp's zip, or the .txt alone —
/// and, on apply, <c>days</c> (yyyy-MM-dd, comma-separated). A refusal names what is wrong.
/// </summary>
internal static class WhatsAppWeekForm
{
    private const string ZipExtension = ".zip";
    private const long MaxExportBytes = 200L * 1024 * 1024;

    public sealed record Read(WhatsAppExportArchive? Archive, WhatsAppWeek? Week, IReadOnlyList<DateOnly> Days, string? Refusal);

    public static async Task<Read> ReadAsync(IFormCollection form, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(form["weekEnding"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var weekEnding))
            return Refuse("weekEnding is required, as yyyy-MM-dd — the Thursday the week ends on.");
        if (weekEnding.DayOfWeek != DayOfWeek.Thursday)
            return Refuse($"{weekEnding:dddd d MMMM yyyy} is not a Thursday — the week runs Friday to Thursday.");
        var week = WhatsAppWeek.EndingOn(weekEnding);

        var days = ReadDays(form["days"].ToString());
        if (days is null) return Refuse("days must be dates, yyyy-MM-dd, comma-separated.");

        var archive = await ReadArchiveAsync(form, cancellationToken);
        return archive.Refusal is not null
            ? Refuse(archive.Refusal)
            : new Read(archive.Archive, week, days, null);
    }

    private static Read Refuse(string reason) => new(null, null, Array.Empty<DateOnly>(), reason);

    private static IReadOnlyList<DateOnly>? ReadDays(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<DateOnly>();
        var days = new List<DateOnly>();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!DateOnly.TryParseExact(part, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)) return null;
            days.Add(day);
        }
        return days;
    }

    private static async Task<(WhatsAppExportArchive? Archive, string? Refusal)> ReadArchiveAsync(IFormCollection form, CancellationToken cancellationToken)
    {
        var file = form.Files.FirstOrDefault(candidate => candidate.Length > 0);
        if (file is null)
        {
            var chatText = form["chatText"].ToString();
            return string.IsNullOrWhiteSpace(chatText)
                ? (null, "Paste the exported chat or drop WhatsApp's export zip.")
                : (WhatsAppExportArchive.FromText(chatText), null);
        }
        if (file.Length > MaxExportBytes)
            return (null, $"The export is {file.Length / 1_048_576.0:0.#} MB — larger than the {MaxExportBytes / 1_048_576} MB an export may be.");

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        if (!file.FileName.EndsWith(ZipExtension, StringComparison.OrdinalIgnoreCase))
            return (WhatsAppExportArchive.FromText(System.Text.Encoding.UTF8.GetString(buffer.ToArray())), null);
        try
        {
            return (WhatsAppExportArchive.FromZip(buffer.ToArray()), null);
        }
        catch (InvalidDataException ex)
        {
            return (null, ex.Message);
        }
    }
}
