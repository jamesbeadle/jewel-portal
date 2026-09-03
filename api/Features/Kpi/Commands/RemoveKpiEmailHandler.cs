using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Kpi;

namespace Jewel.JPMS.Api.Features.Kpi.Commands;

// Deletes the KPI row. The email keeps its JPMS/Admin tag (an administrator did deal with it;
// the Tagged tab's remove-tag returns it to the queue if wanted). Audited by reference only.
public sealed class RemoveKpiEmailHandler : ICommandHandler<RemoveKpiEmail, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;
    public RemoveKpiEmailHandler(JpmsContext context, AuditTrail audit) { this.context = context; this.audit = audit; }

    public async Task<Acknowledgement> HandleAsync(RemoveKpiEmail command, CancellationToken cancellationToken)
    {
        var entity = await context.KpiEmails.FirstOrDefaultAsync(row => row.KpiEmailId == command.KpiEmailId, cancellationToken)
            ?? throw new InvalidOperationException($"KPI '{command.KpiEmailId}' not found.");
        var reference = entity.Reference;
        context.KpiEmails.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            AuditEventType.KpiEmailRemoved,
            $"{reference} removed",
            recordReference: reference,
            cancellationToken: cancellationToken);

        return new Acknowledgement(command.KpiEmailId);
    }
}
