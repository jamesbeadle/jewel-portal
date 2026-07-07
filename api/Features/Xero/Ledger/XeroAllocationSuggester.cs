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
    // Xero cost-code names -> master code (JBB Cost Code Master, trade-prefixed,
    // per JBB_CostCode_Master v2.1). Xero's numeric tracking options are the
    // legacy numbering; this map is how they translate. Curated from the
    // tracking options in use.
    private static readonly Dictionary<string, string> CostCodeAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Foundations"] = "SUB-GWK",              // Groundworks
        ["Masonry"] = "MASON-BRK",                // Masonry brickworks
        ["Site Manager"] = "PRELIMS-SMG",
        ["Project Manager"] = "PRELIMS-PMG",
        ["Electrics"] = "ELE-STD",                // Electrician
        ["Windows & Doors"] = "WDR-TIM",          // Timber windows and doors (review per line)
        ["Structural Metal Members PS"] = "STR-STL",
        ["Structural Works PS"] = "ENABLE-STS",   // Structural support
        ["Roofing & Guttering"] = "ROOF-RFR",     // Roofer
        ["Hard Landscaping"] = "EXTW-LND",        // Landscaping
        ["Plumbing"] = "MEC-PLM",                 // Plumber
        ["Plastering"] = "INT-PLS",
        ["Scaffolding"] = "SCAFF-STD",
        ["Supply of Sanitary PS"] = "SUP-SAN",    // Sanitary supply
        ["Sanitary PS"] = "SUP-SAN",
        ["Kitchen & Appliances PS"] = "SUP-KIT",  // Kitchen supply
        ["Gypsum Board"] = "INT-PLB",             // Plaster Boarding
        ["Air Con PS"] = "MEC-AC",
        ["Staircase"] = "STAIR-TIM",              // Stairs timber
        ["Carpentry 1st Fix"] = "CARP-1FX",
        ["Carpentry 2nd Fix"] = "CARP-2FX",
        ["Demolition"] = "ENABLE-DEM",
        ["Excavation"] = "SUB-EXC",
        ["Hire Costs"] = "HAND-MSC",              // no plant-hire code in the master -> Misc
        ["Labour Costs"] = "PRELIMS-LAB",
        ["Rubbish Removal"] = "ENABLE-SKP",       // Skips
        ["Welfare/Hoarding"] = "PRELIMS-WEL",     // Welfare
        ["Temp Toilet"] = "PRELIMS-WC",
        ["Temp Plumbing & Electrics"] = "PRELIMS-SET", // Site set up
        ["Trenching & Services PS"] = "UTIL-TRN",
        ["Health & Safety"] = "PRELIMS-HSO",
        ["CDM"] = "PRELIMS-HSC",
        ["Asbestos"] = "ENABLE-ASB",
        ["Builders Clean"] = "HAND-CLI",          // Cleaning internal
        ["Floor Finishes"] = "FLR-LVT",           // Floor finish LVT/Lino (review per line)
        ["Painting"] = "DEC-STD",                 // Decorating
        ["Tiling"] = "TIL-STD",
        ["Lift & Hoists PS"] = "SPEC-LFT",
        ["Misc Items"] = "HAND-MSC",
    };

    private readonly List<(string Normalised, string ProjectId)> projects;
    private readonly List<(string Normalised, string Code)> costCenters;
    private readonly HashSet<string> activeCodes;

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
        activeCodes = costCenters.Select(c => c.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
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
        // An alias is only suggested if that code is actually active — a retired
        // master code would fail allocation validation and render as a blank
        // dropdown on the allocation page.
        var name = StripLeadingCode(xeroCostCode);
        if (CostCodeAliases.TryGetValue(name, out var alias) && activeCodes.Contains(alias)) return alias;

        var normalised = Normalise(name);
        foreach (var (ccName, code) in costCenters)
            if (ccName == normalised) return code;
        foreach (var (ccName, code) in costCenters)
            if (normalised.Length >= 5 && (ccName.Contains(normalised) || normalised.Contains(ccName))) return code;
        return null;
    }

    // Supplier/description patterns for lines that are cost of sales but belong to a
    // bucket rather than a project (no site tracking to match). Ordered; first hit wins.
    private static readonly (string Pattern, string Bucket)[] BucketRules =
    {
        (@"parking|\bpcn\b|paybyphone|parkpcm|\bncp\b|ringgo|justpark", XeroBuckets.Parking),
        (@"\bfuel\b|\bshell\b|\bbp\b|\besso\b|texaco|\bgulf\b|petrol|diesel", XeroBuckets.Fuel),
        (@"subscription|software|licen[cs]e|microsoft|azure|adobe|planyard|xero custom|\bsaas\b|dns filter", XeroBuckets.Software),
    };

    /// <summary>
    /// Best-guess bucket for a line with no project match — suggested only, never applied
    /// automatically. Based on supplier name and line description.
    /// </summary>
    public string? SuggestBucket(string? contactName, string? description)
    {
        var text = $"{contactName} {description}".ToLowerInvariant();
        foreach (var (pattern, bucket) in BucketRules)
            if (System.Text.RegularExpressions.Regex.IsMatch(text, pattern)) return bucket;
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
