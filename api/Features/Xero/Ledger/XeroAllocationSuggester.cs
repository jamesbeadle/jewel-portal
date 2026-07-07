using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// Best-guess project and master-cost-centre suggestions for an unallocated
/// ledger line, derived from its Xero tracking. Never applied automatically —
/// the allocation page pre-selects these and the user confirms.
///
/// Project: the Xero "Sites" option is matched against project names
/// (normalised equality first, then containment either way).
///
/// Cost centre: Xero's own cost-code options ("00006-04 Masonry") predate the
/// master list, so the code part is ignored and the NAME is matched against
/// the master cost-centre names via a curated alias map, then normalised
/// equality/containment as fallback.
/// </summary>
public sealed class XeroAllocationSuggester
{
    // Xero cost-code names -> master code. Curated from the tracking options in use.
    private static readonly Dictionary<string, string> CostCodeAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Foundations"] = "00022",              // Groundworks
        ["Masonry"] = "00028",                  // Masonry brickworks
        ["Site Manager"] = "00016",
        ["Project Manager"] = "00017",
        ["Electrics"] = "00072",                // Electrician
        ["Windows & Doors"] = "00050",          // Timber windows and doors (review per line)
        ["Structural Metal Members PS"] = "00010",
        ["Structural Works PS"] = "00019",      // Structural support
        ["Roofing & Guttering"] = "00030",      // Roofer
        ["Hard Landscaping"] = "00117",         // Landscaping
        ["Plumbing"] = "00074",                 // Plumber
        ["Plastering"] = "00062",
        ["Scaffolding"] = "00020",
        ["Supply of Sanitary PS"] = "00122",    // Sanitary supply
        ["Sanitary PS"] = "00122",
        ["Kitchen & Appliances PS"] = "00125",  // Kitchen supply
        ["Gypsum Board"] = "00060",             // Plaster Boarding
        ["Air Con PS"] = "00089",
        ["Staircase"] = "00048",                // Stairs timber
        ["Carpentry 1st Fix"] = "00041",
        ["Carpentry 2nd Fix"] = "00042",
        ["Demolition"] = "00018",
        ["Excavation"] = "00021",
        ["Hire Costs"] = "00134",               // no hire bucket in the master list -> Misc
        ["Labour Costs"] = "00015",
        ["Rubbish Removal"] = "00131",          // Rubbish clearance
        ["Welfare/Hoarding"] = "00004",         // Welfare
        ["Temp Toilet"] = "00007",
        ["Temp Plumbing & Electrics"] = "00002",// Site set up
        ["Trenching & Services PS"] = "00121",
        ["Health & Safety"] = "00005",
        ["CDM"] = "00006",
        ["Asbestos"] = "00114",
        ["Builders Clean"] = "00130",           // Cleaning internal
        ["Floor Finishes"] = "00070",           // Floor finish LVT/Lino (review per line)
        ["Painting"] = "00082",                 // Decorating
        ["Tiling"] = "00076",
        ["Lift & Hoists PS"] = "00099",
        ["Misc Items"] = "00134",
    };

    private readonly List<(string Normalised, string ProjectId)> projects;
    private readonly List<(string Normalised, string Code)> costCenters;

    public XeroAllocationSuggester(
        IEnumerable<ProjectEntity> projects,
        IEnumerable<CostCenterEntity> activeCostCenters)
    {
        this.projects = projects
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => (Normalise(p.Name), p.ProjectId))
            .ToList();
        costCenters = activeCostCenters
            .Select(c => (Normalise(c.Name), c.Code))
            .ToList();
    }

    public string? SuggestProject(string? xeroSite)
    {
        if (string.IsNullOrWhiteSpace(xeroSite)) return null;
        var site = Normalise(xeroSite);

        foreach (var (name, id) in projects)
            if (name == site) return id;
        foreach (var (name, id) in projects)
            if (name.Length >= 6 && (site.Contains(name) || name.Contains(site))) return id;
        return null;
    }

    public string? SuggestCostCenter(string? xeroCostCode)
    {
        if (string.IsNullOrWhiteSpace(xeroCostCode)) return null;

        // "00006-04 Masonry" -> "Masonry"; some options have no numeric prefix.
        var name = StripLeadingCode(xeroCostCode);
        if (CostCodeAliases.TryGetValue(name, out var alias)) return alias;

        var normalised = Normalise(name);
        foreach (var (ccName, code) in costCenters)
            if (ccName == normalised) return code;
        foreach (var (ccName, code) in costCenters)
            if (normalised.Length >= 5 && (ccName.Contains(normalised) || normalised.Contains(ccName))) return code;
        return null;
    }

    private static string StripLeadingCode(string value)
    {
        var trimmed = value.Trim();
        var firstSpace = trimmed.IndexOf(' ');
        if (firstSpace <= 0) return trimmed;
        var prefix = trimmed[..firstSpace];
        // Prefixes look like 00006-04, 0002-3, 10002 — digits and dashes only.
        return prefix.All(ch => char.IsDigit(ch) || ch == '-') ? trimmed[(firstSpace + 1)..].Trim() : trimmed;
    }

    private static string Normalise(string value) =>
        new(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
}
