using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Removes a snapshot (and its lines) taken in error. Never touches live report data. Any invoice
/// pointing at the snapshot has its link cleared so the UI stops offering a dead "View report".
/// </summary>
public sealed class DeleteValuationReportSnapshotHandler : ICommandHandler<DeleteValuationReportSnapshot, Acknowledgement>
{
    private readonly JpmsContext context;
    public DeleteValuationReportSnapshotHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteValuationReportSnapshot command, CancellationToken cancellationToken)
    {
        var snapshot = await context.ValuationReportSnapshots
            .FindAsync(new object[] { command.ValuationReportSnapshotId }, cancellationToken);
        if (snapshot is null) return new Acknowledgement(command.ValuationReportSnapshotId); // already gone — idempotent

        var lines = await context.ValuationReportSnapshotLines
            .Where(line => line.ValuationReportSnapshotId == snapshot.ValuationReportSnapshotId)
            .ToListAsync(cancellationToken);
        context.ValuationReportSnapshotLines.RemoveRange(lines);

        var linkedInvoices = await context.ValuationInvoices
            .Where(invoice => invoice.ValuationReportSnapshotId == snapshot.ValuationReportSnapshotId)
            .ToListAsync(cancellationToken);
        foreach (var invoice in linkedInvoices) invoice.ValuationReportSnapshotId = null;

        context.ValuationReportSnapshots.Remove(snapshot);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.ValuationReportSnapshotId);
    }
}
