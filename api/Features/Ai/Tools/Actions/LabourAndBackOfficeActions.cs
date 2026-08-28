using Jewel.JPMS.Api.Features.AccessRequests.Commands;
using Jewel.JPMS.Api.Features.CostCenters.Commands;
using Jewel.JPMS.Api.Features.Hs.Commands;
using Jewel.JPMS.Api.Features.Platform.Commands;
using Jewel.JPMS.Api.Features.Rates.Commands;
using Jewel.JPMS.Api.Features.UsefulInformation;
using Jewel.JPMS.Api.Features.UsefulInformation.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.AccessRequests;
using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Contracts.Platform;
using Jewel.JPMS.Contracts.Rates;
using Jewel.JPMS.Contracts.UsefulInformation;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Labour and back-office commands as connector actions. Mirrors the command endpoints
/// under Features/CostCenters, Features/Rates, Features/Hs, Features/UsefulInformation,
/// Features/Platform and Features/AccessRequests. The Labour, Xero and Registers command
/// endpoints all gate with inline role checks and have no Authorisation classes, so none of them
/// can be mirrored here — every one is recorded in the skip list at the bottom of this file.
/// Where an authorisation keeps its role set as a private field, the VisibleTo below replicates
/// the identical roles with RoleSet.Of(...) and a comment names the source; where the set is an
/// accessible internal static (UsefulInformationRoles), it is referenced directly.</summary>
internal sealed class LabourAndBackOfficeActions : IAiActionSource
{
    // Replica of AddCostCenterAuthorisation.RolesThatMayManageCostCenters (identical set in
    // ReviseCostCenterAuthorisation).
    private static readonly RoleSet CostCenterManagers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.Estimator);

    // Replica of AddRateAuthorisation.RolesThatMayEditRates (identical to
    // ReviseRateAuthorisation.RolesThatMayReviseRates).
    private static readonly RoleSet RateEditors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.Estimator);

    // Replica of LogHsRecordAuthorisation.RolesThatMayLogHsRecords (identical sets in
    // UpdateHsRecordAuthorisation and RecordAttendanceForHsRecordAuthorisation).
    private static readonly RoleSet HsRecordManagers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager,
            JpmsRoles.HealthAndSafetyLead);

    // Mirror of AdminGate.Allows — Role.Admin or the Finance Director, who is granted the same
    // permissions without holding the Admin identity (see AdminGate).
    private static readonly RoleSet AdminGateRoles =
        RoleSet.Of(Role.Admin, JpmsRoles.FinanceDirector);

    // SubmitAccessRequestAuthorisation checks no roles at all — only that the command's Email is
    // the signed-in user's own email — so the broadest possible set is every portal role.
    private static readonly RoleSet AnySignedInRole =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
            JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.SiteManager,
            JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator,
            JpmsRoles.OfficeAdmin, JpmsRoles.Architect, JpmsRoles.Client,
            JpmsRoles.Subcontractor, JpmsRoles.Foreman, JpmsRoles.SiteOperative,
            JpmsRoles.Accounts);

    public IEnumerable<AiAction> Build() => new[]
    {
        // ---- Cost centres -------------------------------------------------------------------

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
            Notes: "email is the requester's email as listed by the pending access requests view. "
                + "Irreversible — confirm with the user before calling."),
    };

    // Skipped: AddLabourSettlementVariance — no Authorisation class (inline LabourRoleSets.ManageSettlement check only); handler is also dispatched concretely with a createdByEmail overload, not via the registered ICommandHandler.
    // Skipped: AddWorker — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: UpdateWorker — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: DeleteWorker — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: AddWorkerTimesheet — no Authorisation class (inline LabourRoleSets.ApproveTimesheets check only).
    // Skipped: AdjustTimesheet — no Authorisation class (inline LabourRoleSets.ApproveTimesheets check only).
    // Skipped: ApproveTimesheets — no Authorisation class (inline LabourRoleSets.ApproveTimesheets check only). (Distinct from Commercial's ApproveTimesheet, which is out of this file's scope.)
    // Skipped: RejectTimesheet — no Authorisation class (inline LabourRoleSets.ApproveTimesheets check only).
    // Skipped: SubmitWorkerWeek — no Authorisation class (inline LabourRoleSets.ApproveTimesheets check only). (Distinct from Commercial's SubmitTimesheet, which is out of this file's scope.)
    // Skipped: MySiteSignIn — no Authorisation class (inline LabourRoleSets.LogOwnTime check only).
    // Skipped: MySiteSignOut — no Authorisation class (inline LabourRoleSets.LogOwnTime check only).
    // Skipped: MyResubmitTimesheet — no Authorisation class (inline LabourRoleSets.LogOwnTime check only).
    // Skipped: SignOffLabourWeek — no Authorisation class (inline LabourRoleSets.ApproveTimesheets check only); endpoint also dispatches the concrete handler's signedOffByEmail overload, not the registered ICommandHandler.
    // Skipped: RemoveLabourWeekSignOff — no Authorisation class (inline LabourRoleSets.ApproveTimesheets check only).
    // Skipped: SetProjectWorkerAssignment — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: SetWorkerContract — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: SetWorkerCisStatus — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: RecordWorkerAbsence — no Authorisation class (inline LabourRoleSets.ManageWorkers check only); concrete handler dispatched with a recordedByEmail overload.
    // Skipped: RemoveWorkerAbsence — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: AddWorkerSettlementLine — no Authorisation class (inline LabourRoleSets.ManageSettlement check only).
    // Skipped: RemoveWorkerSettlementLine — no Authorisation class (inline LabourRoleSets.ManageSettlement check only).
    // Skipped: SetSiteXeroMapping — no Authorisation class (inline LabourRoleSets.ManageSettlement check only).
    // Skipped: SetCostCodeXeroMapping — no Authorisation class (inline LabourRoleSets.ManageSettlement check only).
    // Skipped: SetXeroLineTimesheetCover — no Authorisation class (inline LabourRoleSets.ManageSettlement check only).
    // Skipped: RunXeroCoding — no Authorisation class (inline LabourRoleSets.ManageSettlement check only).
    // Skipped: SyncXeroLedger — no Authorisation class (inline XeroLedgerRoles.AllowedToAllocate check only).
    // Skipped: AllocateSuggestedXeroLines — no Authorisation class (inline XeroLedgerRoles.AllowedToAllocate check only).
    // Skipped: RetryXeroWriteBack — no Authorisation class (inline XeroLedgerRoles.AllowedToAllocate check only).
    // Skipped: SetXeroAllocation — no Authorisation class (inline XeroLedgerRoles.AllowedToAllocate check only).
    // Skipped: SyncXeroSitePnl — no Authorisation class (inline XeroSitePnlRoles.AllowedToView check only).
    // Skipped: SaveRegisterItem — no Authorisation class (inline RegisterRoleSets.ManageRegisters check only).
    // Skipped: DeactivateRegisterItem — no Authorisation class (inline RegisterRoleSets.ManageRegisters check only).
    // Skipped: PublishPolicyDocument — no Authorisation class (inline RegisterRoleSets.ManageRegisters check only).
    // Skipped: SignPolicy — no Authorisation class, and its handler is not a registered ICommandHandler.
    // Skipped: Ping (Features/Health) — no command dispatch; a bare health probe.
}
