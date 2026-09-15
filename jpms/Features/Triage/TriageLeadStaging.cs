using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Features.Triage;

/// <summary>
/// The sales lead drafted by "Raise Lead" on the Sales pane (2026-09-15, Nigel): an estimate
/// enquiry from someone who might build with Jewel becomes a lead — who, where, what — with the
/// email tagged to it (JPMS/LD-####) so the lead reads its enquiry mail live. The contact is
/// suggested from the sender and the summary from the subject; the estimate itself is opened on
/// the lead's page once it exists. A lead belongs to no project, so nothing here asks for one.
/// The estimated value is held as typed until apply — the StagedRecordCreate arrangement.
/// </summary>
public sealed class StagedLeadDraft
{
    public string ContactName { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public LeadProspectKind ProspectKind { get; set; } = LeadProspectKind.Homeowner;
    public string PropertyAddress { get; set; } = "";
    public string Postcode { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Notes { get; set; } = "";
    public string EstimatedValueText { get; set; } = "";

    public decimal? EstimatedValue => StagedRecordCreate.ParseDecimal(EstimatedValueText);

    /// <summary>The chip's name for the draft: the person, else their company, else who it came from.</summary>
    public string DisplayTitle =>
        !string.IsNullOrWhiteSpace(ContactName) ? ContactName.Trim()
        : !string.IsNullOrWhiteSpace(CompanyName) ? CompanyName.Trim()
        : ContactEmail.Trim();

    /// <summary>Someone to talk to (a name or an email) and a line on the work — the two things
    /// that make the draft a real lead rather than an empty form.</summary>
    public bool IsReady => HasContact && !string.IsNullOrWhiteSpace(Summary);

    private bool HasContact => !string.IsNullOrWhiteSpace(ContactName) || !string.IsNullOrWhiteSpace(ContactEmail);

    /// <summary>What still stops the lead being raised — null when it is complete. Shared by the
    /// editor (inline hint) and the page's Apply (hard gate), so the wording is decided once —
    /// the same "decision not yet made" rule as the staged work order and defect. Mirrors the
    /// server's own rule (CaptureLeadValidation: a person or a company).</summary>
    public string? Problem
    {
        get
        {
            if (!HasContact) return "Name the contact, or give their email.";
            if (string.IsNullOrWhiteSpace(Summary)) return "Say in a line what the work might be.";
            if (!string.IsNullOrWhiteSpace(EstimatedValueText) && EstimatedValue is null)
                return "The estimated value isn't a number.";
            return null;
        }
    }

    public string Outcome => "raise the lead (Engaged, from an inbound enquiry) and tag this email to it";

    public CreateLeadFromMessage ToCommand(
        string messageId, string? internetMessageId, LinkThreadScope scope, bool allowCrossPathway) =>
        new(messageId,
            ContactName.Trim(),
            ContactEmail.Trim(),
            ContactPhone.Trim(),
            CompanyName.Trim(),
            ProspectKind,
            PropertyAddress.Trim(),
            Postcode.Trim(),
            Summary.Trim(),
            Notes.Trim(),
            EstimatedValue,
            InternetMessageId: internetMessageId,
            Scope: scope,
            AllowCrossPathway: allowCrossPathway);
}
