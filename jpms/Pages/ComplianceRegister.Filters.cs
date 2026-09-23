using Jewel.JPMS.Features.Directory;

namespace Jewel.JPMS.Pages;

public partial class ComplianceRegister
{
    private IReadOnlyList<ComplianceRegisterRow> FilteredRows =>
        allRows.Where(row => DirectoryComplianceFilter.Passes(row, statusFilter)).Where(MatchesSearch).ToList();

    private IReadOnlyList<TabItem> StatusChips
    {
        get
        {
            Func<int?>? lapsedOnSiteCount = hasOnSiteFailed ? null : LapsedOnSiteChipCount;
            return DirectoryComplianceFilter.Chips(
                status => IsLoaded ? allRows.Count(row => row.Status == status) : null,
                () => IsLoaded ? allRows.Count(row => row.IsBelowPublicLiabilityRequirement) : null,
                lapsedOnSiteCount);
        }
    }

    private int? LapsedOnSiteChipCount() => IsLoaded && IsOnSiteLoaded ? LapsedCoverOnSiteCount : null;

    private int LapsedCoverOnSiteCount =>
        allRows.Where(row => row.IsLapsedCoverOnSite).Select(row => row.Company.SubcontractorId).Distinct().Count();

    private string Summary
    {
        get
        {
            var companies = DirectoryCompanies().Count;
            var lapsing = allRows.Count(row => row.Status is ComplianceStatus.Expired or ComplianceStatus.ExpiringSoon);
            var below = allRows.Count(row => row.IsBelowPublicLiabilityRequirement);
            var summary = $"{companies} companies · {lapsing} document{(lapsing == 1 ? "" : "s")} expired or due within 30 days.";
            var withBelow = below == 0 ? summary : $"{summary} {below} insured under £5m public liability.";
            var onSite = IsOnSiteLoaded ? LapsedCoverOnSiteCount : 0;
            return onSite == 0 ? withBelow : $"{withBelow} {onSite} on site with insurance lapsed.";
        }
    }

    private bool MatchesSearch(ComplianceRegisterRow row)
    {
        var query = search.Trim();
        if (query.Length == 0) return true;
        return row.Company.CompanyName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || row.Company.TradesLabel.Contains(query, StringComparison.OrdinalIgnoreCase)
            || row.DocumentLabel.Contains(query, StringComparison.OrdinalIgnoreCase)
            || row.PublicLiabilityCoverText.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
