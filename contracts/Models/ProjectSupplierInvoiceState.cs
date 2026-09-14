namespace Jewel.JPMS.Models;

/// <summary>
/// Where a received invoice stands, read off Xero's status and the settled fraction. Awaiting
/// approval is a bill Dext published that nobody has approved yet — it counts as received (the
/// supplier has asked for the money) but nothing is owed on it until it is approved.
/// </summary>
public enum ProjectSupplierInvoiceState
{
    AwaitingApproval = 0,
    AwaitingPayment = 1,
    PartPaid = 2,
    Paid = 3
}

/// <summary>
/// How a received invoice reached the account — every bill is one of these, so the reader always
/// knows whether a figure is settled fact or a bill still waiting for a decision.
/// </summary>
public enum ProjectSupplierInvoicePlacement
{
    /// <summary>Allocated to the project on the Xero allocation page (linked to an order or not).</summary>
    Allocated = 0,
    /// <summary>Not yet allocated — its site tracking, the project set on it in the queue, or a
    /// work-order number written on it points at this project.</summary>
    AwaitingAllocation = 1,
    /// <summary>Not yet allocated and pointing nowhere — counted here because this is the only
    /// project the supplier holds live orders on, so it can be nobody else's.</summary>
    AssumedFromSupplier = 2,
    /// <summary>Parked in dispute on the allocation page — received, contested, not yet coded.</summary>
    Disputed = 3
}

public static class ProjectSupplierInvoiceStates
{
    private const string XeroDraft = "DRAFT";
    private const string XeroSubmitted = "SUBMITTED";

    public static ProjectSupplierInvoiceState For(string? xeroStatus, decimal settledFraction)
    {
        if (settledFraction >= 1m) return ProjectSupplierInvoiceState.Paid;
        if (settledFraction > 0m) return ProjectSupplierInvoiceState.PartPaid;
        return IsAwaitingApproval(xeroStatus)
            ? ProjectSupplierInvoiceState.AwaitingApproval
            : ProjectSupplierInvoiceState.AwaitingPayment;
    }

    /// <summary>The same reading the allocation page's "Draft in Xero" chip makes.</summary>
    public static bool IsAwaitingApproval(string? xeroStatus) =>
        string.Equals(xeroStatus, XeroDraft, StringComparison.OrdinalIgnoreCase)
        || string.Equals(xeroStatus, XeroSubmitted, StringComparison.OrdinalIgnoreCase);

    public static string Label(this ProjectSupplierInvoiceState state) => state switch
    {
        ProjectSupplierInvoiceState.AwaitingApproval => "Received, awaiting approval",
        ProjectSupplierInvoiceState.AwaitingPayment => "Approved, awaiting payment",
        ProjectSupplierInvoiceState.PartPaid => "Part paid",
        _ => "Paid"
    };

    public static string Label(this ProjectSupplierInvoicePlacement placement) => placement switch
    {
        ProjectSupplierInvoicePlacement.AwaitingAllocation => "Not yet allocated to the project",
        ProjectSupplierInvoicePlacement.AssumedFromSupplier => "Not yet allocated — no site on the bill; counted here because this is the supplier's only project",
        ProjectSupplierInvoicePlacement.Disputed => "In dispute on the allocation page",
        _ => ""
    };
}
