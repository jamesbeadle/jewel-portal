namespace Jewel.JPMS.Features.Procurement;

/// <summary>
/// Who a work order can be placed with — the one pool the manual order form and the Control
/// Centre's staged order share (2026-09-15, Nigel: a work order is raised with a materials/goods
/// SUPPLIER as well as a subcontractor, so the merchant's purchase order is the same record as
/// the trade's and no separate purchase-order feature is needed). Vetted directory records of
/// the Subcontractor and Supplier categories, never prospects, never clients or architects —
/// the same rule the bid-package invite picker and the defects picker apply — by company name.
/// </summary>
public static class WorkOrderCompanies
{
    /// <summary>True for a directory record an order can be placed with.</summary>
    public static bool CanTakeOrders(Subcontractor company) =>
        !company.IsProspect
        && company.Category is DirectoryCategory.Subcontractor or DirectoryCategory.Supplier;

    /// <summary>The picker pool, plus the record already on the order (by id) whatever it is
    /// now, so an existing pick — a company since re-categorised or demoted — never vanishes
    /// from its own dropdown and silently reads as "Choose a company…".</summary>
    public static IEnumerable<Subcontractor> Offered(IEnumerable<Subcontractor>? directory, string? selectedId) =>
        (directory ?? Array.Empty<Subcontractor>())
            .Where(company => CanTakeOrders(company)
                || (!string.IsNullOrWhiteSpace(selectedId)
                    && string.Equals(company.SubcontractorId, selectedId, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(company => company.CompanyName, StringComparer.OrdinalIgnoreCase);

    /// <summary>The label the picker's field carries — neither "Subcontractor" nor "Supplier",
    /// since the pool is both.</summary>
    public const string FieldLabel = "Company";
    public const string ChoosePrompt = "Choose a company…";
}
