using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// I-14 First Aid Kit Contents Checklist (Akeva template, H&amp;S Officer File): the header, the
/// sixteen items with the quantity each kit size should hold, the extra items an assessment may
/// call for, comments and the confirmation — the paper sheet's rows, filled in on the portal.
/// </summary>
public static class FirstAidKitChecklistForm
{
    private static readonly string[] KitSizes = { "Small", "Medium", "Large", "Travel" };
    private static readonly string[] YesOrNo = { FormWording.Yes, FormWording.No };

    private static readonly FormTable Contents = new(
        new[]
        {
            Column.Label("item", "Item"),
            Column.Label("small", "Small"),
            Column.Label("medium", "Medium"),
            Column.Label("large", "Large"),
            Column.Label("travel", "Travel"),
            Column.Choice("suitable", "Suitable", YesOrNo),
            Column.Text("actions", "Actions")
        },
        "item",
        FirstAidKitContents.Items);

    private static readonly FormTable ExtraItems = new(
        new[]
        {
            Column.Label("item", "Item"),
            Column.Choice("suitable", "Suitable", YesOrNo),
            Column.Text("actions", "Actions")
        },
        "item",
        FirstAidKitContents.ExtraItems);

    public static readonly FormDefinition Definition = new(
        FormSlugs.FirstAidKit,
        "First Aid Kit Contents Checklist",
        "The I-14 first aid kit contents checklist, filled in on the portal instead of the paper sheet. The quantity "
            + "columns are what each kit size should hold. Fields marked * are required.",
        new[]
        {
            Text("site", "Site address", Required),
            Text("kit_location", "Kit location", Required),
            Choice("kit_size", "Kit size", Required, KitSizes),
            Table("contents", "Contents", Required, Contents),
            Table("extra_items", "Extra items per assessment", Optional, ExtraItems),
            LongText("comments", "Comments", Optional),
            Section("_sec_confirmation", "Confirmation"),
            Text("checked_by", "Name", Required),
            Text("checked_by_job_title", "Job title", Required),
            Signature("checked_by_signature", "Signature", Required),
            Date("checked_on", "Date", Required)
        },
        Store: FormEvidenceStore.HealthAndSafety,
        FilingKind: FormFilingKind.Site,
        FilingKeys: new[] { "site" });
}
