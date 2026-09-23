namespace Jewel.JPMS.Models;

/// <summary>The I-14 sheet's items and the quantity each kit size holds (Small / Medium / Large / Travel), row for row.</summary>
public static class FirstAidKitContents
{
    public static readonly IReadOnlyList<IReadOnlyDictionary<string, string>> Items = new[]
    {
        Row("Guidance leaflet", 1, 1, 1, 1),
        Row("Medium sterile dressing", 2, 4, 6, 1),
        Row("Large sterile dressing", 2, 3, 4, 0),
        Row("Triangular bandage", 2, 3, 4, 1),
        Row("Eye pad", 2, 3, 4, 0),
        Row("Sterile adhesive dressings", 40, 60, 100, 10),
        Row("Alcohol-free wipes", 20, 30, 40, 10),
        Row("Adhesive tape roll", 1, 2, 3, 0),
        Row("Nitrile gloves", 6, 9, 12, 2),
        Row("Finger dressing", 2, 3, 4, 0),
        Row("Resuscitation face shield", 1, 1, 2, 1),
        Row("Foil blanket", 1, 2, 3, 1),
        Row("Burns dressing", 1, 2, 2, 2),
        Row("Shears / scissors", 1, 1, 1, 1),
        Row("Adhesive dressings", 0, 0, 0, 1),
        Row("Trauma dressing (medium)", 0, 0, 0, 1)
    };

    public static readonly IReadOnlyList<IReadOnlyDictionary<string, string>> ExtraItems = new[]
    {
        Item("Plasters"),
        Item("Moist wipes"),
        Item("Sterile water / saline"),
        Item("Eye protection"),
        Item("Apron"),
        Item("Eye wash (in date)"),
        Item("Defibrillator")
    };

    private static IReadOnlyDictionary<string, string> Row(string item, int small, int medium, int large, int travel) =>
        new Dictionary<string, string>
        {
            ["item"] = item,
            ["small"] = small.ToString(),
            ["medium"] = medium.ToString(),
            ["large"] = large.ToString(),
            ["travel"] = travel.ToString()
        };

    private static IReadOnlyDictionary<string, string> Item(string item) => new Dictionary<string, string> { ["item"] = item };
}
