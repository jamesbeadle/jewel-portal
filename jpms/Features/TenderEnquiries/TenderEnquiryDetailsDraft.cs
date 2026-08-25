using Jewel.JPMS.Contracts.TenderEnquiries;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.TenderEnquiries;

/// <summary>
/// The enquiry form's working copy — text fields as typed, dates as "yyyy-MM-dd" strings so a
/// half-typed date never throws. Converts to the contract's <see cref="TenderEnquiryDetails"/>
/// when the form is posted.
/// </summary>
public sealed class TenderEnquiryDetailsDraft
{
    private const string DateFormat = "yyyy-MM-dd";

    public string Title { get; set; } = "";
    public string ArchitectPracticeName { get; set; } = "";
    public string ArchitectContactName { get; set; } = "";
    public string ArchitectContactEmail { get; set; } = "";
    public string ScopeSummary { get; set; } = "";
    public string ContractForm { get; set; } = "";
    public string ReceivedOn { get; set; } = DateTime.Today.ToString(DateFormat);
    public string PqqDueOn { get; set; } = "";
    public string TenderDueOn { get; set; } = "";

    public static TenderEnquiryDetailsDraft From(TenderEnquiry enquiry) => new()
    {
        Title = enquiry.Title,
        ArchitectPracticeName = enquiry.ArchitectPracticeName,
        ArchitectContactName = enquiry.ArchitectContactName,
        ArchitectContactEmail = enquiry.ArchitectContactEmail,
        ScopeSummary = enquiry.ScopeSummary,
        ContractForm = enquiry.ContractForm,
        ReceivedOn = enquiry.ReceivedAt.LocalDateTime.ToString(DateFormat),
        PqqDueOn = enquiry.PqqDueAt?.LocalDateTime.ToString(DateFormat) ?? "",
        TenderDueOn = enquiry.TenderDueAt?.LocalDateTime.ToString(DateFormat) ?? ""
    };

    /// <summary>What still stops this draft being posted — null when it is complete. One answer
    /// for the form's inline hint and the caller's button gate.</summary>
    public string? Problem
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Title)) return "Give the enquiry a title — usually the site address.";
            if (string.IsNullOrWhiteSpace(ArchitectPracticeName)) return "Name the architect's practice.";
            if (ParseDate(ReceivedOn) is null) return "Say when the enquiry was received.";
            return null;
        }
    }

    public TenderEnquiryDetails ToDetails() => new(
        Title.Trim(),
        ArchitectPracticeName.Trim(),
        ArchitectContactName.Trim(),
        ArchitectContactEmail.Trim(),
        ScopeSummary.Trim(),
        ContractForm.Trim(),
        ParseDate(ReceivedOn) ?? DateTimeOffset.Now,
        ParseDate(PqqDueOn),
        ParseDate(TenderDueOn));

    public static DateTimeOffset? ParseDate(string value) =>
        DateTime.TryParse(value, out var parsed) ? new DateTimeOffset(parsed.Date, TimeZoneInfo.Local.GetUtcOffset(parsed.Date)) : null;
}
