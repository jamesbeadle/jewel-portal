namespace Jewel.JPMS.Models;

/// <summary>The G-03 environmental list (26 talks), numbered as Akeva numbers them.</summary>
public static partial class ToolboxTalkTopics
{
    private static string[] EnvironmentalTalks() => new[]
    {
        "G-03 1 Dust & Air Quality",
        "G-03 2 Silt",
        "G-03 3 Concrete and Cement run-off",
        "G-03 4 Tree Protection",
        "G-03 5 Himalayan Balsam",
        "G-03 6 Giant Hogweed",
        "G-03 7 Bats",
        "G-03 8 Badgers",
        "G-03 9 Great Crested Newts",
        "G-03 10 Storage of Waste",
        "G-03 11 Waste Management",
        "G-03 12 Waste Segregation",
        "G-03 13 Fuel & Oil pollution",
        "G-03 14 Spill Control",
        "G-03 15 Japanese Knotweed",
        "G-03 16 Previously Developed Land",
        "G-03 17 Be Neighbourly",
        "G-03 18 Fuel & Oil Storage",
        "G-03 19 Material Handling",
        "G-03 20 Noise & Vibration",
        "G-03 21 Washing Down Plant",
        "G-03 22 Dewatering",
        "G-03 23 Bentonite",
        "G-03 24 Archaeology",
        "G-03 25 Wildlife",
        "G-03 26 Energy and Water Efficiency",
    };

    /// <summary>Every talk, the general list first, each reading "G-02 41 Ladders and Step Ladders".</summary>
    public static readonly string[] All = GeneralTalks().Concat(EnvironmentalTalks()).ToArray();
}
