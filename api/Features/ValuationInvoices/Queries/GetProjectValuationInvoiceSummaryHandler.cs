using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Queries;

public sealed class GetProjectValuationInvoiceSummaryHandler : IQueryHandler<GetProjectValuationInvoiceSummary, ProjectValuationInvoiceSummary>
{
    private readonly JpmsContext context;
    public GetProjectValuationInvoiceSummaryHandler(JpmsContext context) { this.context = context; }

    public async Task<ProjectValuationInvoiceSummary> HandleAsync(GetProjectValuationInvoiceSummary query, CancellationToken cancellationToken)
    {
        // Cancelled invoices are audit-trail residents only — they never count anywhere.
        var invoices = await context.ValuationInvoices
            .Where(invoice => invoice.ProjectId == query.ProjectId
                              && invoice.Status != (int)ValuationInvoiceStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var raised = invoices.Sum(invoice => invoice.Amount);
        var invoiced = invoices
            .Where(invoice => invoice.Status is (int)ValuationInvoiceStatus.Issued or (int)ValuationInvoiceStatus.Paid)
            .Sum(invoice => invoice.Amount);
        var paid = invoices.Sum(invoice => invoice.AmountPaid);
        var awaitingApproval = invoices
            .Where(invoice => invoice.Status is (int)ValuationInvoiceStatus.Submitted or (int)ValuationInvoiceStatus.Approved)
            .Sum(invoice => invoice.Amount);

        return new ProjectValuationInvoiceSummary(
            ProjectId: query.ProjectId,
            TotalRaised: raised,
            TotalInvoiced: invoiced,
            TotalPaid: paid,
            Outstanding: invoiced - paid,
            TotalAwaitingApproval: awaitingApproval);
    }
}
