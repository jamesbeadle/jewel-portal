using Jewel.JPMS.Api.Features.Hs.Audits;
using Jewel.JPMS.Api.Features.Hs.Audits.Commands;
using Jewel.JPMS.Api.Features.Hs.Commands;
using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class LabourAndBackOfficeActions
{
    private static IEnumerable<AiAction> HealthAndSafetyActions() => new AiAction[]
    {
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
                + "or Critical. The assignee is assignedToEmail (a portal user, from "
                + "list_portal_users) OR assignedToName (a person with no login — the site "
                + "manager, say); one of the two is required. list_hs_records reads the register."),

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
            Notes: "hsRecordId comes from list_hs_records. All listed fields are replaced — carry "
                + "forward what should not change, assignedToName included. Closing a corrective "
                + "action minted by a site audit stamps that audit item's date rectified."),

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

        new AiAction(
            Name: "create_hs_audit",
            Area: "Health & safety",
            Description: "Starts an H&S site audit on a project — the header (type, inspection "
                + "date, safety officer, site manager, summary of activities, operative count) — "
                + "and plants every item of the current inspection framework blank (11 sections, "
                + "165 items, version 2026-09-15). Returns the audit with its HSA reference; get_hs_audit then lists the "
                + "items with the ids update_hs_audit_items takes.",
            CommandType: typeof(CreateHsAudit),
            ResultType: typeof(HsAudit),
            AuthorisationType: typeof(CreateHsAuditAuthorisation),
            ValidationType: typeof(CreateHsAuditValidation),
            VisibleTo: HsAuditRoles.Auditors,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. details.type is Initial, Routine, "
                + "FollowUp, Final or Other; details.inspectionDate is a UK calendar date."),

        new AiAction(
            Name: "update_hs_audit_details",
            Area: "Health & safety",
            Description: "Replaces a site audit's header wholesale — type, inspection date, "
                + "safety officer, site manager, summary of activities, operative count, further "
                + "comments. Refused on a Closed audit.",
            CommandType: typeof(UpdateHsAuditDetails),
            ResultType: typeof(HsAudit),
            AuthorisationType: typeof(UpdateHsAuditDetailsAuthorisation),
            ValidationType: typeof(UpdateHsAuditDetailsValidation),
            VisibleTo: HsAuditRoles.Auditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "hsAuditId comes from list_hs_audits. Read the audit first and carry forward "
                + "what should not change."),

        new AiAction(
            Name: "update_hs_audit_items",
            Area: "Health & safety",
            Description: "Writes what the officer found on any number of a site audit's items — "
                + "each entry replaces that item's comment, rate (0 / 5 / 10), class (A–E), "
                + "time-scale, findings, owner name and date rectified wholesale; items not named "
                + "are untouched (minus is carried but no longer scored). The score is recomputed "
                + "from every item after the write (the officer's sheet's rule: Σ rate ÷ (rated × 10), "
                + "unrated items excluded, less a penalty for each class present on the report once — "
                + "A 25%, B 15%, C 5%, D 1% — and 5% once if any item is a repeat).",
            CommandType: typeof(UpdateHsAuditItems),
            ResultType: typeof(HsAuditView),
            AuthorisationType: typeof(UpdateHsAuditItemsAuthorisation),
            ValidationType: typeof(UpdateHsAuditItemsValidation),
            VisibleTo: HsAuditRoles.Auditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "items[].hsAuditItemId comes from get_hs_audit — never the item code. Enums by "
                + "name: comment NotApplicable / Note / NotChecked / NotSeen / Repeat; rate "
                + "NotInPlace / OneWeekOutOfDate / UpToDate; class A–E; timeScale Immediately / "
                + "WithinOneDay / WithinThreeDays / WithinSevenDays / WithinOneMonth / Ongoing. "
                + "Read the item first and carry forward what should not change."),

        new AiAction(
            Name: "issue_hs_audit",
            Area: "Health & safety",
            Description: "The safety officer's declaration on a Draft site audit: moves it to "
                + "Issued and mints one corrective action on the project's H&S register for every "
                + "finding — an item with an owner named or a rate below 10 that is not marked N/A "
                + "— with severity from the class (A Critical, B High, C Medium, else Low), the due "
                + "date from the time-scale and the owner's name as the assignee. Refused unless at "
                + "least one item is rated. Say what will be minted and get the user's yes first.",
            CommandType: typeof(IssueHsAudit),
            ResultType: typeof(HsAuditView),
            AuthorisationType: typeof(IssueHsAuditAuthorisation),
            ValidationType: null,
            VisibleTo: HsAuditRoles.Auditors,
            EmailStamps: new[] { "IssuedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "hsAuditId comes from list_hs_audits. Read get_hs_audit first and list the "
                + "findings that will become corrective actions in the confirm turn.",
            RequiresConfirmation: true),

        new AiAction(
            Name: "close_hs_audit",
            Area: "Health & safety",
            Description: "The site manager's declaration that every action from an Issued audit "
                + "is done: moves it to Closed. Refused while any corrective action minted from "
                + "the audit is still open — close those on the register first (update_hs_record).",
            CommandType: typeof(CloseHsAudit),
            ResultType: typeof(HsAudit),
            AuthorisationType: typeof(CloseHsAuditAuthorisation),
            ValidationType: typeof(CloseHsAuditValidation),
            VisibleTo: HsAuditRoles.Auditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "hsAuditId comes from list_hs_audits; managerName is the person making the "
                + "declaration."),
    };
}
