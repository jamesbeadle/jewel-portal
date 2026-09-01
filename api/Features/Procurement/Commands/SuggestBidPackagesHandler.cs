using System.Text;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Feeds the project's LIVE valuation report to Claude and asks for the bid packages worth
/// tendering for what's left. The report comes from
/// <see cref="ValuationReportSnapshotCapture.ComputeAsync"/> — the same maths as the working-copy
/// PDF, so the AI reasons from exactly what the report tab shows: every priced line with its %
/// complete from the latest claim (no claim yet = everything 0%). Existing packages go into the
/// prompt so the AI does not re-propose scope already out to tender.
///
/// Nothing is created here and nothing is saved — the handler returns proposals; the user picks
/// which become Draft packages via the ordinary CreateBidPackage command. Failure shape follows
/// the triage-draft convention: unconfigured/failed AI degrades to an empty list with a plain
/// Note, never a 500.
///
/// The answer is produced in CHUNKS, client-driven like ContinueAiTurn: one request = one
/// bounded Claude call, and an incomplete result carries PartialText for the client to send
/// straight back so the model continues via assistant prefill. One request waiting on the full
/// 3–4k-token answer is exactly what the SWA gateway's ~45s ceiling killed on the slower tiers
/// (Fable worked out 2026-08-16: Haiku finished in time, Fable got the gateway 500).
/// </summary>
public sealed class SuggestBidPackagesHandler
    : ICommandHandler<SuggestBidPackages, BidPackageSuggestionResult>
{
    // Per-HOP output budget. Sized so even the slowest tier finishes a chunk comfortably inside
    // the client's 35s per-call timeout (and the gateway's ~45s) — smaller means more hops, not
    // a worse answer, because each hop continues the same text.
    private const int HopMaxTokens = 700;

    // Backstop against a runaway continuation loop: past this much accumulated text something
    // is wrong (the whole answer should be well under 20k characters).
    private const int MaxAccumulatedChars = 60_000;

    // Bounds the prompt on a pathological report. 400 priced lines is several times the largest
    // real bill; past that the tail is dropped and the prompt says so, because a silent cut would
    // read as "covered everything".
    private const int MaxReportLines = 400;

    private readonly JpmsContext context;
    private readonly IClaudeClient claude;
    private readonly AnthropicOptions options;
    private readonly ILogger<SuggestBidPackagesHandler> logger;

    public SuggestBidPackagesHandler(
        JpmsContext context, IClaudeClient claude, AnthropicOptions options,
        ILogger<SuggestBidPackagesHandler> logger)
    {
        this.context = context; this.claude = claude; this.options = options; this.logger = logger;
    }

    public async Task<BidPackageSuggestionResult> HandleAsync(SuggestBidPackages command, CancellationToken cancellationToken)
    {
        // Same degrade-don't-error shape as the assistant panel: no key, plain answer.
        var tierKey = AiModelCatalogue.Normalise(command.Model);
        var tierName = AiModelCatalogue.Find(tierKey)?.DisplayName ?? tierKey;
        if (!claude.IsConfigured)
            return new BidPackageSuggestionResult(Array.Empty<BidPackageSuggestion>(), tierName,
                "The AI isn't connected (no Anthropic key is configured), so no suggestions could be produced.");

        // The read-only half of snapshot capture: the same figures the report tab and the
        // working-copy PDF show, computed without touching the change tracker.
        var (snapshot, lines) = await ValuationReportSnapshotCapture.ComputeAsync(
            context, command.ProjectId, "Bid package suggestion working copy", null, cancellationToken);

        // Declined/TBC lines are recorded but not priced — they are not works to procure.
        var pricedLines = lines
            .Where(line => line.LineType is not ((int)ValuationLineType.Declined or (int)ValuationLineType.Tbc))
            .Where(line => line.LineAmount != 0m)
            .ToList();

        if (pricedLines.Count == 0)
            return new BidPackageSuggestionResult(Array.Empty<BidPackageSuggestion>(), tierName,
                "The valuation report has no priced lines yet, so there is nothing to base suggestions on.");

        var existingPackages = await context.BidPackages
            .Where(package => package.ProjectId == command.ProjectId)
            .Select(package => new { package.Title, package.Trade, package.Status })
            .ToListAsync(cancellationToken);

        var noClaimYet = snapshot.ValuationClaimId is null;
        var userPrompt = BuildUserPrompt(pricedLines, existingPackages
            .Select(p => (p.Title, p.Trade, (BidPackageStatus)p.Status)).ToList(), noClaimYet);

        // One bounded chunk per request; PartialText is what earlier hops produced. The prompt
        // is rebuilt from live data each hop — identical unless the report changed mid-run,
        // which is the same staleness any live query has.
        var chunk = await claude.CompleteChunkAsync(
            SystemPrompt, userPrompt, command.PartialText,
            options.ModelForTier(tierKey), HopMaxTokens, cancellationToken);

        if (chunk is null)
            return new BidPackageSuggestionResult(Array.Empty<BidPackageSuggestion>(), tierName,
                "The AI call failed — nothing was produced. Try again, or pick a different model.");

        var responseText = command.PartialText.TrimEnd() + chunk.Text;

        if (!chunk.IsComplete)
        {
            if (responseText.Length > MaxAccumulatedChars)
                return new BidPackageSuggestionResult(Array.Empty<BidPackageSuggestion>(), tierName,
                    "The AI's answer ran far too long to be a real proposal list. Try again, or pick a different model.");
            // Not done yet — hand the accumulated text back for the client to continue with.
            return new BidPackageSuggestionResult(Array.Empty<BidPackageSuggestion>(), tierName,
                null, IsComplete: false, PartialText: responseText);
        }

        var suggestions = ParseSuggestions(responseText);
        if (suggestions is null)
        {
            logger.LogWarning("Bid package suggestion response could not be parsed as JSON.");
            return new BidPackageSuggestionResult(Array.Empty<BidPackageSuggestion>(), tierName,
                "The AI's answer couldn't be read. Try again, or pick a stronger model.");
        }

        string? note = null;
        if (noClaimYet)
            note = "No valuation claim exists yet, so every line was treated as 0% complete — the suggestions cover the whole bill.";
        else if (pricedLines.Count > MaxReportLines)
            note = $"The report has {pricedLines.Count} priced lines; only the largest {MaxReportLines} by remaining value were analysed.";

        return new BidPackageSuggestionResult(suggestions, tierName, note);
    }

    // ---- Prompt -----------------------------------------------------------------------------

    private const string SystemPrompt =
        "You are a quantity surveyor's assistant for a UK residential main contractor. " +
        "You are given the project's current valuation report: every priced line with its value, " +
        "cumulative % complete and remaining (unclaimed) value, plus the bid packages already " +
        "raised. Your job is to propose the bid packages the contractor should put out to tender " +
        "for the REMAINING works.\n\n" +
        "Rules:\n" +
        "- Only propose packages for work with meaningful remaining value. Work at or near 100% complete needs no tender.\n" +
        "- Group by trade/speciality: each package must be work ONE trade can price and deliver as a single phase, " +
        "without depending on another proposed package finishing first. Split a trade into two packages when its works " +
        "clearly fall in different phases (e.g. first-fix vs second-fix at very different completion levels).\n" +
        "- Do NOT propose scope that an existing package already covers (any status except Closed). Closed packages ended " +
        "without an award, so their scope IS fair game — say so in the rationale if you re-propose it.\n" +
        "- Skip preliminaries, contingency allowances, overheads and other non-buildable lines — nobody tenders those.\n" +
        "- As many packages as the remaining works genuinely justify — no fixed limit — but consolidate small " +
        "related scopes into one sensible package: a package per report line is fragmentation, not procurement. " +
        "One trade gets one package unless its works clearly split into separate phases.\n" +
        "- Titles are short and site-friendly (e.g. \"Second-fix carpentry\"); trade is the speciality that prices it " +
        "(e.g. \"Carpenter\").\n" +
        "- scope: 2–4 short lines, newline-separated, stating what the package covers — written to be pasted into a " +
        "tender's \"what this package covers\" summary.\n" +
        "- rationale: ONE short sentence.\n" +
        "- materials_applicable: true when the trade would normally be asked whether they supply their own materials " +
        "(supply-and-fit trades); false for labour-only or client-supplied-materials scopes.\n" +
        "- approx_value: the sum of the remaining values of the report lines behind the package, as a plain number.\n" +
        "- source_lines: the descriptions of the report lines the package draws on, verbatim.\n\n" +
        "Answer with STRICT JSON only — no markdown fences, no commentary before or after:\n" +
        "{\"suggestions\":[{\"title\":\"…\",\"trade\":\"…\",\"scope\":\"…\",\"approx_value\":0,\"materials_applicable\":false," +
        "\"rationale\":\"…\",\"source_lines\":[\"…\"]}]}\n" +
        "Order suggestions by remaining value, largest first. If nothing is worth tendering, return {\"suggestions\":[]}.";

    private static string BuildUserPrompt(
        List<ValuationReportSnapshotLineEntity> pricedLines,
        List<(string Title, string Trade, BidPackageStatus Status)> existingPackages,
        bool noClaimYet)
    {
        var builder = new StringBuilder();
        builder.AppendLine("VALUATION REPORT — current position, one line per priced item.");
        builder.AppendLine("Columns: element | section | description | line value £ | % complete | remaining £");
        if (noClaimYet)
            builder.AppendLine("(No claim has been recorded yet: every line is 0% complete.)");
        builder.AppendLine();

        // Largest remaining first, and the cap trims the tail that matters least.
        foreach (var line in pricedLines
            .OrderByDescending(line => line.LineAmount - line.CumulativeClaimed)
            .Take(MaxReportLines))
        {
            var element = (ValuationElementType)line.ElementType switch
            {
                ValuationElementType.Variation => $"Variation {line.VariationRef}".Trim(),
                ValuationElementType.PcSum => "PC sum",
                ValuationElementType.Contingency => "Contingency",
                _ => "Contract works"
            };
            var section = (ValuationElementType)line.ElementType == ValuationElementType.Variation
                ? line.VariationTitle
                : string.IsNullOrWhiteSpace(line.SectionName) ? line.SectionCode : line.SectionName;
            var remaining = line.LineAmount - line.CumulativeClaimed;
            builder.AppendLine(
                $"{element} | {section} | {line.Description} | {line.LineAmount:0} | {line.PercentComplete:0.#}% | {remaining:0}");
        }

        builder.AppendLine();
        if (existingPackages.Count == 0)
        {
            builder.AppendLine("EXISTING BID PACKAGES: none.");
        }
        else
        {
            builder.AppendLine("EXISTING BID PACKAGES on this project (do not duplicate their scope; Closed ones ended without an award):");
            foreach (var package in existingPackages)
                builder.AppendLine($"- {package.Title} (trade: {package.Trade}; status: {package.Status})");
        }

        builder.AppendLine();
        builder.AppendLine("Propose the bid packages for the remaining works, per the rules. STRICT JSON only.");
        return builder.ToString();
    }

    // ---- Response parsing -------------------------------------------------------------------

    /// <summary>Null only when no JSON object can be read at all; individual malformed entries
    /// are skipped rather than sinking the batch. Tolerates a model that wrapped the JSON in
    /// markdown fences or prose despite the instruction.</summary>
    private static IReadOnlyList<BidPackageSuggestion>? ParseSuggestions(string responseText)
    {
        var start = responseText.IndexOf('{');
        var end = responseText.LastIndexOf('}');
        if (start < 0 || end <= start) return null;

        try
        {
            using var document = JsonDocument.Parse(responseText[start..(end + 1)]);
            if (!document.RootElement.TryGetProperty("suggestions", out var array)
                || array.ValueKind != JsonValueKind.Array)
                return null;

            var suggestions = new List<BidPackageSuggestion>();
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                var title = ReadString(item, "title");
                var trade = ReadString(item, "trade");
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(trade)) continue;

                var sourceLines = new List<string>();
                if (item.TryGetProperty("source_lines", out var sources) && sources.ValueKind == JsonValueKind.Array)
                    foreach (var source in sources.EnumerateArray())
                        if (source.ValueKind == JsonValueKind.String && source.GetString() is { Length: > 0 } text)
                            sourceLines.Add(text);

                suggestions.Add(new BidPackageSuggestion(
                    title.Trim(),
                    trade.Trim(),
                    ReadString(item, "scope").Trim(),
                    ReadDecimal(item, "approx_value"),
                    item.TryGetProperty("materials_applicable", out var materials)
                        && materials.ValueKind == JsonValueKind.True,
                    ReadString(item, "rationale").Trim(),
                    sourceLines));
            }
            return suggestions;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? "" : "";

    private static decimal ReadDecimal(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)) return 0m;
        // The model was asked for a plain number, but tolerate "12,400" / "£12400" strings too.
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number;
        if (value.ValueKind == JsonValueKind.String)
        {
            var text = new string((value.GetString() ?? "").Where(c => char.IsDigit(c) || c is '.' or '-').ToArray());
            if (decimal.TryParse(text, out var parsed)) return parsed;
        }
        return 0m;
    }
}
