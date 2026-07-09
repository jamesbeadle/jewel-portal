using System.Text;
using System.Text.Json;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Agents;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Contracts.Variations;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

// Drafts a VOQ from an RFI and the emails tagged to it. RequestContextAssembler gathers the same
// context the request agents see (header + official document + live tagged-email conversation);
// Claude turns it into a proposed title, scope description, estimated value and suggested bid-package
// lines. Nothing is saved — the proposal goes back to the UI for human review, and CreateVoqFromRfq
// commits what the user accepts. When no LLM is configured (or its answer can't be parsed) the
// proposal degrades to a skeleton of the RFI's own title/description, Proposed = false.
public sealed class PrepareVoqDraftHandler : ICommandHandler<PrepareVoqDraft, VoqDraftProposal>
{
    // Generous for any real RFI thread; guards the prompt against pathological conversations.
    private const int MaxConversationChars = 40_000;

    // Storage limits on VariationOrderQuoteEntity.
    private const int MaxTitleChars = 256;
    private const int MaxDescriptionChars = 2048;

    private readonly JpmsContext context;
    private readonly RequestContextAssembler assembler;
    private readonly IClaudeClient claude;
    private readonly ILogger<PrepareVoqDraftHandler> logger;

    public PrepareVoqDraftHandler(
        JpmsContext context, RequestContextAssembler assembler,
        IClaudeClient claude, ILogger<PrepareVoqDraftHandler> logger)
    {
        this.context = context; this.assembler = assembler;
        this.claude = claude; this.logger = logger;
    }

    public async Task<VoqDraftProposal> HandleAsync(PrepareVoqDraft command, CancellationToken cancellationToken)
    {
        var request = await context.Requests.FindAsync(new object[] { command.RequestId }, cancellationToken);
        if (request is null) throw new InvalidOperationException($"Request {command.RequestId} not found.");

        var skeleton = new VoqDraftProposal(false, request.Title, request.Description, null, "", Array.Empty<VoqDraftLine>());

        if (!claude.IsConfigured)
            return skeleton;

        var agentContext = await assembler.AssembleAsync(command.RequestId, cancellationToken);
        if (agentContext is null)
            return skeleton;

        var answer = await claude.CompleteAsync(
            SystemPrompt,
            BuildUserPrompt(agentContext.Header, agentContext.Conversation),
            cancellationToken);
        if (answer is null)
            return skeleton;

        var parsed = TryParse(answer);
        if (parsed is null)
        {
            logger.LogWarning("VOQ draft for request {RequestId} returned unparseable output.", command.RequestId);
            return skeleton;
        }

        // Never trust the model with storage limits or blanks — clamp, and fall back to the RFI's
        // own wording where the model returned nothing usable.
        var title = Clamp(parsed.Title, MaxTitleChars);
        var description = Clamp(parsed.Description, MaxDescriptionChars);
        return parsed with
        {
            Title = string.IsNullOrWhiteSpace(title) ? request.Title : title,
            Description = string.IsNullOrWhiteSpace(description) ? request.Description : description
        };
    }

    private static string Clamp(string value, int max)
    {
        var trimmed = value.Trim();
        return trimmed.Length > max ? trimmed[..max] : trimmed;
    }

    private const string SystemPrompt =
        "You are a quantity surveyor's assistant at a construction main contractor. You are given an " +
        "RFI (request for information) and its full email correspondence. The RFI has been judged to " +
        "carry a variation to the contract, and you are drafting the Variation Order Quote (VOQ) that " +
        "will be tendered to subcontractors. Return ONLY a JSON object, no markdown fences, of the " +
        "shape: {\"title\": string, \"description\": string, \"estimatedValue\": number|null, " +
        "\"trade\": string, \"lines\": [{\"trade\": string, \"description\": string, \"unit\": string, " +
        "\"quantity\": number}]}. Rules: title is a concise variation title (max 200 characters) in " +
        "the style \"Revised Coving and LED Lighting Details\"; description is the scope of works for " +
        "the variation synthesised from the RFI and the correspondence (max 1900 characters, plain " +
        "text, state what changed against the contract and why); estimatedValue is the variation's " +
        "likely value in GBP as a plain number only where pricing is actually quoted or clearly " +
        "implied in the correspondence, otherwise null — never invent a figure; trade is the single " +
        "trade best placed to price the work (e.g. \"Joinery\", \"Electrical\"); lines are the " +
        "measurable scope items a subcontractor would price (unit examples: nr, m, m2, m3, item; use " +
        "quantity 1 with unit \"item\" for lump-sum scope). Use only information present in the " +
        "material provided. If the correspondence contains no scope you can itemise, return an empty " +
        "lines array.";

    private static string BuildUserPrompt(string header, string conversation)
    {
        var thread = conversation.Length > MaxConversationChars
            ? conversation[..MaxConversationChars]
            : conversation;

        var sb = new StringBuilder();
        sb.AppendLine("RFI:");
        sb.AppendLine(header);
        sb.AppendLine();
        sb.AppendLine("Correspondence (in-app messages and emails tagged to this RFI, oldest first):");
        sb.AppendLine(string.IsNullOrWhiteSpace(thread) ? "(none)" : thread);
        return sb.ToString();
    }

    private static VoqDraftProposal? TryParse(string answer)
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

            var title = ReadString(root, "title");
            var description = ReadString(root, "description");
            var trade = ReadString(root, "trade");

            decimal? estimatedValue = null;
            if (root.TryGetProperty("estimatedValue", out var ev) &&
                ev.ValueKind == JsonValueKind.Number && ev.TryGetDecimal(out var value) && value > 0)
                estimatedValue = value;

            var lines = new List<VoqDraftLine>();
            if (root.TryGetProperty("lines", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    var lineDescription = ReadString(item, "description");
                    if (string.IsNullOrWhiteSpace(lineDescription)) continue;

                    var quantity = ReadDecimal(item, "quantity");
                    lines.Add(new VoqDraftLine(
                        ReadString(item, "trade"),
                        lineDescription,
                        ReadString(item, "unit"),
                        quantity > 0 ? quantity : 1m));
                }
            }

            return new VoqDraftProposal(true, title, description, estimatedValue, trade, lines);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() ?? ""
            : "";

    private static decimal ReadDecimal(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var el)) return 0m;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var value)) return value;
        return 0m;
    }
}
