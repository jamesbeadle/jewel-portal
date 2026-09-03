using Jewel.JPMS.Api.Features.Kpi;
using Jewel.JPMS.Api.Features.Kpi.Commands;
using Jewel.JPMS.Contracts.Kpi;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// The KPI register's writes over the connector (2026-09-03) — administrators only, mirroring
/// the endpoints' gate exactly (KpiRoles). The KPI itself is invisible in the mailbox — the
/// email is tagged only JPMS/Admin (+ Internal pathway) so it leaves the triage queue; the tag
/// says an administrator dealt with it, never that it is a KPI.
/// </summary>
internal sealed class KpiActions : IAiActionSource
{
    private const string PersonNotes =
        "The person is the one the KPI is ABOUT — usually the Jewel staff member who sent or "
        + "handled the email, not the marker. Name them ONE way: personId (list_kpi_people), "
        + "personEmail (a portal user's sign-in address from list_portal_users) or personName "
        + "(someone without a login, e.g. \"James Clark\" — matched to an existing name, else "
        + "added on the spot). Confirm the person with the user when the email names more than "
        + "one member of staff.";

    public IEnumerable<AiAction> Build() => new AiAction[]
    {
        new AiAction(
            Name: "mark_email_as_kpi",
            Area: "KPI",
            Description: "Marks a mailbox email as a KPI filed under one person at Jewel "
                + "(administrators only). The email is read back from the mailbox and its "
                + "subject, sender and date snapshotted onto the KPI row, then tagged JPMS/Admin "
                + "(+ the Internal pathway) so it leaves the triage queue — the tag says only that "
                + "an administrator handled it; nobody triaging can see it is a KPI. Optional note "
                + "says why it's a KPI. Marking the same email for the same person twice keeps the "
                + "one row (a new note replaces the old) and re-applies the tag. Answers with the "
                + "KPI-#### reference.",
            CommandType: typeof(MarkEmailAsKpi),
            ResultType: typeof(KpiEmail),
            AuthorisationType: typeof(MarkEmailAsKpiAuthorisation),
            ValidationType: typeof(MarkEmailAsKpiValidation),
            VisibleTo: KpiRoles.Administrators,
            EmailStamps: new[] { "MarkedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is the mailbox message id (search_mailbox / list_triage_queue / "
                + "get_mailbox_message give it; send internetMessageId too when you have it). "
                + "scope defaults to MessageOnly (the one email leaves the queue); EntireThread "
                + "tags its whole conversation out too. "
                + PersonNotes),

        new AiAction(
            Name: "update_kpi_email",
            Area: "KPI",
            Description: "Re-files a KPI under a different person and/or rewrites its note "
                + "(administrators only). The email snapshot and the KPI-#### reference never "
                + "change. The person and the note are replaced together — read the row with "
                + "list_kpi_emails first and resend what should not change.",
            CommandType: typeof(UpdateKpiEmail),
            ResultType: typeof(KpiEmail),
            AuthorisationType: typeof(UpdateKpiEmailAuthorisation),
            ValidationType: typeof(UpdateKpiEmailValidation),
            VisibleTo: KpiRoles.Administrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "kpiEmailId comes from list_kpi_emails. " + PersonNotes),

        new AiAction(
            Name: "remove_kpi_email",
            Area: "KPI",
            Description: "Takes the KPI mark off an email — deletes the KPI row permanently "
                + "(administrators only). The email keeps its JPMS/Admin tag (so it stays out of "
                + "the queue; removing the tag in the Control Centre's Tagged tab returns it). The "
                + "person stays on the list. There is no undo: mark it again if it was a "
                + "mistake.",
            CommandType: typeof(RemoveKpiEmail),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RemoveKpiEmailAuthorisation),
            ValidationType: null,
            VisibleTo: KpiRoles.Administrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "kpiEmailId comes from list_kpi_emails. In the confirm turn show the reference, "
                + "the person and the subject so the user sees exactly which mark goes."),

        new AiAction(
            Name: "add_kpi_person",
            Area: "KPI",
            Description: "Adds someone KPIs can be filed under who has no portal login — by name "
                + "(\"James Clark\") — or links a portal user by their sign-in email "
                + "(administrators only). Idempotent: an existing match (case-insensitive name, "
                + "or the user's email) is answered rather than duplicated. Not needed before "
                + "mark_email_as_kpi, which adds a person on the spot — use this when the user "
                + "wants the person on the list ahead of any KPI.",
            CommandType: typeof(AddKpiPerson),
            ResultType: typeof(KpiPerson),
            AuthorisationType: typeof(AddKpiPersonAuthorisation),
            ValidationType: typeof(AddKpiPersonValidation),
            VisibleTo: KpiRoles.Administrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "name is required unless email (a portal user from list_portal_users) is given."),
    };
}
