using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>
/// The week's photographs, day by day, read out of the export: each day's media in the order it
/// was sent, a photograph that appears on more than one day kept on the first and dropped from
/// the rest (judged by content, so a re-sent file with a new name is still the same picture), a
/// file the export does not carry listed as missing, and media that is not a photograph — video,
/// voice notes, documents — left out and counted.
/// </summary>
public sealed record WhatsAppWeekPhotos(
    IReadOnlyDictionary<DateOnly, IReadOnlyList<IncomingProgressPhoto>> ByDay,
    IReadOnlyList<WhatsAppRepeatedPhoto> Repeated,
    IReadOnlyList<string> MissingFileNames,
    int NonPhotoCount)
{
    public IReadOnlyList<IncomingProgressPhoto> On(DateOnly day) =>
        ByDay.TryGetValue(day, out var photos) ? photos : Array.Empty<IncomingProgressPhoto>();

    public static WhatsAppWeekPhotos Read(WhatsAppWeekPlan plan, WhatsAppExportArchive archive)
    {
        var byDay = new Dictionary<DateOnly, IReadOnlyList<IncomingProgressPhoto>>();
        var firstDayByHash = new Dictionary<string, DateOnly>(StringComparer.Ordinal);
        var repeats = new Dictionary<string, (DateOnly KeptOn, List<DateOnly> DroppedFrom)>(StringComparer.OrdinalIgnoreCase);
        var missing = new List<string>();
        var nonPhotos = 0;

        foreach (var day in plan.Days)
        {
            var photos = new List<IncomingProgressPhoto>();
            foreach (var fileName in day.MediaFileNames)
            {
                if (!ProgressPhotoFormats.IsAccepted(fileName, null)) { nonPhotos++; continue; }
                var bytes = archive.ReadMedia(fileName);
                if (bytes is null) { missing.Add(fileName); continue; }
                var hash = ProgressPhotoContentHash.Of(bytes);
                if (firstDayByHash.TryGetValue(hash, out var keptOn) && keptOn != day.Date)
                {
                    NoteRepeat(repeats, fileName, keptOn, day.Date);
                    continue;
                }
                firstDayByHash.TryAdd(hash, day.Date);
                photos.Add(new IncomingProgressPhoto(fileName, null, bytes));
            }
            if (photos.Count > 0) byDay[day.Date] = photos;
        }

        var repeated = repeats
            .Select(pair => new WhatsAppRepeatedPhoto(pair.Key, pair.Value.KeptOn, pair.Value.DroppedFrom))
            .ToList();
        return new WhatsAppWeekPhotos(byDay, repeated, missing.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), nonPhotos);
    }

    private static void NoteRepeat(
        Dictionary<string, (DateOnly KeptOn, List<DateOnly> DroppedFrom)> repeats, string fileName, DateOnly keptOn, DateOnly droppedFrom)
    {
        if (!repeats.TryGetValue(fileName, out var repeat))
        {
            repeat = (keptOn, new List<DateOnly>());
            repeats[fileName] = repeat;
        }
        if (!repeat.DroppedFrom.Contains(droppedFrom)) repeat.DroppedFrom.Add(droppedFrom);
    }
}
