using System.Text;
using System.Text.Json;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Drafts a bid package with Claude from what's tagged to it: the related emails (bodies read live
// from the mailbox), the linked drawings' register entries, and the package's own title and trade.
// The cost-centre master list is included in the prompt so proposed lines arrive with a suggested
// cost code — codes the model invents are blanked, never trusted. Nothing is saved here: the
// proposal goes back to the UI's review screen, and only the lines the user accepts are committed
// (via AddBidPackageLineItems, which is append-only). When no LLM is configured, or the model
// returns nothing parseable, Proposed = false and the UI explains rather than erroring.
public sealed class GenerateBidPackageDraftHandler : ICommandHandler<GenerateBidPackageDraft, BidPackageDraftProposal>
{
    // Context caps: enough for real tender correspondence; guards the prompt against pathological
    // bodies and inbox-sized tag sets.
    private const int MaxEmails = 6;
    private const int MaxCharsPerEmail = 8_000;

    private static readonly BidPackageDraftProposal NotProposed =
        new(false, "", Array.Empty<BidPackageDraftLine>());

    private readonly JpmsContext context;
    private readonly RecordEmailReader emails;
    private readonly IIntakeMessageReader reader;
    private readonly IClaudeClient claude;
    private readonly ILogger<GenerateBidPackageDraftHandler> logger;

    public GenerateBidPackageDraftHandler(
        JpmsContext context, RecordEmailReader emails, IIntakeMessageReader reader,
        IClaudeClient claude, ILogger<GenerateBidPackageDraftHandler> logger)
    {
        this.context = context; this.emails = emails; this.reader = reader;
        this.claude = claude; this.logger = logger;
    }

    public async Task<BidPackageDraftProposal> HandleAsync(GenerateBidPackageDraft command, CancellationToken cancellationToken)
    {
        if (!claude.IsConfigured)
            return NotProposed;

        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null)
            return NotProposed;

        var existingLines = await context.BidPackageLineItems
            .Where(line => line.BidPackageId == command.BidPackageId)
            .OrderBy(line => line.SortOrder)
            .ToListAsync(cancellationToken);

        var drawings = await (
            from link in context.BidPackageDrawings
            where link.BidPackageId == command.BidPackageId
            join drawing in context.Drawings on link.DrawingId equals drawing.DrawingId
            select new { drawing.DrawingCode, drawing.Title, drawing.CurrentApprovedRevisionLabel })
            .ToListAsync(cancellationToken);

        // The master list Claude may pick cost codes from; anything outside it is blanked on the
        // way back so an invented code can never reach a line item.
        var costCenters = await context.CostCenters
            .Where(centre => centre.IsActive)
            .OrderBy(centre => centre.SortOrder)
            .Select(centre => new { centre.Code, centre.Name })
            .ToListAsync(cancellationToken);
        var validCostCodes = costCenters.Select(centre => centre.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // The package's tagged emails, most recent first, bodies read live from the mailbox. A
        // package with no tagged emails still drafts — Claude just works from title, trade and
        // drawings, and the UI has already warned the draft may be thin.
        var tagged = await emails.ForRecordAsync(RecordType.BidPackageInvite, command.BidPackageId, cancellationToken);
        var bodies = new List<(string Subject, string From, DateTimeOffset ReceivedAt, string Body)>();
        foreach (var message in tagged.OrderByDescending(m => m.ReceivedAt).Take(MaxEmails))
        {
            var content = await reader.GetAsync(message.Id, cancellationToken);
            var body = content?.Body ?? message.BodyPreview;
            if (string.IsNullOrWhiteSpace(body)) continue;
            if (body.Length > MaxCharsPerEmail) body = body[..MaxCharsPerEmail];
            bodies.Add((message.Subject, message.FromEmail, message.ReceivedAt, body));
        }

        var userPrompt = BuildUserPrompt(package.Title, package.Trade, existingLines, drawings
            .Select(d => (d.DrawingCode, d.Title, d.CurrentApprovedRevisionLabel)).ToList(),
            costCenters.Select(c => (c.Code, c.Name)).ToList(), bodies);

        var answer = await claude.CompleteAsync(SystemPrompt, userPrompt, cancellationToken);
        if (answer is null)
            return NotProposed;

        var parsed = TryParse(answer, validCostCodes, package.Trade);
        if (parsed is null)
        {
            logger.LogWarning("Bid package draft for {BidPackageId} returned unparseable output.", command.BidPackageId);
            return NotProposed;
        }

        return parsed;
    }

    private const string SystemPrompt =
        "You are a quantity surveyor's assistant drafting a bid package (request for tender) for a UK " +
        "residential construction firm. You are given the package's title and trade, its existing line " +
        "items, the drawings linked to it, the cost-centre master list, and the emails tagged to the " +
        "package. Return ONLY a JSON object, no markdown fences, of the shape: " +
        "{\"notes\": string, \"lines\": [{\"trade\": string, \"description\": string, \"unit\": string, " +
        "\"quantity\": number, \"costCode\": string}]}. " +
        "Rules: propose at most 12 measurable line items for the work the sources describe; keep each " +
        "description under 15 words; use standard units (m, m2, m3, nr, item, sum); where a quantity " +
        "is not stated in the sources use 1 with unit \"item\" or \"sum\" — never invent measurements; " +
        "costCode must be a Code from the given cost-centre list that fits the line, or \"\" when none " +
        "fits; do not repeat the package's existing line items; notes is a scope summary under 80 words " +
        "stating what the package covers and every assumption you made. If the sources give too little " +
        "to draft from, return fewer lines rather than inventing work.";

    private static string BuildUserPrompt(
        string title, string trade,
        IReadOnlyList<Data.Entities.BidPackageLineItemEntity> existingLines,
        IReadOnlyList<(string Code, string Title, string? Revision)> drawings,
        IReadOnlyList<(string Code, string Name)> costCenters,
        IReadOnlyList<(string Subject, string From, DateTimeOffset ReceivedAt, string Body)> emailBodies)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Bid package: {title}");
        sb.AppendLine($"Trade: {trade}");
        sb.AppendLine();

        sb.AppendLine(existingLines.Count == 0
            ? "Existing line items: none."
            : "Existing line items (do not repeat these):");
        foreach (var line in existingLines)
            sb.AppendLine($"- {line.Trade} | {line.Description} | {line.Quantity} {line.Unit}");
        sb.AppendLine();

        sb.AppendLine(drawings.Count == 0 ? "Linked drawings: none." : "Linked drawings:");
        foreach (var drawing in drawings)
            sb.AppendLine($"- {drawing.Code} | {drawing.Title}{(string.IsNullOrWhiteSpace(drawing.Revision) ? "" : $" | rev {drawing.Revision}")}");
        sb.AppendLine();

        sb.AppendLine("Cost centres (use these codes):");
        foreach (var centre in costCenters)
            sb.AppendLine($"- {centre.Code} | {centre.Name}");
        sb.AppendLine();

        if (emailBodies.Count == 0)
        {
            sb.AppendLine("Related emails: none.");
        }
        else
        {
            sb.AppendLine("Related emails (most recent first):");
            foreach (var email in emailBodies)
            {
                sb.AppendLine($"--- From {email.From} on {email.ReceivedAt:yyyy-MM-dd} | Subject: {email.Subject}");
                sb.AppendLine(email.Body);
            }
        }

        return sb.ToString();
    }

    private static BidPackageDraftProposal? TryParse(string answer, HashSet<string> validCostCodes, string packageTrade)
    {
        try
        {
            // Models occasionally fence the JSON despite instructions; strip any fences defensively.
            var json = answer.Trim();
            if (json.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewline = json.IndexOf('\n');
                var lastFence = json.LastIndexOf("```", StringComparison.Ordinal);
                if (firstNewline >= 0 && lastFence > firstNewline)
                    json = json[(firstNewline + 1)..lastFence].Trim();
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var notes = root.TryGetProperty("notes", out var n) ? n.GetString() ?? "" : "";

            var lines = new List<BidPackageDraftLine>();
            if (root.TryGetProperty("lines", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    var description = ReadString(item, "description");
                    if (string.IsNullOrWhiteSpace(description)) continue;

                    // Never trust a code the model invented — an unknown code becomes "pick one".
                    var costCode = ReadString(item, "costCode");
                    if (!validCostCodes.Contains(costCode)) costCode = "";

                    var trade = ReadString(item, "trade");
                    if (string.IsNullOrWhiteSpace(trade)) trade = packageTrade;

                    var quantity = ReadDecimal(item, "quantity");
                    if (quantity <= 0) quantity = 1;

                    lines.Add(new BidPackageDraftLine(trade, description, ReadString(item, "unit"), quantity, costCode));
                }
            }

            return new BidPackageDraftProposal(true, notes, lines);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ReadString(JsonElement item, string property) =>
        item.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() ?? ""
            : "";

    private static decimal ReadDecimal(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out var el)) return 0m;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var value)) return value;
        return 0m;
    }
}
