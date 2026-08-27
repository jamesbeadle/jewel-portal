using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.BuildingControl;

/// <summary>
/// The editable face of a building control case — everything the set-up/edit dialog captures,
/// shared by create and update so the two routes cannot drift apart. Dates are UK-local calendar
/// dates stored as midnight UTC (the SiteClock convention).
/// </summary>
public sealed record BuildingControlCaseDetails(
    BuildingControlRegime Regime,
    string BodyName,
    string BodyReference,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    DateTimeOffset? NoticeSubmittedOn,
    DateTimeOffset? AcceptedOn,
    string Notes);

/// <summary>Sets up the project's building control case. SeedStandardStages plants the default
/// checklist (BuildingControlStages.DefaultChecklist) as Planned inspections — a starting point,
/// freely edited afterwards. CreatedByEmail is stamped server-side from the signed-in user.
/// Refused while the project already has an active (non-lapsed, non-certified) case — mark that
/// one Lapsed first; the schema allows successors, the UI works one case at a time.</summary>
public sealed record CreateBuildingControlCase(
    string ProjectId,
    BuildingControlCaseDetails Details,
    bool SeedStandardStages = true,
    string CreatedByEmail = "") : ICommand<BuildingControlCase>;

public sealed record UpdateBuildingControlCase(
    string BuildingControlCaseId,
    BuildingControlCaseDetails Details) : ICommand<BuildingControlCase>;

/// <summary>One move along the case ladder. Moving to CompletionCertified stamps
/// CompletionCertifiedOn (today) unless a date is passed; moving away clears it.</summary>
public sealed record SetBuildingControlCaseStatus(
    string BuildingControlCaseId,
    BuildingControlCaseStatus Status,
    DateTimeOffset? CompletionCertifiedOn = null) : ICommand<BuildingControlCase>;

/// <summary>
/// The editable face of one inspection stage — shared by add, update and the triage
/// create-from-message. BookedFor is the official date agreed with the inspector (user-editable,
/// what lists lead with); InspectedAt is when the visit actually happened.
/// </summary>
public sealed record BuildingControlInspectionDetails(
    string StageName,
    DateTimeOffset? BookedFor,
    DateTimeOffset? InspectedAt,
    string OutcomeNotes,
    string InspectorName);

/// <summary>Adds one inspection stage to the case, at the foot of the running order. A stage
/// with a BookedFor date starts at Booked, otherwise Planned. RaisedByEmail is stamped
/// server-side from the signed-in user.</summary>
public sealed record AddBuildingControlInspection(
    string BuildingControlCaseId,
    BuildingControlInspectionDetails Details,
    string RaisedByEmail = "") : ICommand<BuildingControlInspection>;

public sealed record UpdateBuildingControlInspection(
    string BuildingControlInspectionId,
    BuildingControlInspectionDetails Details) : ICommand<BuildingControlInspection>;

/// <summary>One move along the inspection ladder (Planned → Booked → Inspected → Passed /
/// ActionsRequired → Closed — and back, when a booking falls through). Moving to Inspected
/// stamps InspectedAt (today) unless the inspection already carries one; moving back to
/// Planned/Booked clears it.</summary>
public sealed record SetBuildingControlInspectionStatus(
    string BuildingControlInspectionId,
    BuildingControlInspectionStatus Status) : ICommand<BuildingControlInspection>;

/// <summary>Removes a stage that never happened — only a Planned inspection with no files;
/// anything booked, inspected or carrying evidence is history, not clutter (close it instead).</summary>
public sealed record DeleteBuildingControlInspection(
    string BuildingControlInspectionId) : ICommand<Acknowledgement>;

/// <summary>Re-kinds a stored file (a copied-off report that landed as Other, a certificate
/// uploaded under the wrong kind) — the row is the record, the bytes never move.</summary>
public sealed record SetBuildingControlAttachmentKind(
    string BuildingControlAttachmentId,
    BuildingControlAttachmentKind Kind) : ICommand<BuildingControlAttachment>;

/// <summary>Removes a stored file. The row is deleted first; the blob goes best-effort — the
/// tender-enquiry attachment rule.</summary>
public sealed record RemoveBuildingControlAttachment(
    string BuildingControlAttachmentId) : ICommand<Acknowledgement>;

/// <summary>
/// Copies files off an email linked to the inspection — the inspector's site report, their
/// photos — into the inspection's attachment store (Source = Email), so the evidence lives with
/// the record rather than only in the thread. Kind is inferred per file when not passed:
/// image/* → Photo, PDF → SiteInspectionReport, anything else → Other.
/// </summary>
public sealed record CopyEmailAttachmentsToBuildingControlInspection(
    string BuildingControlInspectionId,
    string MessageId,
    IReadOnlyList<string> AttachmentIds,
    BuildingControlAttachmentKind? Kind = null,
    string AddedByEmail = "") : ICommand<IReadOnlyList<BuildingControlAttachment>>;
