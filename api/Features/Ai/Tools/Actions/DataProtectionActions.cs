using Jewel.JPMS.Api.Features.DataProtection.Commands;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.DataProtection;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// The rights a person can exercise, over the connector as on the site (data protection,
/// 2026-09-21): erasure (anonymise_person, administrators, confirm-first, read get_person_dossier
/// first), a worker's contact details cleared (retire_worker, by name), and a prospect's
/// "please stop" recorded (withdraw_lead_marketing_consent).
/// </summary>
internal sealed class DataProtectionActions : IAiActionSource
{
    private const string Area = "Data protection";

    public IEnumerable<AiAction> Build() => new AiAction[]
    {
        new AiAction(
            Name: "anonymise_person",
            Area: Area,
            Description: "PERMANENTLY erases a person's details across the portal — the rows that are them "
                + "(a client's contact, a person at an architect or a directory record, a lead, their "
                + "imagine rounds, messages they wrote) lose their name, email, phone and free text, and "
                + "every column that names them (raised-by stamps, the audit actor) is rewritten to one "
                + "pseudonym. Nothing is deleted: orders, invoices and audit rows keep their money and "
                + "dates. There is no undo.",
            CommandType: typeof(AnonymisePerson),
            ResultType: typeof(PersonAnonymisation),
            AuthorisationType: typeof(AnonymisePersonAuthorisation),
            ValidationType: typeof(AnonymisePersonValidation),
            VisibleTo: JpmsRoleSets.Administrators,
            EmailStamps: new[] { "AnonymisedByEmail" },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Read get_person_dossier for the same email first and show the user what will be "
                + "erased. Refused while the address still has a sign-in (remove_directory_user, then "
                + "delete_directory_user) or names a worker with history who has not been retired "
                + "(retire_worker). The reason is written to the audit trail — the request's date, "
                + "never the person's name."),
        new AiAction(
            Name: "retire_worker",
            Area: Area,
            Description: "Retires a worker who has timesheet or register history: clears their contact "
                + "email and phone, marks them inactive and closes their engagement. Their name, rate "
                + "history and every timesheet stay, because recorded cost and the CIS returns are built "
                + "on them. A worker with no history is deleted on the Workers page instead.",
            CommandType: typeof(RetireWorkerByName),
            ResultType: typeof(Worker),
            AuthorisationType: typeof(RetireWorkerByNameAuthorisation),
            ValidationType: typeof(RetireWorkerByNameValidation),
            VisibleTo: LabourRoleSets.ManageWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "workerName as the register spells it (list_workers). Works on an inactive worker "
                + "too. Retiring is a step towards erasure, not erasure: follow with anonymise_person "
                + "if the person asks for everything."),
        new AiAction(
            Name: "withdraw_lead_marketing_consent",
            Area: Area,
            Description: "Records that a prospect has asked us to stop keeping in touch about their "
                + "home. From then on no proposal or follow-up is emailed to them; the concepts they "
                + "asked for are unaffected. Use when they say so on the phone or in a reply — their "
                + "own imagine page has the same door.",
            CommandType: typeof(WithdrawLeadMarketingConsent),
            ResultType: typeof(Lead),
            AuthorisationType: typeof(WithdrawLeadMarketingConsentAuthorisation),
            ValidationType: typeof(WithdrawLeadMarketingConsentValidation),
            VisibleTo: SalesRoles.SalesTeam,
            EmailStamps: new[] { "WithdrawnByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "leadId from list_leads or get_lead. get_lead's marketingConsent reads Given, "
                + "Withdrawn or NotRecorded afterwards.")
    };
}
