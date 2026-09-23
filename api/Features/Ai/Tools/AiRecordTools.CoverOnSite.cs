using Jewel.JPMS.Contracts.Subcontractors;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The compliance register's "On site, insurance lapsed" chip as a connector read: every company
/// working on a live project under a released work order whose insurance certificate has passed its
/// expiry, with the projects and the lapsed certificates. The same two reads as the page
/// (ListCompaniesOnSite beside ListCurrentComplianceDocuments) and the same rule
/// (ComplianceInsurance.HasLapsed), so the assistant and the register never disagree.
/// </summary>
internal static partial class AiRecordTools
{
    private static IEnumerable<AiTool> CoverOnSiteTools() => new AiTool[]
    {
        new(
            "list_lapsed_cover_on_site",
            "The companies working on site today (a released work order on a project that is not "
            + "finished) whose insurance certificate on file has passed its expiry, each with the "
            + "projects it is on and the lapsed certificates (kind, expiry). This is the compliance "
            + "register's \"On site, insurance lapsed\" chip: an uninsured company on a Jewel site is a "
            + "live exposure from the day the certificate lapses. Call it for who is on site uninsured, "
            + "before placing more work with a company, or before chasing renewals. "
            + "list_compliance_register has every company's documents.",
            AiToolSchema.Empty(),
            AiToolKind.Read,
            ComplianceReaders,
            async (context, input, ct) => Serialise(await LapsedCoverOnSiteAsync(context, ct)))
    };

    private static async Task<object> LapsedCoverOnSiteAsync(AiToolContext context, CancellationToken ct)
    {
        var onSite = await context.Services.GetRequiredService<IQueryHandler<ListCompaniesOnSite, IReadOnlyList<CompanyOnSite>>>()
            .HandleAsync(new ListCompaniesOnSite(), ct);
        var documents = await context.Services
            .GetRequiredService<IQueryHandler<ListCurrentComplianceDocuments, IReadOnlyList<ComplianceDocument>>>()
            .HandleAsync(new ListCurrentComplianceDocuments(), ct);
        var lapsedByCompany = documents.Where(document => document.HasLapsed())
            .ToLookup(document => document.SubcontractorId, StringComparer.OrdinalIgnoreCase);
        var uninsured = onSite.Where(company => lapsedByCompany[company.SubcontractorId].Any()).ToList();
        var ids = uninsured.Select(company => company.SubcontractorId).ToList();
        var names = await context.Db.Subcontractors.AsNoTracking().Where(row => ids.Contains(row.SubcontractorId))
            .ToDictionaryAsync(row => row.SubcontractorId, row => row.CompanyName, StringComparer.OrdinalIgnoreCase, ct);
        var companies = uninsured.Select(company => new
        {
            subcontractorId = company.SubcontractorId,
            companyName = names.GetValueOrDefault(company.SubcontractorId, ""),
            projects = company.ProjectNames,
            lapsedCertificates = lapsedByCompany[company.SubcontractorId]
                .Select(document => new { document.ComplianceDocumentId, document.Kind, document.ExpiresAt })
        }).OrderBy(company => company.companyName, StringComparer.OrdinalIgnoreCase).ToList();
        return new { ok = true, count = companies.Count, companies };
    }
}
