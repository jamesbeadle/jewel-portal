namespace Jewel.JPMS.Api.Features.Commercial.Queries;

/// <summary>
/// Whether a bill nobody has allocated yet belongs on a supplier's account for a project, and how
/// it got there. The precedence is the allocation page's own: the project already decided on the
/// bill in the queue (the "Set project" half-step), else the project its Xero Sites tracking names;
/// failing both, a work-order number the supplier wrote on the bill (the purchase order prints it);
/// failing that, the bill is counted only when the supplier holds live orders on no other project,
/// because then it can be nobody else's — and the account says so on the row. A bill that points at
/// another project, or names only another project's orders, is not this project's and is left out.
/// Pure, so the rule is unit-tested without a ledger.
/// </summary>
internal static class SupplierBillPlacement
{
    public static ProjectSupplierInvoicePlacement? Decide(
        string projectId,
        string? hintedProjectId,
        IReadOnlyList<int> orderNumbersOnBill,
        IReadOnlySet<int> projectOrderNumbers,
        bool isSupplierOnlyHere)
    {
        if (hintedProjectId is not null)
            return IsThisProject(projectId, hintedProjectId) ? ProjectSupplierInvoicePlacement.AwaitingAllocation : null;

        if (orderNumbersOnBill.Count > 0)
            return orderNumbersOnBill.Any(projectOrderNumbers.Contains) ? ProjectSupplierInvoicePlacement.AwaitingAllocation : null;

        return isSupplierOnlyHere ? ProjectSupplierInvoicePlacement.AssumedFromSupplier : null;
    }

    private static bool IsThisProject(string projectId, string hintedProjectId) =>
        string.Equals(projectId, hintedProjectId, StringComparison.OrdinalIgnoreCase);
}
