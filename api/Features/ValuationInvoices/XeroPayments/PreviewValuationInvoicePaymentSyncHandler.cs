using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.XeroPayments;

/// <summary>The preview is the plan, shown — nothing is written.</summary>
public sealed class PreviewValuationInvoicePaymentSyncHandler : IQueryHandler<PreviewValuationInvoicePaymentSync, ValuationInvoicePaymentSyncPreview>
{
    private readonly JpmsContext context;
    private readonly IXeroClient xero;

    public PreviewValuationInvoicePaymentSyncHandler(JpmsContext context, IXeroClient xero)
    {
        this.context = context;
        this.xero = xero;
    }

    public async Task<ValuationInvoicePaymentSyncPreview> HandleAsync(PreviewValuationInvoicePaymentSync query, CancellationToken cancellationToken) =>
        (await new ValuationInvoicePaymentSyncPlanner(context, xero).PlanAsync(query.ProjectId, cancellationToken)).ToPreview();
}
