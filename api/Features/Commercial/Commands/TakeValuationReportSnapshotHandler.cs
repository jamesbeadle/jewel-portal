using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class TakeValuationReportSnapshotHandler : ICommandHandler<TakeValuationReportSnapshot, ValuationReportSnapshot>
{
    private readonly JpmsContext context;
    public TakeValuationReportSnapshotHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationReportSnapshot> HandleAsync(TakeValuationReportSnapshot command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var snapshot = await ValuationReportSnapshotCapture.CaptureAsync(
            context, command.ProjectId, command.Label, command.ValuationInvoiceId, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return snapshot.ToModel();
    }
}
