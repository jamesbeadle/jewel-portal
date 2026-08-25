using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Jewel.JPMS.Features.TenderEnquiries;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Triage;

/// <summary>
/// The tender enquiry drafted by "Log Tender Enquiry" in System Actions: the enquiry's details,
/// plus the Lead-stage project it creates when the job is new to Jewel (the usual case — an
/// enquiry is how a job first arrives). With <see cref="CreatesNewProject"/> off, the enquiry
/// lands on the project picked in the triage bar. Ticked email attachments (the PQQ, the
/// drawings) are copied onto the enquiry server-side.
/// </summary>
public sealed class StagedTenderEnquiryDraft
{
    public TenderEnquiryDetailsDraft Details { get; } = new();

    public bool CreatesNewProject { get; set; } = true;
    public string ProjectName { get; set; } = "";
    public string ClientName { get; set; } = "";
    public Organisation Organisation { get; set; } = Organisation.JewelBespokeBuild;
    public string AddressLine { get; set; } = "";
    public string Town { get; set; } = "";
    public string Postcode { get; set; } = "";

    /// <summary>Graph attachment ids ticked from the open email — all of them by default, since an
    /// enquiry's attachments are the questionnaire and the drawings.</summary>
    public List<string> EmailAttachmentIds { get; } = new();

    /// <summary>What still stops the enquiry being logged — null when it is complete. Shared by
    /// the editor (inline hint) and the page's Apply (hard gate), so the wording is decided once.
    /// The triage-bar project only matters when the enquiry is NOT creating its own.</summary>
    public string? Problem(bool isProjectSet)
    {
        if (Details.Problem is { } detailsProblem) return detailsProblem;
        if (CreatesNewProject && string.IsNullOrWhiteSpace(ProjectName)) return "Name the new project.";
        if (!CreatesNewProject && !isProjectSet)
            return "Set the email's Project in the bar above, or tick “Create a new project”.";
        return null;
    }

    public string Outcome => CreatesNewProject
        ? "create a Lead-stage project, log the tender enquiry on it, copy the ticked files across and tag this email to it"
        : "log the tender enquiry on the email's project, copy the ticked files across and tag this email to it";

    public LogTenderEnquiryFromMessage ToCommand(
        string messageId, string? internetMessageId, string triageProjectId, LinkThreadScope scope, bool allowCrossPathway) =>
        new(
            messageId,
            internetMessageId,
            ProjectId: CreatesNewProject ? null : triageProjectId,
            NewProject: CreatesNewProject ? ToProjectDraft() : null,
            Details: Details.ToDetails(),
            AttachmentIds: EmailAttachmentIds.ToList(),
            Scope: scope,
            AllowCrossPathway: allowCrossPathway);

    private TenderEnquiryProjectDraft ToProjectDraft() =>
        new(ProjectName.Trim(), ClientName.Trim(), Organisation, AddressLine.Trim(), Town.Trim(), Postcode.Trim());
}
