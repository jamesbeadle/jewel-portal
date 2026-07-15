using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class CreateProgressReportHandler
    : ICommandHandler<CreateProgressReport, ProgressReport>
{
    private readonly JpmsContext context;

    public CreateProgressReportHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgressReport> HandleAsync(CreateProgressReport command, CancellationToken cancellationToken)
    {
        // Only updates that actually belong to the project can be selected onto its report.
        var selectable = await context.ProgressUpdates
            .Where(row => row.ProjectId == command.ProjectId)
            .Select(row => row.ProgressUpdateId)
            .ToListAsync(cancellationToken);
        var selected = command.SelectedUpdateIds
            .Where(selectable.Contains)
            .Distinct()
            .ToList();

        var report = new ProgressReportEntity
        {
            ProgressReportId = ProgressIdentifierFactory.NextProgressReportId(),
            ProjectId = command.ProjectId,
            Title = command.Title.Trim(),
            PeriodStart = command.PeriodStart,
            PeriodEnd = command.PeriodEnd,
            Introduction = command.Introduction.Trim(),
            WorkCompleted = command.WorkCompleted.Trim(),
            UpcomingWorks = command.UpcomingWorks.Trim(),
            CreatedByEmail = command.CreatedByEmail,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.ProgressReports.Add(report);

        var sortOrder = 0;
        foreach (var updateId in selected)
        {
            context.ProgressReportSelections.Add(new ProgressReportSelectionEntity
            {
                ProgressReportSelectionId = ProgressIdentifierFactory.NextProgressReportSelectionId(),
                ProgressReportId = report.ProgressReportId,
                ProgressUpdateId = updateId,
                SortOrder = sortOrder++
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return report.ToModel(selected);
    }
}
