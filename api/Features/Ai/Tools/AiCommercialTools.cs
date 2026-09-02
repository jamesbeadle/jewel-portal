using System.Text.RegularExpressions;
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The valuation loop's readers (docs/ai/06-context-retrieval.md, Phase 2): one variation in full
/// — header, linked request, the approved lines that stand on the Valuation Report under its
/// V-number with their claimed % — and the live Valuation Report itself, line by line, with the
/// selected claim's % complete and the previous claim's. Together with the two dialogs
/// (variation_edit_lines, claim_progress) they close "update V01 to the V01 tab and correct its
/// % complete": read the tab, read the variation, read the report, fill the dialogs, the user
/// presses Save.
///
/// <para>Every line carries its ValuationLineItemId because that is what the dialogs key on —
/// descriptions repeat, ids do not.</para>
/// </summary>
internal static partial class AiCommercialTools
{
    public const string GetVariationContext = "get_variation_context";
    public const string GetValuationContext = "get_valuation_context";
    public const string GetCostCodeBudgets = "get_cost_code_budgets";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    /// <summary>The same tolerance find_by_reference gives a variation: V72, VO72, VOQ-0072, v 72.</summary>
    private static readonly Regex VariationReference = new("^v(?:oq|o)?0*(\\d+)$", RegexOptions.Compiled);

    public static IReadOnlyList<AiTool> Build() =>
        VariationContextTool()
            .Concat(ValuationContextTool())
            .Concat(CostCodeBudgetsTool())
            .ToList();

    /// <summary>"V01", "v1", "VO 1" and "V001" all mean the same line: normalised to "V1".</summary>
    private static string? NormaliseVariationRef(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = value.Replace("-", "").Replace(" ", "").ToLowerInvariant();
        var match = VariationReference.Match(cleaned);
        return match.Success && int.TryParse(match.Groups[1].Value, out var number)
            ? $"V{number}"
            : value.Trim().ToUpperInvariant();
    }
}
