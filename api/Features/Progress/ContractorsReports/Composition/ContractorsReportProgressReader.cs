using Jewel.JPMS.Api.Features.Progress.WhatsApp;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>A progress update in the reporting week, with its photographs, as Sections 1 and 9
/// read it.</summary>
public sealed record ContractorsReportUpdate(
    string ProgressUpdateId,
    DateOnly WorkDate,
    string Title,
    string Description,
    IReadOnlyList<ContractorsReportPhoto> Photos);

internal static class ContractorsReportProgressReader
{
    public static async Task<IReadOnlyList<ContractorsReportUpdate>> UpdatesInPeriodAsync(
        JpmsContext context, string projectId, WhatsAppWeek week, CancellationToken cancellationToken)
    {
        var from = new DateTimeOffset(week.Start.AddDays(-1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = new DateTimeOffset(week.End.AddDays(2).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var updates = await context.ProgressUpdates.AsNoTracking()
            .Where(update => update.ProjectId == projectId && update.WorkDate != null && update.WorkDate >= from && update.WorkDate < to)
            .OrderBy(update => update.WorkDate).ThenBy(update => update.CreatedAt)
            .ToListAsync(cancellationToken);
        var updateIds = updates.Select(update => update.ProgressUpdateId).ToList();
        var photosByUpdate = (await context.ProgressPhotos.AsNoTracking()
                .Where(photo => updateIds.Contains(photo.ProgressUpdateId))
                .OrderBy(photo => photo.SortOrder)
                .ToListAsync(cancellationToken))
            .ToLookup(photo => photo.ProgressUpdateId);

        return updates
            .Select(update => new ContractorsReportUpdate(
                update.ProgressUpdateId,
                DateOnly.FromDateTime(update.WorkDate!.Value.Date),
                update.Title,
                update.Description,
                photosByUpdate[update.ProgressUpdateId]
                    .Select(photo => new ContractorsReportPhoto(photo.ProgressPhotoId, photo.FileName, photo.ContentType))
                    .ToList()))
            .Where(update => week.Contains(update.WorkDate))
            .ToList();
    }
}
