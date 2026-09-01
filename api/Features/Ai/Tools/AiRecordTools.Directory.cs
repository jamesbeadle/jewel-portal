using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.RecordLinks;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiRecordTools
{
    private static IEnumerable<AiTool> DirectoryTools()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new AiTool[]
        {
            new(
                "search_directory",
                "Finds company records in the subcontractor/supplier directory: search by name (or "
                + "contact name/email), optionally narrowed to a category. Returns each match's "
                + "subcontractorId — the id update_subcontractor and the procurement actions need — "
                + "with its category, trades (ids and names, exactly what update_subcontractor must "
                + "send back in full), primary contact, postal address, CIS status, payment terms, "
                + "whether it is linked to a Xero contact, and whether it is still a tender-only "
                + "prospect (promote_subcontractor_to_directory makes those permanent). Call this "
                + "BEFORE update_subcontractor or add_subcontractor_to_directory — never guess an "
                + "id, and never create a record before checking it isn't already here.",
                AiToolSchema.Object(
                    ("search", "string",
                        "Text matched against company name, contact name and contact email — "
                        + "\"Sussex Tiling\", \"jo@acme\". Left out, the directory lists from the "
                        + "top (capped, so search when you know the name).", false),
                    ("category", "string",
                        "Optional filter: Subcontractor, Client, Architect, Supplier or Other.", false)),
                AiToolKind.Read,
                // Mirrors ListSubcontractorsEndpoint.InternalRolesThatMayListDirectory (the full
                // directory with contact details is internal-only; external sessions get their own
                // scoped views), plus Role.Admin named explicitly per the authorisation convention.
                RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
                    JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.SiteManager,
                    JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator,
                    JpmsRoles.OfficeAdmin, JpmsRoles.Foreman),
                async (context, input, ct) =>
                {
                    var search = AiToolSchema.Text(input, "search");
                    var categoryText = AiToolSchema.Text(input, "category");

                    var query = context.Db.Subcontractors.AsNoTracking();
                    if (!string.IsNullOrWhiteSpace(search))
                        query = query.Where(row => row.CompanyName.Contains(search)
                            || row.ContactName.Contains(search)
                            || row.ContactEmail.Contains(search));
                    if (!string.IsNullOrWhiteSpace(categoryText))
                    {
                        if (!Enum.TryParse<DirectoryCategory>(categoryText, ignoreCase: true, out var category))
                            return Fail("category must be Subcontractor, Client, Architect, Supplier or Other.");
                        query = query.Where(row => row.Category == (int)category);
                    }

                    // Capped like every listing tool — the fix for a truncated result is a better
                    // search term, not a bigger dump.
                    const int cap = 25;
                    var rows = await query.OrderBy(row => row.CompanyName)
                        .Take(cap + 1).ToListAsync(ct);
                    var truncated = rows.Count > cap;
                    if (truncated) rows = rows.Take(cap).ToList();

                    var ids = rows.Select(row => row.SubcontractorId).ToList();
                    var tradeLinks = await (
                        from link in context.Db.SubcontractorTrades.AsNoTracking()
                        join trade in context.Db.Trades.AsNoTracking() on link.TradeId equals trade.TradeId
                        where ids.Contains(link.SubcontractorId)
                        select new { link.SubcontractorId, trade.TradeId, trade.Name })
                        .ToListAsync(ct);
                    var xeroLinked = (await context.Db.SubcontractorXeroLinks.AsNoTracking()
                        .Where(link => ids.Contains(link.SubcontractorId))
                        .Select(link => link.SubcontractorId)
                        .Distinct().ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var companies = rows.Select(row => new
                    {
                        subcontractorId = row.SubcontractorId,
                        companyName = row.CompanyName,
                        category = ((DirectoryCategory)row.Category).ToString(),
                        isProspect = row.IsProspect,
                        trades = tradeLinks.Where(link => link.SubcontractorId == row.SubcontractorId)
                            .Select(link => new { tradeId = link.TradeId, name = link.Name })
                            .OrderBy(trade => trade.name).ToList(),
                        contactName = row.ContactName,
                        contactEmail = row.ContactEmail,
                        contactPhone = row.ContactPhone,
                        mobileNumber = row.MobileNumber,
                        address = new { row.AddressLine, row.Town, row.County, row.Postcode },
                        cisStatus = row.CisStatus,
                        paymentTermsDays = row.PaymentTermsDays,
                        xeroLinked = xeroLinked.Contains(row.SubcontractorId)
                    }).ToList();

                    return Serialise(new
                    {
                        ok = true,
                        count = companies.Count,
                        truncated,
                        companies,
                        note = "subcontractorId is what update_subcontractor takes; send its FULL "
                               + "trades list back when updating — removing the last trade is "
                               + "refused. The address here is what the purchase order prints. "
                               + "Xero-linked records copied their address from Xero at import "
                               + "only — later Xero edits never flow back, so the directory is "
                               + "corrected here."
                               + (truncated ? " More records matched than shown — narrow the search." : "")
                    });
                }),
        };
    }
}
