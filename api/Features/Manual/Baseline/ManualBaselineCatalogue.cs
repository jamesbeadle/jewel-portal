using System.Reflection;

namespace Jewel.JPMS.Api.Features.Manual.Baseline;

/// <summary>One module of the JBB baseline: its index entry and the markdown body embedded beside it.</summary>
public sealed record ManualBaselineEntry(
    string Code,
    string Title,
    string Purpose,
    string SourceSections,
    ManualAudience Audience,
    IReadOnlyList<string> LinkedFormSlugs,
    string LinkedStandards,
    string Body);

/// <summary>
/// The JBB Site Manager Manual v0.13 (May 2026, a working draft) carved into the modules of the
/// modular restructure, shipped as embedded markdown so the office loads it once and edits it in
/// the portal from then on. The index is baseline.json; each module's text is its code's .md file.
/// </summary>
public static class ManualBaselineCatalogue
{
    public const string ChangeSummary = "Loaded from the JBB Site Manager Manual v0.13 (May 2026, working draft). Review, name the owner and approver, then approve.";

    private const string ResourcePrefix = "Manual.Baseline.";
    private const string IndexResource = ResourcePrefix + "baseline.json";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private sealed record IndexEntry(
        string Code, string Title, string Purpose, string SourceSections,
        bool IsForSiteManagers, bool IsForHealthAndSafetyOfficer, bool IsForForemen,
        IReadOnlyList<string> LinkedFormSlugs, string LinkedStandards);

    public static IReadOnlyList<ManualBaselineEntry> Read()
    {
        var index = JsonSerializer.Deserialize<List<IndexEntry>>(ReadResource(IndexResource), Json) ?? new List<IndexEntry>();
        return index.Select(ToEntry).ToList();
    }

    private static ManualBaselineEntry ToEntry(IndexEntry entry) =>
        new(entry.Code, entry.Title, entry.Purpose, entry.SourceSections,
            new ManualAudience(entry.IsForSiteManagers, entry.IsForHealthAndSafetyOfficer, entry.IsForForemen),
            entry.LinkedFormSlugs, entry.LinkedStandards, ReadResource(ResourcePrefix + entry.Code + ".md").Trim());

    private static string ReadResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"The baseline resource {name} is missing from the build.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
