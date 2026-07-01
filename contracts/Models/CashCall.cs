namespace Jewel.JPMS.Models;

// Lifecycle of a monthly cash call: Requested (called up), Invoiced (client invoice prepared —
// reduces the amount available), Received (client has paid — increases the project cash-call total).
public enum CashCallStatus
{
    Requested = 0,
    Invoiced = 1,
    Received = 2
}

// A monthly cash call: the demand for funds raised against the current valuation/CVR. Drawn from a
// valuation claim when one is linked. When received, its amount rolls into the project-level total.
public sealed record CashCall(
    string CashCallId,
    string ProjectId,
    string? ValuationClaimId,
    int Number,
    string Reference,             // e.g. "CC-0001"
    DateTimeOffset PeriodMonth,   // the month this call covers
    decimal AmountRequested,
    decimal AmountReceived,
    CashCallStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? InvoicedAt = null,
    DateTimeOffset? ReceivedAt = null)
{
    public string DisplayNumber => Number > 0 ? $"CC-{Number:0000}" : "";
}

// Project-level roll-up of cash calls for the directors' view.
public sealed record ProjectCashCallSummary(
    string ProjectId,
    decimal TotalRequested,   // sum of amounts called up
    decimal TotalReceived,    // sum of amounts the client has paid (the project cash-call total)
    decimal Outstanding);     // requested but not yet received
