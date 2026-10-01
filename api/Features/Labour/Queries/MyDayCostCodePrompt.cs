using System.Text;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>What Claude is asked when the trade-word rulebook cannot place the day's words on one
/// of the site's cost codes: the words, the site's codes as the only candidates, and one code or
/// nothing back as strict JSON.</summary>
public static class MyDayCostCodePrompt
{
    private const string NoCode = "";

    public const string System =
        "You code a site operative's day for a UK residential main contractor. You are given the " +
        "operative's own words about what they did today and the cost codes the site carries. " +
        "Answer with the ONE cost code that the work described belongs to.\n\n" +
        "Rules:\n" +
        "- Answer only with a code from the list given, spelled exactly as given.\n" +
        "- Judge from the work described, not from the trade the operative usually does.\n" +
        "- When the words do not say what trade the work was, or fit several codes equally, " +
        "answer with an empty code rather than guessing.\n\n" +
        "Answer with STRICT JSON only — no markdown fences, no commentary:\n" +
        "{\"costCode\":\"…\"}";

    public static string User(string description, IReadOnlyList<SiteSheetCostCode> candidates)
    {
        var builder = new StringBuilder();
        builder.AppendLine("WHAT WAS DONE TODAY:");
        builder.AppendLine(description.Trim());
        builder.AppendLine();
        builder.AppendLine("COST CODES ON THIS SITE (code | name):");
        foreach (var candidate in candidates) builder.AppendLine($"- {candidate.Code} | {candidate.Name}");
        builder.AppendLine();
        builder.AppendLine("Name the one code the work belongs to, or an empty code. STRICT JSON only.");
        return builder.ToString();
    }

    /// <summary>The code answered, or empty when nothing usable came back. Tolerates fences and
    /// stray prose around the JSON, as every one-shot parser in the api does.</summary>
    public static string ParseCode(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText)) return NoCode;
        var start = responseText.IndexOf('{');
        var end = responseText.LastIndexOf('}');
        if (start < 0 || end <= start) return NoCode;
        try
        {
            using var document = JsonDocument.Parse(responseText[start..(end + 1)]);
            var hasCode = document.RootElement.TryGetProperty("costCode", out var value);
            return hasCode ? value.GetString()?.Trim() ?? NoCode : NoCode;
        }
        catch (JsonException) { return NoCode; }
    }
}
