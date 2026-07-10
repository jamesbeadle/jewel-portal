using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// Deletes a valuation invoice at any status. A Paid invoice's receipt is rolled back out
/// of the project's ValuationInvoicePaidTotal in the same save. When the deleted invoice
/// counted toward "Certified to date" (Issued/Paid), the project's Preapproved claims get
/// their frozen totals re-frozen so the report summary stays truthful.
/// </summary>
public sealed class DeleteValuationInvoiceHandler : ICommandHandler<DeleteValuationInvoice, Acknowledgement>
{
    private readonly JpmsContext context;
    public DeleteValuationInvoiceHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteValuationInvoice command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.FindAsync(new object[] { command.ValuationInvoiceId }, cancellationToken);
        if (entity is null) return new Acknowledgement(command.ValuationInvoiceId); // already gone — idempotent

        if (entity.Status == (int)ValuationInvoiceStatus.Paid && entity.AmountPaid != 0m)
        {
            var project = await context.Projects.FindAsync(new object[] { entity.ProjectId }, cancellationToken);
            if (project is not null) project.ValuationInvoicePaidTotal -= entity.AmountPaid;
        }

        var countedTowardCertified = entity.Status is (int)ValuationInvoiceStatus.Issued or (int)ValuationInvoiceStatus.Paid;
        var projectId = entity.ProjectId;

        context.ValuationInvoices.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        if (countedTowardCertified)
            await PreapprovedClaimTotals.RefreshAsync(context, projectId, cancellationToken);

        return new Acknowledgement(command.ValuationInvoiceId);
    }
}
