using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// I-01 Work Equipment Inspection Schedule (Akeva template, H&amp;S Officer File): the register of the
/// equipment on a site, one row per item, exactly as the paper sheet rules it.
/// </summary>
public static class WorkEquipmentScheduleForm
{
    private static readonly FormTable Equipment = new(
        new[]
        {
            Column.Text("description", "Description of equipment"),
            Column.Text("make", "Make / manufacturer"),
            Column.Text("equipment_id", "Equipment ID no."),
            Column.Text("owner", "Owner / user company"),
            Column.Text("frequency", "Inspection frequency"),
            Column.Date("brought_on", "Date brought on site"),
            Column.Date("removed", "Date removed from site")
        },
        "item of equipment");

    public static readonly FormDefinition Definition = new(
        FormSlugs.EquipmentSchedule,
        "Work Equipment Inspection Schedule",
        "The I-01 schedule of the work equipment on site, filled in on the portal instead of the paper sheet. "
            + "Fields marked * are required.",
        new[]
        {
            Text("site", "Site address", Required),
            Table("equipment", "Equipment", Required, Equipment)
        },
        Store: FormEvidenceStore.HealthAndSafety,
        FilingKind: FormFilingKind.Site,
        FilingKeys: new[] { "site" });
}
