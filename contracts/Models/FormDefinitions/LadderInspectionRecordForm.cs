using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// I-05 Ladder, Stepladder + Hop-up Inspection Record (Akeva template, H&amp;S Officer File): a running
/// log, one row per inspection, exactly as the paper sheet rules it. The site names the folder the
/// record is filed under, as the sheet's header would.
/// </summary>
public static class LadderInspectionRecordForm
{
    private static readonly FormTable Inspections = new(
        new[]
        {
            Column.Date("date", "Date"),
            Column.Text("description", "Description"),
            Column.Text("location", "Location"),
            Column.Text("hazards", "Hazards identified"),
            Column.Text("action", "Action taken"),
            Column.Text("inspector", "Inspection carried out by"),
            Column.Text("signed", "Signed")
        },
        "inspection");

    public static readonly FormDefinition Definition = new(
        FormSlugs.LadderInspection,
        "Ladder, Stepladder and Hop-up Inspection Record",
        "The I-05 inspection record, filled in on the portal instead of the paper sheet. Fields marked * are required.",
        new[]
        {
            Text("site", "Site", Required),
            Table("inspections", "Inspections", Required, Inspections)
        },
        Store: FormEvidenceStore.HealthAndSafety,
        FilingKind: FormFilingKind.Site,
        FilingKeys: new[] { "site" });
}
