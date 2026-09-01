using Jewel.JPMS.Api.Features.AccessRequests.Commands;
using Jewel.JPMS.Api.Features.Ai.Skills;
using Jewel.JPMS.Api.Features.CostCenters.Commands;
using Jewel.JPMS.Api.Features.Hs.Commands;
using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Api.Features.Platform.Commands;
using Jewel.JPMS.Api.Features.Rates.Commands;
using Jewel.JPMS.Api.Features.UsefulInformation;
using Jewel.JPMS.Api.Features.UsefulInformation.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.AccessRequests;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Contracts.Platform;
using Jewel.JPMS.Contracts.Rates;
using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class LabourAndBackOfficeActions
{
    private static IEnumerable<AiAction> BackOfficeActions() => new AiAction[]
    {
        new AiAction(
            Name: "add_cost_center",
            Area: "Cost centres",
            Description: "Adds a cost code to the GLOBAL cost-center master — it appears at once in "
                + "the cost-code dropdowns and the Financials views that every project's money is "
                + "coded against. This is a commercial control shared by all projects, not a "
                + "per-project setting.",
            CommandType: typeof(AddCostCenter),
            ResultType: typeof(CostCenter),
            AuthorisationType: typeof(AddCostCenterAuthorisation),
            ValidationType: typeof(AddCostCenterValidation),
            VisibleTo: CostCenterManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Pass sortOrder 0 to append after the current last code. Duplicate codes are "
                + "refused by the handler."),

        new AiAction(
            Name: "revise_cost_center",
            Area: "Cost centres",
            Description: "Revises a cost code in the global cost-center master — code, name, order "
                + "and active flag — changing how money is coded on every project from now on. "
                + "Setting isActive false retires the code: it drops out of dropdowns and the "
                + "Financials view without deleting it, so historical allocations keep resolving.",
            CommandType: typeof(ReviseCostCenter),
            ResultType: typeof(CostCenter),
            AuthorisationType: typeof(ReviseCostCenterAuthorisation),
            ValidationType: typeof(ReviseCostCenterValidation),
            VisibleTo: CostCenterManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "costCenterId identifies the existing code (over HTTP it is the route value). "
                + "Confirm with the user before retiring a code — it disappears from every "
                + "project's dropdowns at once."),

        // ---- Rates --------------------------------------------------------------------------

        new AiAction(
            Name: "add_rate",
            Area: "Rates",
            Description: "Adds a rate to the company rate library (trade, description, unit, £ "
                + "value, supplier) — the priced reference the commercial team estimates and "
                + "prices work from. Money-facing: a wrong value here feeds wrong pricing.",
            CommandType: typeof(AddRate),
            ResultType: typeof(Rate),
            AuthorisationType: typeof(AddRateAuthorisation),
            ValidationType: typeof(AddRateValidation),
            VisibleTo: RateEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "revise_rate",
            Area: "Rates",
            Description: "Revises an existing rate in the company rate library, replacing its "
                + "trade, description, unit, £ value and supplier in one write. Money-facing: the "
                + "revised value is what future pricing reads.",
            CommandType: typeof(ReviseRate),
            ResultType: typeof(Rate),
            AuthorisationType: typeof(ReviseRateAuthorisation),
            ValidationType: typeof(ReviseRateValidation),
            VisibleTo: RateEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "rateId identifies the existing rate. All fields are replaced — carry forward "
                + "the values that should not change."),

        // ---- Health & safety ----------------------------------------------------------------

        new AiAction(
            Name: "log_hs_record",
            Area: "Health & safety",
            Description: "Logs a health & safety record on a project — an observation, near miss, "
                + "incident, corrective action, toolbox talk or permit — visible on the project's "
                + "H&S register immediately and assigned to a named person by email.",
            CommandType: typeof(LogHsRecord),
            ResultType: typeof(HsRecord),
            AuthorisationType: typeof(LogHsRecordAuthorisation),
            ValidationType: typeof(LogHsRecordValidation),
            VisibleTo: HsRecordManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. kind is one of Observation, NearMiss, "
                + "Incident, CorrectiveAction, ToolboxTalk, Permit; severity is Low, Medium, High "
                + "or Critical. assignedToEmail is the assignee's portal email."),

        new AiAction(
            Name: "update_hs_record",
            Area: "Health & safety",
            Description: "Updates an existing health & safety record — summary, severity, status "
                + "(Open, InProgress, Closed), assignee and due date. Setting status Closed closes "
                + "the record on the project's H&S register.",
            CommandType: typeof(UpdateHsRecord),
            ResultType: typeof(HsRecord),
            AuthorisationType: typeof(UpdateHsRecordAuthorisation),
            ValidationType: typeof(UpdateHsRecordValidation),
            VisibleTo: HsRecordManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "hsRecordId identifies the record. All listed fields are replaced — carry "
                + "forward what should not change."),

        new AiAction(
            Name: "record_attendance_for_hs_record",
            Area: "Health & safety",
            Description: "Records a named attendee against a health & safety record (typically a "
                + "toolbox talk register) — the attendance row is on the record immediately.",
            CommandType: typeof(RecordAttendanceForHsRecord),
            ResultType: typeof(HsRecordAttendance),
            AuthorisationType: typeof(RecordAttendanceForHsRecordAuthorisation),
            ValidationType: typeof(RecordAttendanceForHsRecordValidation),
            VisibleTo: HsRecordManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "signatureBlobRef is a reference to a captured signature blob — normally taken "
                + "on-site through the portal UI; only hsRecordId and attendeeName are required."),

        // ---- Useful information -------------------------------------------------------------

        new AiAction(
            Name: "add_useful_information_note",
            Area: "Useful information",
            Description: "Adds a Useful Information note to a project — internal reference "
                + "material such as door codes, key safe locations and site access notes, visible "
                + "to all staff on the project's Useful Information tab immediately. Never shown to "
                + "external logins. Recorded as created by the signed-in user.",
            CommandType: typeof(AddUsefulInformationNote),
            ResultType: typeof(UsefulInformationNote),
            AuthorisationType: typeof(AddUsefulInformationNoteAuthorisation),
            ValidationType: typeof(AddUsefulInformationNoteValidation),
            VisibleTo: UsefulInformationRoles.AllowedToManage,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

        new AiAction(
            Name: "update_useful_information_note",
            Area: "Useful information",
            Description: "Replaces a Useful Information note's title and body in one write — the "
                + "whole staff sees the new text immediately. Recorded as edited by the signed-in "
                + "user.",
            CommandType: typeof(UpdateUsefulInformationNote),
            ResultType: typeof(UsefulInformationNote),
            AuthorisationType: typeof(UpdateUsefulInformationNoteAuthorisation),
            ValidationType: typeof(UpdateUsefulInformationNoteValidation),
            VisibleTo: UsefulInformationRoles.AllowedToManage,
            EmailStamps: new[] { "UpdatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "usefulInformationNoteId identifies the note. Both title and body are replaced "
                + "— read the current note first and carry forward what should not change."),

        new AiAction(
            Name: "delete_useful_information_note",
            Area: "Useful information",
            Description: "Deletes a Useful Information note permanently. There is no undo.",
            CommandType: typeof(DeleteUsefulInformationNote),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteUsefulInformationNoteAuthorisation),
            ValidationType: typeof(DeleteUsefulInformationNoteValidation),
            VisibleTo: UsefulInformationRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which note, by title, before calling."),

        // ---- Platform -----------------------------------------------------------------------

        new AiAction(
            Name: "publish_app_version",
            Area: "Platform",
            Description: "Bumps the announced app version by one, which raises the update toast on "
                + "EVERY open portal tab and prompts every signed-in user to refresh. Carries no "
                + "target number — one call, one increment, no way to move the number backwards.",
            CommandType: typeof(PublishAppVersion),
            ResultType: typeof(AnnouncedAppVersion),
            AuthorisationType: typeof(PublishAppVersionAuthorisation),
            ValidationType: typeof(PublishAppVersionValidation),
            VisibleTo: AdminGateRoles,
            EmailStamps: new[] { "PublishedBy" },
            NameStamps: Array.Empty<string>(),
            Notes: "Affects every user's open session at once and cannot be undone — confirm with "
                + "the user before calling."),

        new AiAction(
            Name: "attach_action_skills",
            Area: "Platform",
            Description: "Replaces the set of skills attached to one connector action or to a whole "
                + "action area — the wiring the AI Actions admin page edits. An attached skill's "
                + "doctrine is served by describe_action with that action's contract from the very "
                + "next call. An empty skill list detaches everything from the target.",
            CommandType: typeof(SaveAiActionSkills),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(SaveAiActionSkillsAuthorisation),
            ValidationType: typeof(SaveAiActionSkillsValidation),
            VisibleTo: SkillRoles.ManageSkills,
            EmailStamps: new[] { "SavedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "targetKind is \"action\" or \"area\"; targetKey is the action name or the area "
                + "exactly as list_actions shows it; skillKeys come from list_skills. The save "
                + "REPLACES the target's whole set, so include every skill that should remain "
                + "attached, not just the one being added."),

        // ---- Access requests ----------------------------------------------------------------

        new AiAction(
            Name: "submit_access_request",
            Area: "Access requests",
            Description: "Submits (or refreshes) a pending portal access request for the signed-in "
                + "user's own email — it appears on the administrators' pending access requests "
                + "list. Calling again for the same email updates the display name and request "
                + "time rather than creating a duplicate.",
            CommandType: typeof(SubmitAccessRequest),
            ResultType: typeof(AccessRequest),
            AuthorisationType: typeof(SubmitAccessRequestAuthorisation),
            ValidationType: typeof(SubmitAccessRequestValidation),
            VisibleTo: AnySignedInRole,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "email must be the signed-in user's own email — the authorisation rejects any "
                + "other value. Further per-record checks apply at execution."),

        new AiAction(
            Name: "resolve_access_request",
            Area: "Access requests",
            Description: "Resolves a pending access request by DELETING its row permanently — the "
                + "request disappears from the pending list and there is no undo. This does not "
                + "itself grant or deny access; it only clears the request.",
            CommandType: typeof(ResolveAccessRequest),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ResolveAccessRequestAuthorisation),
            ValidationType: typeof(ResolveAccessRequestValidation),
            VisibleTo: AdminGateRoles,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "email is the requester's email as listed by the pending access requests view. "
                + "Irreversible — confirm with the user before calling."),
    };
}
