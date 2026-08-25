using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Documents;

/// <summary>
/// Everything needed to render an enquiry's PQQ response, collated from the SQL source of truth.
/// A flat, self-contained snapshot — the renderer has no database dependency, and the bytes are
/// a pure function of the current record (bar <see cref="GeneratedAt"/>), so regeneration on
/// download and attach is idempotent. Same arrangement as VariationDocumentModel.
/// </summary>
public sealed record TenderEnquiryDocumentModel(
    string TenderEnquiryId,
    string Reference,                  // "TEQ-0001"
    string Title,                      // the job as the architect names it
    string ArchitectPracticeName,
    string ArchitectContactName,
    string ScopeSummary,
    string ContractForm,
    string StatusLabel,
    string ProjectName,
    string ProjectReference,
    string SiteAddress,                // the project's address lines joined, blank when unknown
    string OwnerEmail,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? PqqDueAt,
    IReadOnlyList<TenderEnquiryAnswer> Answers,
    DateTimeOffset GeneratedAt)
{
    private const int TitleMaxCharsInFileName = 60;

    /// <summary>A safe, human file name — "TEQ-0001 - 85 Northampton Road PQQ Response.pdf".</summary>
    public string FileName
    {
        get
        {
            var title = Title.Trim();
            if (title.Length > TitleMaxCharsInFileName) title = title[..TitleMaxCharsInFileName].TrimEnd();
            var stem = title.Length > 0 ? $"{Reference} - {title} PQQ Response" : $"{Reference} PQQ Response";
            foreach (var invalid in Path.GetInvalidFileNameChars())
                stem = stem.Replace(invalid, '-');
            return stem + ".pdf";
        }
    }

    public string EmailSubject => $"Pre-Qualification Questionnaire response: {Title} — Jewel Bespoke Build";
}
