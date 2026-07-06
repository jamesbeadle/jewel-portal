using System.Text;
using System.Text.Json;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Reads a tender-response email and proposes the quote it contains. The sender is matched to one of
// the package's recipients by their directory email; Claude aligns the email's pricing to the
// package's line items. Nothing is saved — the proposal goes back to the UI for human review, and
// SaveExtractedQuote commits what the user accepts. When no LLM is configured (or the body can't be
// read / the model's answer can't be parsed) the proposal degrades to a manual-entry skeleton: one
// zero-priced row per package line item, Proposed = false.
public sealed class ExtractQuoteFromMessageHandler : ICommandHandler<ExtractQuoteFromMessage, QuoteExtractionProposal>
{
    // Enough for any real tender email; guards the prompt against pathological bodies.
    private const int MaxBodyChars = 40_000;

    private readonly JpmsContext context;
    private readonly IIntakeMessageReader reader;
    private readonly IMailboxGraphClient mailbox;
    private readonly IClaudeClient claude;
    private readonly ILogger<ExtractQuoteFromMessageHandler> logger;

    public ExtractQuoteFromMessageHandler(
        JpmsContext context, IIntakeMessageReader reader, IMailboxGraphClient mailbox,
        IClaudeClient claude, ILogger<ExtractQuoteFromMessageHandler> logger)
    {
        this.context = context; this.reader = reader; this.mailbox = mailbox;
        this.claude = claude; this.logger = logger;
    }

    public async Task<QuoteExtractionProposal> HandleAsync(ExtractQuoteFromMessage command, CancellationToken cancellationToken)
    {
        var lines = await context.BidPackageLineItems
            .Where(line => line.BidPackageId == command.BidPackageId)
            .OrderBy(line => line.SortOrder)
            .ToListAsync(cancellationToken);

        // Who sent it? Match the sender address against the package's invited subcontractors.
        string? subcontractorId = null;
        var snapshot = await mailbox.GetSnapshotAsync(command.MessageId, null, cancellationToken);
        var fromEmail = snapshot?.FromEmail;
        if (!string.IsNullOrWhiteSpace(fromEmail))
        {
            subcontractorId = await (
                from recipient in context.BidPackageRecipients
                where recipient.BidPackageId == command.BidPackageId
                join sub in context.Subcontractors on recipient.SubcontractorId equals sub.SubcontractorId
                where sub.ContactEmail == fromEmail
                select recipient.SubcontractorId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var skeleton = lines
            .Select(line => new QuoteExtractionLine(line.LineItemId, line.Description, line.Unit, line.Quantity, 0m, 0m))
            .ToList();

        if (!claude.IsConfigured)
            return new QuoteExtractionProposal(false, subcontractorId, "", skeleton);

        var content = await reader.GetAsync(command.MessageId, cancellationToken);
        if (content is null || string.IsNullOrWhiteSpace(content.Body))
            return new QuoteExtractionProposal(false, subcontractorId, "", skeleton);

        var body = content.Body.Length > MaxBodyChars ? content.Body[..MaxBodyChars] : content.Body;
        var answer = await claude.CompleteAsync(SystemPrompt, BuildUserPrompt(lines, body), cancellationToken);
        if (answer is null)
            return new QuoteExtractionProposal(false, subcontractorId, "", skeleton);

        var parsed = TryParse(answer, lines.Select(l => l.LineItemId).ToHashSet(StringComparer.OrdinalIgnoreCase));
        if (parsed is null)
        {
            logger.LogWarning("Quote extraction for package {BidPackageId} returned unparseable output.", command.BidPackageId);
            return new QuoteExtractionProposal(false, subcontractorId, "", skeleton);
        }

        return new QuoteExtractionProposal(true, subcontractorId, parsed.Value.Notes, parsed.Value.Lines);
    }

    private const string SystemPrompt =
        "You are a quantity surveyor's assistant extracting a subcontractor's tender submission from an " +
        "email. You are given the bid package's line items (each with an id) and the email body. Return " +
        "ONLY a JSON object, no markdown fences, of the shape: " +
        "{\"notes\": string, \"lines\": [{\"lineItemId\": string|null, \"description\": string, " +
        "\"unit\": string, \"quantity\": number, \"rate\": number, \"total\": number}]}. " +
        "Rules: match each priced item in the email to a package line item id where you can; use null " +
        "lineItemId for priced items outside the package's scope. Use the email's own quantities and " +
        "rates; where only a lump sum is given, set quantity 1, rate = total. Amounts are numbers " +
        "without currency symbols or thousands separators. Put exclusions, caveats and lead times in " +
        "notes. If the email contains no pricing at all, return {\"notes\": \"\", \"lines\": []}.";

    private static string BuildUserPrompt(IReadOnlyList<Data.Entities.BidPackageLineItemEntity> lines, string body)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bid package line items:");
        foreach (var line in lines)
            sb.AppendLine($"- id: {line.LineItemId} | {line.Trade} | {line.Description} | {line.Quantity} {line.Unit}");
        sb.AppendLine();
        sb.AppendLine("Email body:");
        sb.AppendLine(body);
        return sb.ToString();
    }

    private static (string Notes, IReadOnlyList<QuoteExtractionLine> Lines)? TryParse(string answer, HashSet<string> validLineIds)
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

            var lines = new List<QuoteExtractionLine>();
            if (root.TryGetProperty("lines", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    var lineItemId = item.TryGetProperty("lineItemId", out var li) && li.ValueKind == JsonValueKind.String
                        ? li.GetString() : null;
                    // Never trust an id the model invented — an unknown id becomes an unaligned line.
                    if (lineItemId is not null && !validLineIds.Contains(lineItemId)) lineItemId = null;

                    lines.Add(new QuoteExtractionLine(
                        lineItemId,
                        item.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                        item.TryGetProperty("unit", out var u) ? u.GetString() ?? "" : "",
                        ReadDecimal(item, "quantity"),
                        ReadDecimal(item, "rate"),
                        ReadDecimal(item, "total")));
                }
            }

            return (notes, lines);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static decimal ReadDecimal(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out var el)) return 0m;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var value)) return value;
        return 0m;
    }
}
