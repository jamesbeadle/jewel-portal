
namespace Jewel.JPMS.Api.Features.TenderEnquiries.Documents;

/// <summary>
/// Collates a <see cref="TenderEnquiryDocumentModel"/> from the SQL source of truth. Pure read —
/// calling it on download or on every attach always reflects the enquiry and its answers exactly
/// as they stand (idempotent regeneration; nothing is persisted).
/// </summary>
public static class TenderEnquiryDocumentBuilder
{
    public static async Task<TenderEnquiryDocumentModel?> BuildAsync(
        JpmsContext context, string tenderEnquiryId, CancellationToken cancellationToken)
    {
        var entity = await context.TenderEnquiries.AsNoTracking()
            .FirstOrDefaultAsync(row => row.TenderEnquiryId == tenderEnquiryId, cancellationToken);
        if (entity is null) return null;

        var enquiry = entity.ToModel();
        var project = await context.Projects.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ProjectId == entity.ProjectId, cancellationToken);
        var answers = await TenderEnquiryAnswerReader.ListAsync(context, tenderEnquiryId, cancellationToken);

        return new TenderEnquiryDocumentModel(
            TenderEnquiryId: enquiry.TenderEnquiryId,
            Reference: enquiry.Reference,
            Title: enquiry.Title,
            ArchitectPracticeName: enquiry.ArchitectPracticeName,
            ArchitectContactName: enquiry.ArchitectContactName,
            ScopeSummary: enquiry.ScopeSummary,
            ContractForm: enquiry.ContractForm,
            StatusLabel: enquiry.Status.DisplayName(),
            ProjectName: project?.Name ?? "(unknown project)",
            ProjectReference: project?.Reference ?? entity.ProjectId,
            SiteAddress: SiteAddressOf(project?.AddressLine, project?.Town, project?.Postcode),
            OwnerEmail: enquiry.OwnerEmail,
            ReceivedAt: enquiry.ReceivedAt,
            PqqDueAt: enquiry.PqqDueAt,
            Answers: answers,
            GeneratedAt: DateTimeOffset.UtcNow);
    }

    private static string SiteAddressOf(params string?[] parts) =>
        string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
}
