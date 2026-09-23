using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// K-02 Site Incident Report Form (Akeva template, Site Managers Working File), as the paper form
/// asks it: the incident, the injured person, what happened and what was done, who at head office
/// was told, and the head-office box the office fills in afterwards. An accident report: the alert
/// goes to the accident addresses the hour it lands, and the answers stay in the portal (the
/// health and safety store) — the alert carries none of them.
/// </summary>
public static class SiteIncidentReportForm
{
    private static readonly string[] Statuses = { "Employee", "Contractor", "Member of the public" };

    public static readonly FormDefinition Definition = new(
        FormSlugs.SiteIncident,
        "Site Incident Report",
        "The K-02 site incident report, filled in on the portal instead of the paper form. Complete one after every "
            + "incident. Fields marked * are required.",
        new[]
        {
            LongText("summary", "Brief description of the incident", Required),
            Text("site", "Site / workplace address", Required),
            Date("date", "Date", Required),
            Text("location", "Location", Required),
            Text("time", "Time", Required),
            Text("manager", "Manager's name", Required),
            Text("manager_tel", "Manager's telephone", Optional),
            Text("supervisor", "Supervisor's name", Optional),
            Text("supervisor_tel", "Supervisor's telephone", Optional),
            Section("_sec_injured", "Injured person"),
            Text("injured_name", "Name", Optional),
            Text("injured_job_title", "Job title", Optional),
            LongText("injured_address", "Address", Optional),
            Text("injury_type", "Type of injury", Optional) with { IsSpecialCategory = true },
            Text("body_part", "Part of body", Optional) with { IsSpecialCategory = true },
            Choice("injured_status", "Status", Optional, Statuses),
            Section("_sec_account", "The incident"),
            LongText("description", "Description of the incident", Required),
            LongText("conclusions", "Conclusions (reason for the incident)", Optional),
            LongText("immediate_actions", "Immediate actions taken", Optional),
            LongText("further_actions", "Further actions required", Optional),
            Date("further_actions_complete", "Date further actions complete", Optional),
            Text("head_office_informed", "Who at head office was informed", Required),
            Section("_sec_signed", "Signed"),
            Text("reporter_position", "Position", Required),
            Signature("reporter_signature", "Signed (print your name and sign)", Required,
                "The date is recorded automatically when you submit."),
            Section("_sec_head_office", "Head office use only", "Leave blank on site — head office completes this box."),
            Text("incident_reference", "Incident reference", Optional),
            Date("report_received", "Date report received", Optional),
            Text("closed_out_by", "Closed out (signed / print)", Optional),
            Choice("riddor", "RIDDOR 2013 reportable", Optional, new[] { FormWording.Yes, FormWording.No }),
            Text("riddor_reported_by", "Reported by", Optional) with { ShownWhen = new("riddor", FormWording.Yes) },
            Text("riddor_job_title", "Job title", Optional) with { ShownWhen = new("riddor", FormWording.Yes) },
            Date("riddor_reported_on", "Date reported", Optional) with { ShownWhen = new("riddor", FormWording.Yes) },
            Text("riddor_reference", "RIDDOR reference", Optional) with { ShownWhen = new("riddor", FormWording.Yes) }
        },
        Store: FormEvidenceStore.HealthAndSafety,
        FilingKind: FormFilingKind.Site,
        FilingKeys: new[] { "site" },
        IsAnAccidentReport: true);
}
