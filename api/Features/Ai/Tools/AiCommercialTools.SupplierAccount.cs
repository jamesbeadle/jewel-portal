using Jewel.JPMS.Api.Features.Subcontractors;
using Jewel.JPMS.Contracts.Commercial;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiCommercialTools
{
    public const string GetProjectSupplierAccountName = "get_project_supplier_account";
    private const string SupplierArgument = "supplier";

    private static readonly int[] LiveOrderStatuses = { (int)WorkOrderStatus.Released, (int)WorkOrderStatus.Complete };

    private static IEnumerable<AiTool> SupplierAccountTool()
    {
        return new AiTool[]
        {
            new(
                GetProjectSupplierAccountName,
                "One supplier's account on one project, as the Work orders tab's Supplier account and its "
                + "PDF read it: the live work orders they hold there with their priced lines, invoiced and "
                + "linked, paid and left to invoice per order; then EVERY invoice received from them for the "
                + "project — linked to an order or not, awaiting approval included — with its CIS labour / "
                + "materials split, Xero status and the order(s) it is matched to; and the position: received "
                + "less ordered, positive being an over-invoice to query before it is paid. Use this for 'has "
                + "<supplier> over-invoiced', 'what is left to invoice on their orders', 'which invoices sit "
                + "against WO-0024' — quote its figures, never re-add them.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view.", false),
                    (SupplierArgument, "string", "The supplier: a directory record id (subcontractorId) or the company name as the Work orders tab shows it.", true)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                RunSupplierAccountAsync)
        };
    }

    private static async Task<string> RunSupplierAccountAsync(AiToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        var projectId = AiToolSchema.Text(input, "projectId") ?? context.Scope?.ProjectId;
        if (string.IsNullOrWhiteSpace(projectId))
            return Fail("Say which project: pass projectId (list_projects returns ids) or have the user open one of its pages.");
        var wanted = AiToolSchema.Text(input, SupplierArgument)?.Trim();
        if (string.IsNullOrWhiteSpace(wanted)) return Fail("Say which supplier: a directory record id or the company name.");

        var candidates = await SuppliersWithLiveOrdersAsync(context, projectId, cancellationToken);
        var matches = candidates.Where(candidate => IsSupplierMatch(candidate, wanted)).ToList();
        if (matches.Count > 1)
            return Fail($"\"{wanted}\" matches several suppliers on this project: {string.Join(", ", matches.Select(match => $"{match.CompanyName} ({match.SubcontractorId})"))}. Pass the subcontractorId.");
        if (matches.Count == 0)
            return Fail($"No supplier holding a live work order on this project matches \"{wanted}\". Suppliers here: {string.Join(", ", candidates.Select(candidate => candidate.CompanyName))}.");

        var account = await context.Services
            .GetRequiredService<IQueryHandler<GetProjectSupplierAccount, ProjectSupplierAccount>>()
            .HandleAsync(new GetProjectSupplierAccount(projectId, matches[0].SubcontractorId), cancellationToken);
        return Serialise(ShapeSupplierAccount(account));
    }

    private sealed record SupplierCandidate(string SubcontractorId, string CompanyName);

    private static async Task<List<SupplierCandidate>> SuppliersWithLiveOrdersAsync(AiToolContext context, string projectId, CancellationToken cancellationToken)
    {
        var supplierIds = await context.Db.WorkOrders.AsNoTracking()
            .Where(order => order.ProjectId == projectId && LiveOrderStatuses.Contains(order.Status))
            .Select(order => order.SubcontractorId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return await context.Db.Subcontractors.AsNoTracking()
            .Where(company => supplierIds.Contains(company.SubcontractorId))
            .OrderBy(company => company.CompanyName)
            .Select(company => new SupplierCandidate(company.SubcontractorId, company.CompanyName))
            .ToListAsync(cancellationToken);
    }

    // The id outright, the exact name, or the directory's own fuzzy rule — the same reading that
    // pairs a Xero contact with a directory record, so "S Williams" finds "S Williams Plumbing & Heating".
    private static bool IsSupplierMatch(SupplierCandidate candidate, string wanted) =>
        string.Equals(candidate.SubcontractorId, wanted, StringComparison.OrdinalIgnoreCase)
        || string.Equals(candidate.CompanyName, wanted, StringComparison.OrdinalIgnoreCase)
        || DirectoryXeroMatcher.Matches(candidate.CompanyName, wanted);
}
