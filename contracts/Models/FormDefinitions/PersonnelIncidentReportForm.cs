using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// K-03 Personnel Incident Report (Akeva template, Site Managers Working File): the injured person's
/// fuller record, as the paper form asks it. It carries their NI number, date of birth, home address
/// and the injury — special category data: the NI number, date of birth and sex are sensitive by
/// key and never echoed, the injury and its treatment are health answers withheld until someone who
/// may see them reveals them, and the whole form lives in the health and safety store, so the
/// office alert carries no answers at all.
/// </summary>
public static class PersonnelIncidentReportForm
{
    private static readonly string[] Statuses = { "Employee", "Contractor", "Agency", "Member of the public" };

    public static readonly FormDefinition Definition = new(
        FormSlugs.PersonnelIncident,
        "Personnel Incident Report",
        "The K-03 personnel incident report, filled in on the portal instead of the paper form — one per injured person. "
            + "Fields marked * are required.",
        new[]
        {
            Text("site", "Project", Required),
            Section("_sec_injured", "Injured person"),
            Text("injured_name", "Full name", Required),
            LongText("injured_address", "Address", Optional),
            Text("occupation", "Occupation", Optional),
            Text("ni_number", "National Insurance number", Optional),
            Choice("sex", "Sex", Optional, new[] { "Female", "Male" }),
            Date("dob", "Date of birth", Optional),
            Choice("injured_status", "Status", Optional, Statuses),
            Section("_sec_completing", "Person completing this report"),
            Text("completer_name", "Name", Required),
            LongText("completer_address", "Address", Optional),
            Text("completer_occupation", "Occupation", Optional),
            Text("completer_tel", "Telephone", Optional),
            LongText("witnesses", "Witness names", Optional, "A separate witness form is completed for each witness."),
            Section("_sec_incident", "The incident"),
            DateAndTime("occurred_at", "Date and time of the incident", Required),
            DateAndTime("reported_at", "Date and time reported", Optional),
            Text("where", "Where it happened", Required),
            LongText("injury", "Injury", Required) with { IsSpecialCategory = true },
            Text("body_part", "Part of body", Optional) with { IsSpecialCategory = true },
            LongText("how", "How it happened (cause)", Required) with { IsSpecialCategory = true },
            LongText("immediate_action", "Immediate action taken (treatment, prevention)", Optional) with { IsSpecialCategory = true },
            Section("_sec_employer", "Employer"),
            Date("returned_to_work", "Date of return to work", Optional),
            Text("absence", "Days / hours absent", Optional) with { IsSpecialCategory = true },
            Choice("riddor", "Reportable under RIDDOR", Optional, new[] { FormWording.Yes, FormWording.No }),
            Date("riddor_reported_on", "Date reported to the HSE", Optional) with { ShownWhen = new("riddor", FormWording.Yes) },
            Text("employer_company", "Company", Optional),
            Signature("employer_signature", "Signature", Required, "The date is recorded automatically when you submit."),
            LongText("notes", "Notes (duties on return, investigation outcome, claims)", Optional) with { IsSpecialCategory = true }
        },
        Store: FormEvidenceStore.HealthAndSafety,
        FilingKind: FormFilingKind.Site,
        FilingKeys: new[] { "site" },
        IsAnAccidentReport: true);
}
