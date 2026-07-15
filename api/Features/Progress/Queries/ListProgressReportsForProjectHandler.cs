using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Progress.Queries;

public sealed class ListProgressReportsForProjectHandler
    : IQueryHandler<ListProgressReportsForProject, IReadOnlyList<ProgressReport>>
{
    private readonly JpmsContext context;

    public ListProgressReportsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ProgressReport>> HandleAsync(
        ListProgressReportsForProject query, CancellationToken cancellationToken)
    {
        var reports = await context.ProgressReports
            .Where(row => row.ProjectId == query.ProjectId)
            .OrderByDescending(row => row.CreatedAt)
            .ToListAsync(cancellationToken);

        var reportIds = reports.Select(report => report.ProgressReportId).ToList();
        var selectionsByReport = (await context.ProgressReportSelections
                .Where(row => reportIds.Contains(row.ProgressReportId))
                .OrderBy(row => row.SortOrder)
                .ToListAsync(cancellationToken))
            .GroupBy(row => row.ProgressReportId)
            .ToDictionary(group => group.Key, group => group.Select(row => row.ProgressUpdateId).ToList());

        return reports
            .Select(report => report.ToModel(
                selectionsByReport.TryGetValue(report.ProgressReportId, out var selected)
                    ? selected
                    : new List<string>()))
            .ToList();
    }
}
