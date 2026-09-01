using System.Text;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

/// <summary>
/// Works out the trade to search subcontractors for from the bid package's own substance — title,
/// specification summary and line items — so nobody picks it from a dropdown (the package already
/// says what it is, and its stored Trade is often too generic to search on: "Specialist").
///
/// <para>Readiness is decided FIRST and without the AI: a package with no title, or with neither a
/// specification summary nor a single line item, has nothing to tender and nothing to reason from —
/// inviting subcontractors to it is premature whatever the trade, so Ready=false with the reason
/// phrased for the user. The same rule the UI disables the invite buttons on; enforced here too so
/// the gate cannot be skipped by calling the endpoint directly.</para>
///
/// <para>The AI call follows the triage-draft convention: unconfigured, failed or unparsable
/// degrades to the package's own stored trade with a plain note, never a 500. The curated trade
/// list is offered as candidates so the answer lines up with the directory's vocabulary where it
/// can — but a more specific term is allowed, because the term feeds a live web search and
/// "Aluminium windows and glazing" finds companies that "Specialist" never will.</para>
/// </summary>
public sealed class ResolveBidPackageTradeHandler
    : IQueryHandler<ResolveBidPackageTrade, BidPackageTradeResolution>
{
    // A trade term is a few words, not a paragraph. Anything longer than this is the model
    // narrating, and it would both overflow TradeEntity.Name (64) and make a poor search term.
    private const int MaxTradeChars = 64;

    private readonly JpmsContext context;
    private readonly IClaudeClient claude;
    private readonly ILogger<ResolveBidPackageTradeHandler> logger;

    public ResolveBidPackageTradeHandler(
        JpmsContext context, IClaudeClient claude, ILogger<ResolveBidPackageTradeHandler> logger)
    {
        this.context = context; this.claude = claude; this.logger = logger;
    }

    public async Task<BidPackageTradeResolution> HandleAsync(
        ResolveBidPackageTrade query, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.AsNoTracking()
            .FirstOrDefaultAsync(row => row.BidPackageId == query.BidPackageId, cancellationToken);
        if (package is null)
            return new BidPackageTradeResolution(false, Reason: "That bid package could not be found.");

        var lines = await context.BidPackageLineItems.AsNoTracking()
            .Where(row => row.BidPackageId == query.BidPackageId)
            .OrderBy(row => row.SortOrder)
            .Select(row => new { row.Trade, row.Description })
            .ToListAsync(cancellationToken);

        // ---- Readiness: the gate on inviting anybody, not just on the AI call. ----
        var hasTitle = !string.IsNullOrWhiteSpace(package.Title);
        var hasDetails = !string.IsNullOrWhiteSpace(package.SpecificationSummary) || lines.Count > 0;
        if (!hasTitle || !hasDetails)
        {
            var missing = !hasTitle && !hasDetails
                ? "a title and its details (a specification summary or line items)"
                : !hasTitle
                    ? "a title"
                    : "its details — a specification summary or line items";
            return new BidPackageTradeResolution(false, Reason:
                $"This package needs {missing} before subcontractors are invited — the details are "
                + "what the invite, the pricing schedule and the trade match all work from. Add them "
                + "under Details first.");
        }

        // ---- Degrade path: no AI means the package's stored trade, said out loud. ----
        if (!claude.IsConfigured)
        {
            return new BidPackageTradeResolution(true, Fallback(package.Trade), Reason:
                "The AI isn't connected (no Anthropic key is configured), so the package's own "
                + "trade was used.", UsedAi: false);
        }

        var curated = await context.Trades.AsNoTracking()
            .OrderBy(row => row.Name)
            .Select(row => row.Name)
            .ToListAsync(cancellationToken);

        var response = await claude.CompleteAsync(
            SystemPrompt, BuildUserPrompt(package.Title, package.Trade, package.SpecificationSummary,
                lines.Select(line => (line.Trade, line.Description)).ToList(), curated),
            cancellationToken);

        var trade = ParseTrade(response);
        if (string.IsNullOrWhiteSpace(trade))
        {
            logger.LogWarning("Bid package trade resolution failed or could not be parsed for {BidPackageId}.",
                query.BidPackageId);
            return new BidPackageTradeResolution(true, Fallback(package.Trade), Reason:
                "The AI call didn't produce a usable trade, so the package's own trade was used.",
                UsedAi: false);
        }

        return new BidPackageTradeResolution(true, trade);
    }

    /// <summary>The stored trade, or a term generic enough to search on when even that is blank.</summary>
    private static string Fallback(string storedTrade) =>
        string.IsNullOrWhiteSpace(storedTrade) ? "General builder" : storedTrade.Trim();

    // ---- Prompt -----------------------------------------------------------------------------

    private const string SystemPrompt =
        "You are a quantity surveyor's assistant for a UK residential main contractor. You are " +
        "given one bid package — its title, its stored trade, its specification summary and its " +
        "line items — and the contractor's curated trade list. Answer with the single trade or " +
        "speciality that would price and deliver this package, phrased as a search term for " +
        "finding such companies (e.g. \"Aluminium windows and glazing\", \"Drylining\", " +
        "\"Electrician\").\n\n" +
        "Rules:\n" +
        "- Prefer a name from the curated list when one genuinely fits the works.\n" +
        "- When the curated names are all too generic for these works, answer with a concise " +
        "2–4 word speciality instead — specific beats curated for a web search.\n" +
        "- Never answer \"Specialist\", \"General\", \"Contractor\" or anything equally vague " +
        "when the works say what the speciality is.\n" +
        "- Judge from the works described, not from the stored trade — the stored trade may be " +
        "the generic label this call exists to improve on.\n\n" +
        "Answer with STRICT JSON only — no markdown fences, no commentary:\n" +
        "{\"trade\":\"…\"}";

    private static string BuildUserPrompt(
        string title, string storedTrade, string specificationSummary,
        IReadOnlyList<(string Trade, string Description)> lines, IReadOnlyList<string> curated)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"PACKAGE TITLE: {title}");
        builder.AppendLine($"STORED TRADE (may be generic): {storedTrade}");
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(specificationSummary))
        {
            builder.AppendLine("SPECIFICATION SUMMARY:");
            builder.AppendLine(specificationSummary.Trim());
            builder.AppendLine();
        }

        if (lines.Count > 0)
        {
            builder.AppendLine("LINE ITEMS (trade | description):");
            foreach (var line in lines.Take(60))
                builder.AppendLine($"- {line.Trade} | {line.Description}");
            if (lines.Count > 60)
                builder.AppendLine($"(and {lines.Count - 60} more lines of the same works)");
            builder.AppendLine();
        }

        builder.AppendLine(curated.Count == 0
            ? "CURATED TRADE LIST: none yet."
            : $"CURATED TRADE LIST: {string.Join(", ", curated)}");
        builder.AppendLine();
        builder.AppendLine("Name the trade that prices this package. STRICT JSON only.");
        return builder.ToString();
    }

    // ---- Response parsing ---------------------------------------------------------------------

    /// <summary>Null when nothing usable came back. Tolerates markdown fences and stray prose
    /// around the JSON, same as every other one-shot parser in this feature.</summary>
    private static string? ParseTrade(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText)) return null;

        var start = responseText.IndexOf('{');
        var end = responseText.LastIndexOf('}');
        if (start < 0 || end <= start) return null;

        try
        {
            using var document = JsonDocument.Parse(responseText[start..(end + 1)]);
            if (!document.RootElement.TryGetProperty("trade", out var value)
                || value.ValueKind != JsonValueKind.String)
                return null;

            var trade = (value.GetString() ?? "").Trim();
            if (trade.Length == 0 || trade.Length > MaxTradeChars) return null;
            return trade;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
