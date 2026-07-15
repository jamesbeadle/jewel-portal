using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Progress;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>Deletes a report and its selection rows. The underlying progress updates and their
/// photos are untouched — they remain available for other reports.</summary>
public sealed class DeleteProgressReportHandler
    : ICommandHandler<DeleteProgressReport, Acknowledgement>
{
    private readonly JpmsContext context;

    public DeleteProgressReportHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteProgressReport command, CancellationToken cancellationToken)
    {
        var report = await context.ProgressReports.FindAsync(new object[] { command.ProgressReportId }, cancellationToken);
        if (report is null) throw new InvalidOperationException($"Progress report {command.ProgressReportId} not found.");

        var selections = await context.ProgressReportSelections
            .Where(row => row.ProgressReportId == command.ProgressReportId)
            .ToListAsync(cancellationToken);

        context.ProgressReportSelections.RemoveRange(selections);
        context.ProgressReports.Remove(report);
        await context.SaveChangesAsync(cancellationToken);

        return new Acknowledgement(command.ProgressReportId);
    }
}
