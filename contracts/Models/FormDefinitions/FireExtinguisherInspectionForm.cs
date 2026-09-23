using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// E-02 Site Fire Extinguisher Inspection Record (Akeva template, H&amp;S Officer File): the site, one
/// row per extinguisher — fire point, location, type, capacity and the sheet's eight dated check
/// columns, a tick per visit — and the footer, exactly as the paper sheet rules it and no more: no
/// servicing, no supplier, no reminders (Nigel, 23 Sep 2026).
/// </summary>
public static class FireExtinguisherInspectionForm
{
    private const int ChecksOnTheSheet = 8;

    private static readonly FormTable Extinguishers = new(
        new[]
        {
            Column.Text("fire_point", "Fire point no."),
            Column.Text("location", "Location"),
            Column.Text("type", "Type"),
            Column.Text("capacity", "Capacity")
        }.Concat(CheckColumns()).ToList(),
        "extinguisher");

    public static readonly FormDefinition Definition = new(
        FormSlugs.FireExtinguishers,
        "Site Fire Extinguisher Inspection Record",
        "The E-02 fire extinguisher inspection record, filled in on the portal instead of the paper sheet: one row per "
            + "extinguisher, with the date of each check. Fields marked * are required.",
        new[]
        {
            Text("site", "Site location", Required),
            Table("extinguishers", "Extinguishers", Required, Extinguishers),
            Section("_sec_footer", "Carried out by"),
            Text("carried_out_by", "Carried out by", Required),
            Text("initials", "Initials", Required)
        },
        Store: FormEvidenceStore.HealthAndSafety,
        FilingKind: FormFilingKind.Site,
        FilingKeys: new[] { "site" });

    private static IEnumerable<FormColumn> CheckColumns() =>
        Enumerable.Range(1, ChecksOnTheSheet).Select(number => Column.Date($"check_{number}", $"Check {number} - date"));
}
