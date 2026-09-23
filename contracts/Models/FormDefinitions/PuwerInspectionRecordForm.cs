using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// I-03 PUWER Inspection Record (Akeva template, H&amp;S Officer File): the inspection log of the
/// equipment on a site, one row per inspection with the next one due, exactly as the paper sheet
/// rules it — the sheet's own worked example is a weekly check and a damaged cable removed from site.
/// </summary>
public static class PuwerInspectionRecordForm
{
    private static readonly FormTable Inspections = new(
        new[]
        {
            Column.Date("date", "Date"),
            Column.Text("description", "Description of equipment"),
            Column.Text("equipment_id", "Equipment ID no."),
            Column.Text("result", "Result of inspection"),
            Column.Text("action", "Action taken"),
            Column.Date("next_due", "Next inspection due"),
            Column.Text("signed", "Signed")
        },
        "inspection");

    public static readonly FormDefinition Definition = new(
        FormSlugs.PuwerInspection,
        "PUWER Inspection Record",
        "The I-03 PUWER inspection record, filled in on the portal instead of the paper sheet. Fields marked * are required.",
        new[]
        {
            Text("site", "Site address", Required),
            Table("inspections", "Inspections", Required, Inspections)
        },
        Store: FormEvidenceStore.HealthAndSafety,
        FilingKind: FormFilingKind.Site,
        FilingKeys: new[] { "site" });
}
