using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>Reads a week of the export for one project: the sender lists from Project settings,
/// the export's messages and photographs, and the updates already on the week's days.</summary>
public sealed class WhatsAppWeekReader
{
    private readonly JpmsContext context;

    public WhatsAppWeekReader(JpmsContext context) { this.context = context; }

    public async Task<WhatsAppWeekReading> ReadAsync(
        string projectId, WhatsAppExportArchive archive, WhatsAppWeek week, CancellationToken cancellationToken)
    {
        var senders = await SendersAsync(projectId, cancellationToken);
        var reading = WhatsAppExportParser.Parse(archive.ChatText);
        var plan = WhatsAppWeekPlanner.Plan(reading.Messages, week, senders);
        var photos = WhatsAppWeekPhotos.Read(plan, archive);
        var existing = await ExistingUpdatesAsync(projectId, week, cancellationToken);
        return new WhatsAppWeekReading(plan, photos, reading.OmittedMediaCount, archive.CarriesMedia, senders.HasAnyoneListed, existing);
    }

    private async Task<WhatsAppSenderAttribution> SendersAsync(string projectId, CancellationToken cancellationToken)
    {
        var lists = await context.Projects.AsNoTracking()
            .Where(project => project.SiteNoteSenderNames != null)
            .Select(project => new { project.ProjectId, project.SiteNoteSenderNames })
            .ToListAsync(cancellationToken);
        var thisProject = lists
            .Where(list => list.ProjectId == projectId)
            .SelectMany(list => SiteNoteSenders.Parse(list.SiteNoteSenderNames))
            .ToList();
        var otherProjects = lists
            .Where(list => list.ProjectId != projectId)
            .SelectMany(list => SiteNoteSenders.Parse(list.SiteNoteSenderNames))
            .ToList();
        return new WhatsAppSenderAttribution(thisProject, otherProjects);
    }

    private async Task<IReadOnlyDictionary<DateOnly, IReadOnlyList<string>>> ExistingUpdatesAsync(
        string projectId, WhatsAppWeek week, CancellationToken cancellationToken)
    {
        var from = new DateTimeOffset(week.Start.AddDays(-1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = new DateTimeOffset(week.End.AddDays(2).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var updates = await context.ProgressUpdates.AsNoTracking()
            .Where(update => update.ProjectId == projectId && update.WorkDate != null && update.WorkDate >= from && update.WorkDate < to)
            .Select(update => new { update.WorkDate, update.Title })
            .ToListAsync(cancellationToken);
        return updates
            .Select(update => (Day: DateOnly.FromDateTime(update.WorkDate!.Value.Date), update.Title))
            .Where(update => week.Contains(update.Day))
            .GroupBy(update => update.Day)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.Select(update => update.Title).ToList());
    }
}
