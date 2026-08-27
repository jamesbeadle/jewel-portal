namespace Jewel.JPMS.Models;

// Which regime the project's building control runs under — who signs the work off.
// Persisted as its integer value; append new members, never insert mid-list.
public enum BuildingControlRegime
{
    LocalAuthority = 0,     // application to the council's building control (or a partner authority)
    RegisteredApprover = 1  // a private registered building control approver, working an initial notice
}

public static class BuildingControlRegimes
{
    public static readonly BuildingControlRegime[] All =
    {
        BuildingControlRegime.LocalAuthority,
        BuildingControlRegime.RegisteredApprover
    };

    public static string Label(this BuildingControlRegime regime) => regime switch
    {
        BuildingControlRegime.LocalAuthority => "Local authority",
        BuildingControlRegime.RegisteredApprover => "Registered approver (private)",
        _ => regime.ToString()
    };
}

// Where the case as a whole has got to — the notice/application at the front, the completion
// certificate at the end. Persisted as its integer value; append, never insert.
public enum BuildingControlCaseStatus
{
    NoticeSubmitted = 0,      // the notice/application is with the body, awaiting acceptance
    InForce = 1,              // accepted — inspections run under it
    CompletionRequested = 2,  // works done, completion visit/certificate asked for
    CompletionCertified = 3,  // the completion certificate is in hand — the case's closed-out state
    Lapsed = 4                // withdrawn, rejected or expired — a replacement case takes over
}

public static class BuildingControlCaseStatuses
{
    // UI wording for a status — the enum's member names must never leak into copy.
    public static string DisplayName(this BuildingControlCaseStatus status) => status switch
    {
        BuildingControlCaseStatus.NoticeSubmitted => "Notice submitted",
        BuildingControlCaseStatus.InForce => "In force",
        BuildingControlCaseStatus.CompletionRequested => "Completion requested",
        BuildingControlCaseStatus.CompletionCertified => "Completion certified",
        BuildingControlCaseStatus.Lapsed => "Lapsed",
        _ => status.ToString()
    };
}

// The inspection ladder: Planned (stage exists, no date) → Booked → Inspected (visit happened,
// outcome pending or verbal) → Passed / ActionsRequired (fix and re-book the SAME record — one
// stage, one row, the whole history in its thread) → Closed. Persisted as its integer value.
public enum BuildingControlInspectionStatus
{
    Planned = 0,
    Booked = 1,
    Inspected = 2,
    Passed = 3,
    ActionsRequired = 4,
    Closed = 5
}

public static class BuildingControlInspectionStatuses
{
    public static string DisplayName(this BuildingControlInspectionStatus status) => status switch
    {
        BuildingControlInspectionStatus.Planned => "Planned",
        BuildingControlInspectionStatus.Booked => "Booked",
        BuildingControlInspectionStatus.Inspected => "Inspected",
        BuildingControlInspectionStatus.Passed => "Passed",
        BuildingControlInspectionStatus.ActionsRequired => "Actions required",
        BuildingControlInspectionStatus.Closed => "Closed",
        _ => status.ToString()
    };
}

// What a stored building control file IS — drives where it renders (photos grid vs documents
// list) and what Close-Out can look for (the completion certificate). Persisted as its integer
// value; append, never insert.
public enum BuildingControlAttachmentKind
{
    Photo = 0,
    SiteInspectionReport = 1,
    Notice = 2,               // the initial notice / application as submitted
    Acknowledgement = 3,
    DecisionNotice = 4,
    PlanningPermission = 5,   // kept with the case for the inspector's reference
    CompletionCertificate = 6,
    Other = 7
}

public static class BuildingControlAttachmentKinds
{
    public static readonly BuildingControlAttachmentKind[] All =
    {
        BuildingControlAttachmentKind.Photo,
        BuildingControlAttachmentKind.SiteInspectionReport,
        BuildingControlAttachmentKind.Notice,
        BuildingControlAttachmentKind.Acknowledgement,
        BuildingControlAttachmentKind.DecisionNotice,
        BuildingControlAttachmentKind.PlanningPermission,
        BuildingControlAttachmentKind.CompletionCertificate,
        BuildingControlAttachmentKind.Other
    };

    public static string Label(this BuildingControlAttachmentKind kind) => kind switch
    {
        BuildingControlAttachmentKind.Photo => "Photo",
        BuildingControlAttachmentKind.SiteInspectionReport => "Site inspection report",
        BuildingControlAttachmentKind.Notice => "Notice / application",
        BuildingControlAttachmentKind.Acknowledgement => "Acknowledgement",
        BuildingControlAttachmentKind.DecisionNotice => "Decision notice",
        BuildingControlAttachmentKind.PlanningPermission => "Planning permission",
        BuildingControlAttachmentKind.CompletionCertificate => "Completion certificate",
        BuildingControlAttachmentKind.Other => "Other",
        _ => kind.ToString()
    };
}

/// <summary>Where a building control file came from.</summary>
public enum BuildingControlAttachmentSource
{
    /// <summary>A file uploaded from the computer (or the phone's browser, on site).</summary>
    Upload = 0,
    /// <summary>Copied off an email linked to the record — the inspector's report, their photos.</summary>
    Email = 1
}

// The project's case with a building control body: who signs the work off, under what reference,
// and where the case as a whole has got to. One ACTIVE case per project in the UI; the model
// allows more (a second notice for an outbuilding, a re-submission after a lapse) so that day
// never needs a migration. Owns a sequential "BC-0001" reference which is also its mailbox tag
// stem — case-level correspondence (the "who is our contact" email, the acknowledgement) files
// against it, while each inspection's own thread files against the inspection.
public sealed record BuildingControlCase(
    string BuildingControlCaseId,
    string ProjectId,
    int Number,
    string Reference,        // sequential human reference, e.g. "BC-0001" (also the tag stem)
    BuildingControlRegime Regime,
    string BodyName,         // "Bromley Building Control", "Assent Building Control"
    string BodyReference,    // their reference — "BC2415406DOMFPB", "25-129527"
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    BuildingControlCaseStatus Status,
    DateTimeOffset? NoticeSubmittedOn,      // official dates, user-entered — what lists lead with
    DateTimeOffset? AcceptedOn,
    DateTimeOffset? CompletionCertifiedOn,
    string Notes,
    string CreatedByEmail,
    DateTimeOffset CreatedAt);

// One inspection stage on a case — the record the Building Control tab is built around. Owns a
// sequential "BCI-0001" reference which is also its mailbox tag stem, so the inspector's booking
// and follow-up emails read back live under it (the Defect / Calendar Event mechanism).
// BookedFor is the official date agreed with the inspector — user-editable, what lists lead
// with; RaisedAt is the system's own stamp, a secondary fact on the detail page only.
public sealed record BuildingControlInspection(
    string BuildingControlInspectionId,
    string BuildingControlCaseId,
    string ProjectId,
    int Number,
    string Reference,        // sequential human reference, e.g. "BCI-0001" (also the tag stem)
    string StageName,        // "Foundations — ground beam reinforcement"
    BuildingControlInspectionStatus Status,
    DateTimeOffset? BookedFor,
    DateTimeOffset? InspectedAt,
    string OutcomeNotes,     // the inspector's verbal outcome / actions required
    string InspectorName,
    int DisplayOrder,
    string RaisedByEmail,
    DateTimeOffset RaisedAt);

/// <summary>
/// A file kept on the case (notices, decision, completion certificate) or on one inspection
/// (photos, the site inspection report). Exactly one parent is set. Files live in their own
/// private container and are proxied on download — the tender-enquiry attachment arrangement.
/// </summary>
public sealed record BuildingControlAttachment(
    string BuildingControlAttachmentId,
    string ProjectId,
    string? BuildingControlCaseId,
    string? BuildingControlInspectionId,
    BuildingControlAttachmentKind Kind,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    BuildingControlAttachmentSource Source,
    DateTimeOffset AddedAt,
    string AddedByEmail)
{
    /// <summary>True for the image types a browser can show inline, so the photos grid can thumbnail them.</summary>
    public bool IsImage =>
        ContentType is { Length: > 0 } type
        && type.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>The label a person reads — the file's name, with a safe fallback.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(FileName) ? "Attachment" : FileName;
}

/// <summary>
/// The standard stage checklist a new case offers to seed — a template, not a rule: every
/// project renames, reorders, adds and deletes freely (the By France job ran nine stages, Abbot
/// Road four — no fixed list survives contact with a real build).
/// </summary>
public static class BuildingControlStages
{
    public static readonly IReadOnlyList<string> DefaultChecklist = new[]
    {
        "Foundations",
        "Drainage",
        "Superstructure & roof",
        "Insulation & fire lining",
        "Completion"
    };
}
