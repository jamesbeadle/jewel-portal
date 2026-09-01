using Jewel.JPMS.Api.Features.ArchitectInstructions;
using Jewel.JPMS.Api.Features.BuildingControl;
using Jewel.JPMS.Api.Features.BuildingControl.Attachments;
using Jewel.JPMS.Api.Features.BuildingControl.Commands;
using Jewel.JPMS.Api.Features.Mobilisation.Commands;
using Jewel.JPMS.Api.Features.ProjectContracts;
using Jewel.JPMS.Api.Features.ProjectContracts.Commands;
using Jewel.JPMS.Api.Features.Projects.Commands;
using Jewel.JPMS.Api.Features.Projects.Contacts;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.TenderEnquiries;
using Jewel.JPMS.Api.Features.TenderEnquiries.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ArchitectInstructions;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class ProjectsAndTendersActions
{
    private static IEnumerable<AiAction> BuildingControlActions() => new AiAction[]
    {
        new AiAction(
            Name: "create_building_control_case",
            Area: "Building control",
            Description: "Sets up a project's building control case — regime, body, references, "
                + "contact and dates. With seedStandardStages true (the default) the standard "
                + "inspection checklist is planted as Planned stages, freely edited afterwards. "
                + "Refused while the project already has an active case — mark that one Lapsed first.",
            CommandType: typeof(CreateBuildingControlCase),
            ResultType: typeof(BuildingControlCase),
            AuthorisationType: typeof(CreateBuildingControlCaseAuthorisation),
            ValidationType: typeof(CreateBuildingControlCaseValidation),
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. Dates are UK-local calendar dates."),

        new AiAction(
            Name: "update_building_control_case",
            Area: "Building control",
            Description: "Replaces a building control case's details wholesale — regime, body, "
                + "references, contact, notice/acceptance dates and notes. A NoticeSubmitted case "
                + "whose acceptance date has just been entered moves to In force automatically.",
            CommandType: typeof(UpdateBuildingControlCase),
            ResultType: typeof(BuildingControlCase),
            AuthorisationType: typeof(UpdateBuildingControlCaseAuthorisation),
            ValidationType: typeof(UpdateBuildingControlCaseValidation),
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "buildingControlCaseId comes from the project's Building Control tab data "
                + "(GetBuildingControlForProject). Read the case first and carry forward what should "
                + "not change."),

        new AiAction(
            Name: "set_building_control_case_status",
            Area: "Building control",
            Description: "Moves a building control case along its ladder (NoticeSubmitted, InForce, "
                + "CompletionRequested, CompletionCertified, Lapsed). Moving to CompletionCertified "
                + "stamps the certificate date (today unless one is passed); moving away clears it.",
            CommandType: typeof(SetBuildingControlCaseStatus),
            ResultType: typeof(BuildingControlCase),
            AuthorisationType: typeof(SetBuildingControlCaseStatusAuthorisation),
            ValidationType: null,
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm the move with the user before calling — the case status is where the "
                + "project's sign-off stands."),

        new AiAction(
            Name: "add_building_control_inspection",
            Area: "Building control",
            Description: "Adds one inspection stage to a building control case, at the foot of the "
                + "running order. A stage with a bookedFor date starts at Booked, otherwise Planned.",
            CommandType: typeof(AddBuildingControlInspection),
            ResultType: typeof(BuildingControlInspection),
            AuthorisationType: typeof(AddBuildingControlInspectionAuthorisation),
            ValidationType: typeof(AddBuildingControlInspectionValidation),
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: new[] { "RaisedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "buildingControlCaseId comes from GetBuildingControlForProject. bookedFor is the "
                + "official date agreed with the inspector; inspectedAt is when the visit actually "
                + "happened."),

        new AiAction(
            Name: "update_building_control_inspection",
            Area: "Building control",
            Description: "Replaces an inspection stage's details wholesale — stage name, booked and "
                + "inspected dates, outcome notes and inspector name.",
            CommandType: typeof(UpdateBuildingControlInspection),
            ResultType: typeof(BuildingControlInspection),
            AuthorisationType: typeof(UpdateBuildingControlInspectionAuthorisation),
            ValidationType: typeof(UpdateBuildingControlInspectionValidation),
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "buildingControlInspectionId comes from GetBuildingControlForProject. Read the "
                + "inspection first and carry forward what should not change."),

        new AiAction(
            Name: "set_building_control_inspection_status",
            Area: "Building control",
            Description: "Moves an inspection along its ladder (Planned, Booked, Inspected, Passed, "
                + "ActionsRequired, Closed — and back, when a booking falls through). Moving to "
                + "Inspected stamps the inspected date (today unless the inspection already carries "
                + "one); moving back to Planned/Booked clears it.",
            CommandType: typeof(SetBuildingControlInspectionStatus),
            ResultType: typeof(BuildingControlInspection),
            AuthorisationType: typeof(SetBuildingControlInspectionStatusAuthorisation),
            ValidationType: null,
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "delete_building_control_inspection",
            Area: "Building control",
            Description: "Deletes an inspection stage permanently — only a Planned stage with no "
                + "files; anything booked, inspected or carrying evidence is refused (close it "
                + "instead). There is no undo, and the stage's BCI number is never re-issued.",
            CommandType: typeof(DeleteBuildingControlInspection),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteBuildingControlInspectionAuthorisation),
            ValidationType: null,
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which stage, by name, before calling."),

        new AiAction(
            Name: "create_building_control_inspection_from_message",
            Area: "Building control",
            Description: "Turns an inspector's email — a booking confirmation, a visit arrangement — "
                + "into an inspection stage on the project's building control case and tags the email "
                + "thread to it (triage pathway). Requires the case to exist already.",
            CommandType: typeof(CreateBuildingControlInspectionFromMessage),
            ResultType: typeof(BuildingControlInspection),
            AuthorisationType: typeof(CreateBuildingControlInspectionFromMessageAuthorisation),
            ValidationType: typeof(CreateBuildingControlInspectionFromMessageValidation),
            VisibleTo: TriageRoles.AllowedToTriage,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is a mailbox message id from the triage queue, not a request id. "
                + "Refused if the thread already carries another pathway unless allowCrossPathway is "
                + "true — pass that only with the user's explicit say-so."),

        new AiAction(
            Name: "set_building_control_attachment_kind",
            Area: "Building control",
            Description: "Re-kinds a stored building control file (Photo, SiteInspectionReport, "
                + "Notice, Acknowledgement, DecisionNotice, PlanningPermission, CompletionCertificate, "
                + "Other) — the row is the record, the bytes never move.",
            CommandType: typeof(SetBuildingControlAttachmentKind),
            ResultType: typeof(BuildingControlAttachment),
            AuthorisationType: typeof(SetBuildingControlAttachmentKindAuthorisation),
            ValidationType: null,
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "buildingControlAttachmentId comes from the case's or inspection's attachment "
                + "list."),

        new AiAction(
            Name: "remove_building_control_attachment",
            Area: "Building control",
            Description: "Deletes a stored building control file permanently — the row first, the "
                + "blob best-effort. There is no undo.",
            CommandType: typeof(RemoveBuildingControlAttachment),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RemoveBuildingControlAttachmentAuthorisation),
            ValidationType: null,
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which file, by name, before calling."),

        new AiAction(
            Name: "copy_email_attachments_to_building_control_inspection",
            Area: "Building control",
            Description: "Copies files off an email linked to an inspection — the inspector's site "
                + "report, their photos — into the inspection's attachment store, so the evidence "
                + "lives with the record rather than only in the thread. Kind is inferred per file "
                + "when not passed: image → Photo, PDF → SiteInspectionReport, anything else → Other.",
            CommandType: typeof(CopyEmailAttachmentsToBuildingControlInspection),
            ResultType: typeof(IReadOnlyList<BuildingControlAttachment>),
            AuthorisationType: typeof(CopyEmailAttachmentsToBuildingControlInspectionAuthorisation),
            ValidationType: null,
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: new[] { "AddedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "messageId and attachmentIds come from the emails linked to the inspection "
                + "(read_record_emails)."),

        // ---- Architect instructions ---------------------------------------------------------
        // Unlocked 2026-08-31 (docs/ai/11 §4): gate classes added in
        // ArchitectInstructionCommandGates.cs, same RoleSet the endpoints check inline.

    };
}
