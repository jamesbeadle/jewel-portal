using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>Everything the preview shows and the apply acts on, read once from the export and the
/// project: the plan, the photographs, what the parser set aside, and the updates the project
/// already holds on the week's days.</summary>
public sealed record WhatsAppWeekReading(
    WhatsAppWeekPlan Plan,
    WhatsAppWeekPhotos Photos,
    int OmittedMediaCount,
    bool ExportCarriesMedia,
    bool ProjectListsAnySender,
    IReadOnlyDictionary<DateOnly, IReadOnlyList<string>> ExistingUpdateTitlesByDay)
{
    public IReadOnlyList<string> ExistingOn(DateOnly day) =>
        ExistingUpdateTitlesByDay.TryGetValue(day, out var titles) ? titles : Array.Empty<string>();

    public WhatsAppWeekPreview ToPreview(string projectId) => new(
        projectId,
        Plan.Week.Start,
        Plan.Week.End,
        Plan.Days.Select(ToPreviewDay).ToList(),
        Plan.Unattributed.Select(ToUnattributed).ToList(),
        Photos.Repeated,
        Plan.OtherSiteMessageCount,
        Plan.OtherSiteSenders,
        Plan.MessagesOutsideWeek,
        Photos.MissingFileNames,
        Notes());

    private WhatsAppWeekDay ToPreviewDay(WhatsAppPlannedDay day) => new(
        day.Date,
        WhatsAppDayText.Title(day.Date),
        WhatsAppDayText.Description(day.Messages),
        day.Messages.Count,
        Photos.On(day.Date).Select(photo => photo.FileName).ToList(),
        ExistingOn(day.Date));

    private static WhatsAppUnattributedMessage ToUnattributed(WhatsAppMessage message) => new(
        message.Day,
        message.SentAt.ToString("HH:mm"),
        message.Sender,
        message.Text,
        message.HasMedia ? 1 : 0);

    private IReadOnlyList<string> Notes()
    {
        var notes = new List<string>();
        if (!ProjectListsAnySender)
            notes.Add("This project lists no site-note senders yet — add the site manager's WhatsApp name under Project settings → Edit details, or every message lands in the review list.");
        if (!ExportCarriesMedia)
            notes.Add("The chat came as text, so it carries no photographs — drop the WhatsApp zip to bring them in.");
        if (OmittedMediaCount > 0)
            notes.Add($"{OmittedMediaCount} media item(s) were omitted from the export itself (\"<Media omitted>\") — export again with media to bring them in.");
        if (Photos.NonPhotoCount > 0)
            notes.Add($"{Photos.NonPhotoCount} file(s) that are not photographs (video, voice notes, documents) were left out.");
        return notes;
    }
}
