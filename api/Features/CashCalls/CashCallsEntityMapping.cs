using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.CashCalls;

internal static class CashCallsEntityMapping
{
    public static CashCall ToModel(this CashCallEntity entity) => new(
        CashCallId: entity.CashCallId,
        ProjectId: entity.ProjectId,
        ValuationClaimId: entity.ValuationClaimId,
        Number: entity.Number,
        Reference: entity.Reference,
        PeriodMonth: entity.PeriodMonth,
        AmountRequested: entity.AmountRequested,
        AmountReceived: entity.AmountReceived,
        Status: (CashCallStatus)entity.Status,
        RequestedAt: entity.RequestedAt,
        InvoicedAt: entity.InvoicedAt,
        ReceivedAt: entity.ReceivedAt);
}
