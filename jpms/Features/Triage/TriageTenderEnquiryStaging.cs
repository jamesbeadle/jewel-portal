using Jewel.JPMS.Contracts.TenderEnquiries;
using Jewel.JPMS.Features.TenderEnquiries;

namespace Jewel.JPMS.Features.Triage;

/// <summary>
/// The tender enquiry drafted by "Log Tender Enquiry" in System Actions: the enquiry's details,
/// plus the Lead-stage project it creates — an enquiry is how a job first arrives, so this is
/// the rule. The one exception is a second email on a job that is ALREADY a Lead (the tender
/// pack following the PQQ): with <see cref="CreatesNewProject"/> off, the enquiry joins the
/// Lead project picked in the triage bar. Live work never receives an enquiry — the editor only
/// offers the switch when the bar's project is at Lead stage. Ticked email attachments (the
/// PQQ, the drawings) are copied onto the enquiry server-side.
/// </summary>
public sealed class StagedTenderEnquiryDraft
{
    public TenderEnquiryDetailsDraft Details { get; } = new();
    public TenderEnquiryNewProjectDraft NewProject { get; } = new();

    public bool CreatesNewProject { get; set; } = true;

    /// <summary>Graph attachment ids ticked from the open email — all of them by default, since an
    /// enquiry's attachments are the questionnaire and the drawings.</summary>
    public List<string> EmailAttachmentIds { get; } = new();

    /// <summary>What still stops the enquiry being logged — null when it is complete. Shared by
    /// the editor (inline hint) and the page's Apply (hard gate), so the wording is decided once.
    /// The triage-bar project only matters when the enquiry is joining an existing Lead.</summary>
    public string? Problem(bool isProjectSet)
    {
        if (Details.Problem is { } detailsProblem) return detailsProblem;
        if (CreatesNewProject) return NewProject.Problem;
        if (!isProjectSet) return "Set the Lead project in the bar above, or tick “Create a new project”.";
        return null;
    }

    public string Outcome => CreatesNewProject
        ? "create a Lead-stage project, log the tender enquiry on it, copy the ticked files across and tag this email to it"
        : "log the tender enquiry on the email's Lead project, copy the ticked files across and tag this email to it";

    public LogTenderEnquiryFromMessage ToCommand(
        string messageId, string? internetMessageId, string triageProjectId, LinkThreadScope scope, bool allowCrossPathway) =>
        new(
            messageId,
            internetMessageId,
            ProjectId: CreatesNewProject ? null : triageProjectId,
            NewProject: CreatesNewProject ? NewProject.ToDraft() : null,
            Details: Details.ToDetails(),
            AttachmentIds: EmailAttachmentIds.ToList(),
            Scope: scope,
            AllowCrossPathway: allowCrossPathway);
}
