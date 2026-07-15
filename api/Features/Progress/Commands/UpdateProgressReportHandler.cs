using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>Replaces the report's narrative sections and its selections (the selection rows are
/// rewritten wholesale — order comes from the command's list order).</summary>
public sealed class UpdateProgressReportHandler
    : ICommandHandler<UpdateProgressReport, ProgressReport>
{
    private readonly JpmsContext context;

    public UpdateProgressReportHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgressReport> HandleAsync(UpdateProgressReport command, CancellationToken cancellationToken)
    {
        var report = await context.ProgressReports.FindAsync(new object[] { command.ProgressReportId }, cancellationToken);
        if (report is null) throw new InvalidOperationException($"Progress report {command.ProgressReportId} not found.");

        report.Title = command.Title.Trim();
        report.PeriodStart = command.PeriodStart;
        report.PeriodEnd = command.PeriodEnd;
        report.Introduction = command.Introduction.Trim();
        report.WorkCompleted = command.WorkCompleted.Trim();
        report.UpcomingWorks = command.UpcomingWorks.Trim();

        var selectable = await context.ProgressUpdates
            .Where(row => row.ProjectId == report.ProjectId)
            .Select(row => row.ProgressUpdateId)
            .ToListAsync(cancellationToken);
        var selected = command.SelectedUpdateIds
            .Where(selectable.Contains)
            .Distinct()
            .ToList();

        var existingSelections = await context.ProgressReportSelections
            .Where(row => row.ProgressReportId == command.ProgressReportId)
            .ToListAsync(cancellationToken);
        context.ProgressReportSelections.RemoveRange(existingSelections);

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
