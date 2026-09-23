using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Features.Directory;

/// <summary>One line of the compliance register: a company and one of its current documents —
/// or the company alone, standing Missing, when it holds none. Rows read in
/// <see cref="ComplianceStatusExtensions.ReadingOrder"/> (Expired, Expiring soon, Current, then the
/// Missing companies last), soonest expiry first within a standing, then by company. A company
/// working on site carries the projects it is on, so lapsed insurance on site is read on the row.</summary>
public sealed record ComplianceRegisterRow(
    Subcontractor Company, ComplianceDocument? Document, ComplianceStatus Status, IReadOnlyList<string> OnSiteAt)
{
    public string DocumentLabel => Document?.Kind ?? "No documents on file";

    public DateTimeOffset? ExpiresAt => Document?.ExpiresAt;

    /// <summary>The public liability figure the document records, in pounds (2026-09-10, the
    /// accountant's ask) — null on a Missing row or a document with none recorded.</summary>
    public decimal? PublicLiabilityCover => Document?.PublicLiabilityCover;

    public string PublicLiabilityCoverText => ComplianceDocumentExtensions.PublicLiabilityCoverText(PublicLiabilityCover);

    /// <summary>A recorded figure under the £5m Jewel's insurer requires on big jobs. A flag on
    /// the row, never a status — see ComplianceDocument.IsBelowPublicLiabilityRequirement.</summary>
    public bool IsBelowPublicLiabilityRequirement => Document?.IsBelowPublicLiabilityRequirement == true;

    public bool IsOnSite => OnSiteAt.Count > 0;

    /// <summary>Insurance whose expiry has passed, held by a company still working on site.</summary>
    public bool IsLapsedCoverOnSite => IsOnSite && Document?.HasLapsed() == true;
}

/// <summary>The register's rows built from the directory, the current documents and who is on site.</summary>
public static class ComplianceRegisterRows
{
    public static IReadOnlyList<ComplianceRegisterRow> Build(
        IReadOnlyList<Subcontractor> companies, IReadOnlyList<ComplianceDocument> currentDocuments, IReadOnlyList<CompanyOnSite> onSite)
    {
        var documentsByCompany = currentDocuments
            .Where(document => document.IsCurrentVersion)
            .ToLookup(document => document.SubcontractorId, StringComparer.OrdinalIgnoreCase);
        var projectsByCompany = onSite.ToDictionary(company => company.SubcontractorId, company => company.ProjectNames, StringComparer.OrdinalIgnoreCase);
        return companies
            .SelectMany(company => RowsFor(company, documentsByCompany[company.SubcontractorId].ToList(),
                projectsByCompany.GetValueOrDefault(company.SubcontractorId) ?? Array.Empty<string>()))
            .OrderBy(row => row.Status.ReadingRank())
            .ThenBy(row => row.ExpiresAt ?? DateTimeOffset.MaxValue)
            .ThenBy(row => row.Company.CompanyName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<ComplianceRegisterRow> RowsFor(
        Subcontractor company, IReadOnlyList<ComplianceDocument> documents, IReadOnlyList<string> onSiteAt)
    {
        if (documents.Count == 0) return new[] { new ComplianceRegisterRow(company, null, ComplianceStatus.Missing, onSiteAt) };
        return documents.Select(document => new ComplianceRegisterRow(company, document, document.Status(), onSiteAt));
    }
}
