using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Pages;

/// <summary>
/// Who is working on site, read beside the documents so a lapsed certificate held by a company still
/// on a live project is seen on the register the day it lapses. The register stands without it: a
/// failed read leaves the rows as they were and takes the on-site chip away rather than offering a
/// filter nobody can answer.
/// </summary>
public partial class ComplianceRegister
{
    private IReadOnlyList<CompanyOnSite>? companiesOnSite;
    private bool hasOnSiteFailed;

    private bool IsOnSiteLoaded => companiesOnSite is not null;

    private IReadOnlyList<CompanyOnSite> CompaniesOnSite => companiesOnSite ?? Array.Empty<CompanyOnSite>();

    private async Task RefreshOnSiteAsync()
    {
        try { companiesOnSite = await Queries.AskAsync(new ListCompaniesOnSite(), CancellationToken.None); }
        catch { hasOnSiteFailed = true; }
        RebuildRows();
        StateHasChanged();
    }
}
