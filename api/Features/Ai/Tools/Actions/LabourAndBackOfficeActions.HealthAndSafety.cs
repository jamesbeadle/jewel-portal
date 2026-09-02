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
    };
}
