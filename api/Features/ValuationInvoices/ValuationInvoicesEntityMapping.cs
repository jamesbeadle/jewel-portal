using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.ValuationInvoices;

internal static class ValuationInvoicesEntityMapping
{
    public static ValuationInvoice ToModel(this ValuationInvoiceEntity entity) => new(
        ValuationInvoiceId: entity.ValuationInvoiceId,
        ProjectId: entity.ProjectId,
        ValuationClaimId: entity.ValuationClaimId,
        Number: entity.Number,
        Reference: entity.Reference,
        PeriodMonth: entity.PeriodMonth,
        Amount: entity.Amount,
        AmountPaid: entity.AmountPaid,
        Status: (ValuationInvoiceStatus)entity.Status,
        RaisedAt: entity.RaisedAt,
        IssuedAt: entity.IssuedAt,
        PaidAt: entity.PaidAt);
}
