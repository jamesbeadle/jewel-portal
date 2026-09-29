using System.Text.RegularExpressions;

namespace Jewel.JPMS.Models;

/// <summary>
/// A module's code is its family and its number — GOV-01, RUN-02, WFL-03 — read straight off the
/// modular restructure. The family is what groups the master and orders the site views.
/// </summary>
public static class ManualModuleCodes
{
    private static readonly Regex Shape = new(@"^[A-Z]{2,4}-\d{2}$", RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, string> FamilyNames = new Dictionary<string, string>
    {
        ["GOV"] = "Governance",
        ["ROLE"] = "Role",
        ["MOB"] = "Pre-start and mobilisation",
        ["SET"] = "Site setup",
        ["RUN"] = "Routines",
        ["REP"] = "Reporting",
        ["WFL"] = "Workflows",
        ["RES"] = "Resources and procurement",
        ["STD"] = "Standards",
        ["COM"] = "Compliance",
        ["REF"] = "Reference",
    };

    private static readonly List<string> FamilyList = FamilyNames.Keys.ToList();

    public static IReadOnlyList<string> Families => FamilyList;

    public static bool IsWellFormed(string code) => Shape.IsMatch(code);

    public static string FamilyOf(string code)
    {
        var separator = code.IndexOf('-');
        if (separator < 0) return code;
        return code[..separator];
    }

    public static string FamilyName(string family) =>
        FamilyNames.TryGetValue(family, out var name) ? name : family;

    public static int FamilyOrder(string family)
    {
        var order = FamilyList.IndexOf(family);
        if (order < 0) return FamilyList.Count;
        return order;
    }
}
