using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Procurement.Documents;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Acceptance;

/// <summary>
/// Everything the public acceptance page can do, keyed by the order's token and nothing else —
/// the same shape as ImaginePublicService. An unknown token answers null and the endpoint turns
/// that into a 404 with no detail; a refusal throws InvalidOperationException with the sentence
/// the page shows. The view is the purchase order as the PDF prints it (WorkOrderPoDocumentBuilder,
/// so the page and the attachment can never disagree) plus the directory contact the link went to.
/// </summary>
public sealed class WorkOrderAcceptanceService
{
    private const int MaxNameLength = 256;
    private const string NameRequired = "Please enter your name to accept the work order.";
    private const string OrderClosed = "This work order is no longer open for acceptance — please contact Jewel Bespoke Build.";

    private readonly JpmsContext context;
    private readonly AuditTrail audit;

    public WorkOrderAcceptanceService(JpmsContext context, AuditTrail audit)
    {
        this.context = context;
        this.audit = audit;
    }

    public async Task<WorkOrderAcceptanceView?> ViewAsync(string token, CancellationToken cancellationToken)
    {
        var order = await FindOrderAsync(token, cancellationToken);
        if (order is null) return null;
        return await ViewOfAsync(order, cancellationToken);
    }

    public async Task<WorkOrderAcceptanceView?> AcceptAsync(string token, WorkOrderAcceptanceSignature signature, CancellationToken cancellationToken)
    {
        var order = await FindOrderAsync(token, cancellationToken);
        if (order is null) return null;
        var name = (signature.Name ?? "").Trim();
        if (name.Length == 0) throw new InvalidOperationException(NameRequired);
        var supplier = await SupplierOfAsync(order, cancellationToken);

        var wasAcceptedAlready = WorkOrderAcceptance.IsAccepted(order);
        var isStamped = WorkOrderAcceptance.TryStamp(order, Clip(name), supplier?.ContactEmail ?? "", DateTimeOffset.UtcNow);
        if (!isStamped) throw new InvalidOperationException(OrderClosed);
        await context.SaveChangesAsync(cancellationToken);

        if (!wasAcceptedAlready) await RecordAcceptanceAsync(order, cancellationToken);
        return await ViewOfAsync(order, cancellationToken);
    }

    private Task<WorkOrderEntity?> FindOrderAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) return Task.FromResult<WorkOrderEntity?>(null);
        return context.WorkOrders.FirstOrDefaultAsync(row => row.AcceptanceToken == token, cancellationToken);
    }

    private Task<SubcontractorEntity?> SupplierOfAsync(WorkOrderEntity order, CancellationToken cancellationToken) =>
        context.Subcontractors.AsNoTracking()
            .FirstOrDefaultAsync(row => row.SubcontractorId == order.SubcontractorId, cancellationToken);

    private async Task<WorkOrderAcceptanceView> ViewOfAsync(WorkOrderEntity order, CancellationToken cancellationToken)
    {
        var document = await WorkOrderPoDocumentBuilder.BuildAsync(context, order.WorkOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Work order {order.WorkOrderId} not found.");
        var supplier = await SupplierOfAsync(order, cancellationToken);
        return new WorkOrderAcceptanceView(
            document.Order, document.Lines, document.SupplierName, document.SupplierContactName,
            supplier?.ContactEmail ?? "", document.SupplierAddressLines, document.ProjectName,
            document.SiteAddressLines, document.ApprovedByName, document.PaymentTermsDays);
    }

    private Task RecordAcceptanceAsync(WorkOrderEntity order, CancellationToken cancellationToken) =>
        audit.WriteAsync(
            AuditEventType.WorkOrderAccepted,
            detail: $"{order.Reference} accepted by {order.AcceptedByName} from the acceptance link in the purchase-order email",
            projectId: order.ProjectId,
            recordType: RecordType.WorkOrder,
            recordId: order.WorkOrderId,
            recordReference: order.Reference,
            actorEmail: order.AcceptedByEmail,
            cancellationToken: cancellationToken);

    private static string Clip(string name) => name.Length <= MaxNameLength ? name : name[..MaxNameLength];
}
