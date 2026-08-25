using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries;

/// <summary>
/// The one set of rules for what a person types about an enquiry, shared by every route that
/// accepts <see cref="TenderEnquiryDetails"/> — logging from an email, logging by hand, editing —
/// so a wording one route accepts can never be refused by another.
/// </summary>
internal static class TenderEnquiryDetailsRules
{
    private const int TitleMaxChars = 256;
    private const int NameMaxChars = 256;
    private const int ScopeMaxChars = 4000;

    public static IReadOnlyList<string> Problems(TenderEnquiryDetails? details)
    {
        var problems = new List<string>();
        if (details is null)
        {
            problems.Add("Details are required.");
            return problems;
        }
        if (string.IsNullOrWhiteSpace(details.Title)) problems.Add("Title is required.");
        if (string.IsNullOrWhiteSpace(details.ArchitectPracticeName)) problems.Add("The architect's practice name is required.");
        if (details.ReceivedAt == default) problems.Add("The date the enquiry was received is required.");
        if (details.PqqDueAt is { } pqqDue && pqqDue < details.ReceivedAt.AddYears(-1))
            problems.Add("The PQQ return date can't be before the enquiry was received.");
        if (details.TenderDueAt is { } tenderDue && tenderDue < details.ReceivedAt.AddYears(-1))
            problems.Add("The tender return date can't be before the enquiry was received.");
        return problems;
    }

    /// <summary>Writes the details onto the row, trimmed and clamped to the column widths — the
    /// email subject an enquiry is titled from can be longer than any column.</summary>
    public static void Apply(TenderEnquiryEntity entity, TenderEnquiryDetails details)
    {
        entity.Title = Clamp(details.Title, TitleMaxChars);
        entity.ArchitectPracticeName = Clamp(details.ArchitectPracticeName, NameMaxChars);
        entity.ArchitectContactName = Clamp(details.ArchitectContactName, NameMaxChars);
        entity.ArchitectContactEmail = Clamp(details.ArchitectContactEmail, NameMaxChars);
        entity.ScopeSummary = Clamp(details.ScopeSummary, ScopeMaxChars);
        entity.ContractForm = Clamp(details.ContractForm, NameMaxChars);
        entity.ReceivedAt = details.ReceivedAt;
        entity.PqqDueAt = details.PqqDueAt;
        entity.TenderDueAt = details.TenderDueAt;
    }

    public static string Clamp(string? value, int maxLength)
    {
        var trimmed = (value ?? "").Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
