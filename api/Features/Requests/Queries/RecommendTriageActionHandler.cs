using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

/// <summary>
/// Asks Claude to read a triage email's whole thread and recommend the next triage action. Follows
/// the same shape as the bid-quote extractor: JSON-only response, defensive fence-stripping, and no
/// trust in anything the model invents (the action key and project id are validated against the real
/// lists). Advisory only — returns Unavailable (and the UI hides the box) when the AI feature is
/// unconfigured or the call fails, so triage itself never depends on it.
/// </summary>
public sealed class RecommendTriageActionHandler : IQueryHandler<RecommendTriageAction, TriageRecommendation>
{
    // The action keys the model may answer with, mirroring what the triage UI can actually do.
    private static readonly HashSet<string> ActionKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "link_to_existing", "create_request", "create_bid_package",
        "tag_scheduling", "create_todos", "discard", "none"
    };

    // Bounds on what one recommendation reads: enough thread to judge, small enough to stay cheap.
    private const int MaxThreadMessages = 12;
    private const int MaxBodyChars = 6000;

    private readonly IMailboxGraphClient graph;
    private readonly IIntakeMessageReader reader;
    private readonly IClaudeClient claude;
    private readonly JpmsContext context;

    public RecommendTriageActionHandler(
        IMailboxGraphClient graph, IIntakeMessageReader reader, IClaudeClient claude, JpmsContext context)
    {
        this.graph = graph;
        this.reader = reader;
        this.claude = claude;
        this.context = context;
    }

    public async Task<TriageRecommendation> HandleAsync(RecommendTriageAction query, CancellationToken cancellationToken)
    {
        if (!claude.IsConfigured || string.IsNullOrWhiteSpace(query.MessageId))
            return TriageRecommendation.Unavailable();

        // The thread, oldest first — or just the clicked message when Graph gave no conversation id.
        var thread = await LoadThreadAsync(query, cancellationToken);

        var projects = await context.Projects
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new { p.ProjectId, p.Reference, p.Name, p.ClientName })
            .ToListAsync(cancellationToken);
        var validProjectIds = projects.Select(p => p.ProjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var userPrompt = await BuildUserPromptAsync(query, thread, projects.Select(p =>
            $"- id: {p.ProjectId} | {p.Reference} | {p.Name} | client: {p.ClientName}"), cancellationToken);

        var answer = await claude.CompleteAsync(SystemPrompt, userPrompt, cancellationToken);
        if (string.IsNullOrWhiteSpace(answer))
            return TriageRecommendation.Unavailable();

        return TryParse(answer, validProjectIds) ?? TriageRecommendation.Unavailable();
    }

    private async Task<IReadOnlyList<MailboxMessage>> LoadThreadAsync(RecommendTriageAction query, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(query.ConversationId))
        {
            try
            {
                var page = await graph.ListConversationAsync(query.ConversationId, ct);
                if (page.Items.Count > 0)
                    return page.Items;
            }
            catch { /* fall through to the single-message fallback */ }
        }

        // No thread available: describe just the clicked message from what the query carried.
        return new[]
        {
            new MailboxMessage(
                query.MessageId, query.InternetMessageId ?? "", query.FromEmail ?? "", query.FromName ?? "",
                query.Subject ?? "", "", false, DateTimeOffset.UtcNow, Array.Empty<string>())
        };
    }

    private async Task<string> BuildUserPromptAsync(
        RecommendTriageAction query, IReadOnlyList<MailboxMessage> thread,
        IEnumerable<string> projectLines, CancellationToken ct)
    {
        var sb = new StringBuilder();

        sb.AppendLine("PROJECT CANDIDATES (id | reference | name | client):");
        var any = false;
        foreach (var line in projectLines) { sb.AppendLine(line); any = true; }
        if (!any) sb.AppendLine("(none)");
        sb.AppendLine();

        // Keep the newest messages when the thread is longer than the cap, but keep oldest-first order.
        var members = thread.Count <= MaxThreadMessages
            ? thread
            : thread.Skip(thread.Count - MaxThreadMessages).ToList();

        sb.AppendLine($"EMAIL THREAD (oldest first, {members.Count} of {thread.Count} messages, the one being triaged is marked ⟵ CURRENT):");
        var index = 0;
        foreach (var member in members)
        {
            index++;
            var current = member.Id == query.MessageId ? " ⟵ CURRENT" : "";
            sb.AppendLine($"--- message {index}{current} ---");
            sb.AppendLine($"From: {member.FromName} <{member.FromEmail}> | Sent: {member.ReceivedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Subject: {member.Subject}");
            if (member.Categories.Count > 0)
                sb.AppendLine($"Already tagged to: {string.Join(", ", member.Categories)}");

            var content = await ReadBodyAsync(member, ct);
            if (content.Attachments.Count > 0)
                sb.AppendLine($"Attachments (names only): {string.Join(", ", content.Attachments)}");
            sb.AppendLine("Body:");
            sb.AppendLine(content.Body);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private sealed record MemberContent(string Body, IReadOnlyList<string> Attachments);

    // Full body read live per member, flattened to plain text and truncated. A member that can't be
    // read degrades to its stored preview rather than failing the whole recommendation.
    private async Task<MemberContent> ReadBodyAsync(MailboxMessage member, CancellationToken ct)
    {
        try
        {
            var content = await reader.GetAsync(member.Id, ct);
            if (content is not null)
            {
                var text = content.IsHtml ? HtmlToText(content.Body) : content.Body;
                return new MemberContent(Truncate(text), content.Attachments.Select(a => a.Name).ToList());
            }
        }
        catch { /* degrade to the preview */ }
        return new MemberContent(Truncate(member.BodyPreview), Array.Empty<string>());
    }

    private static string Truncate(string text)
    {
        text = text.Trim();
        return text.Length <= MaxBodyChars ? text : text[..MaxBodyChars] + "\n[truncated]";
    }

    // Crude but sufficient HTML flattening for prompting: drop style/script, keep table cell and
    // line breaks as whitespace, strip the rest of the tags, decode entities, collapse blank runs.
    private static string HtmlToText(string html)
    {
        var text = Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</(p|div|tr|li|h[1-6])>|<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</t[dh]>", " | ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<[^>]+>", " ");
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"[ \t]{2,}", " ");
        return Regex.Replace(text, @"(\s*\n\s*){2,}", "\n");
    }

    private const string SystemPrompt =
        "You are a triage assistant for JPMS, the project-management system of Jewel Bespoke Build, a " +
        "super-prime residential construction company in Surrey, UK. Inbound email to the projects " +
        "mailbox sits in a triage queue until a member of staff assigns it to a record. Read one email " +
        "thread and recommend the single best next action, with a short summary the triager can act on " +
        "in seconds.\n\n" +
        "ACTIONS (recommend exactly one as recommendedAction):\n" +
        "- link_to_existing: the thread continues something already tracked (a reference like REQ-0012, " +
        "RFI-004 or TODO-0031 in the subject or an 'Already tagged to' line elsewhere in the thread is " +
        "the strongest signal). Name the reference in the reasoning.\n" +
        "- create_request: promote the email to a new request (question blocking work, approval sought, " +
        "notice of delay, or anything needing a tracked answer). New requests always start as General " +
        "containers and are promoted to RFI/RFQ later, so do not pick a request subtype.\n" +
        "- create_bid_package: subcontractor tender/quote correspondence for procurement.\n" +
        "- tag_scheduling: programme/logistics content (site attendance, sequencing, delivery dates).\n" +
        "- create_todos: the email is a punch list of small actionable items; propose the items, one " +
        "short imperative title each.\n" +
        "- discard: no action needed (auto-replies, spam, FYI-only, courtesy replies).\n" +
        "- none: genuinely ambiguous; say what a human should check first.\n\n" +
        "DOMAIN RULES:\n" +
        "- 'Valuation invoice' is the only term for money Jewel claims from the client (never 'cash " +
        "call'). Supplier/subcontractor invoices TO Jewel are different — those usually belong with " +
        "procurement or an existing record.\n" +
        "- An email can carry several signals (e.g. a payment chaser that also lists snagging items). " +
        "Pick the primary action; list the rest in secondaryActions.\n" +
        "- Delay language is time-sensitive under JCT notice clauses — set urgency high and surface " +
        "any dates in keyDates.\n" +
        "- Prefer link_to_existing over creating a new record when the thread clearly continues an " +
        "existing conversation.\n" +
        "- Only use a projectId from the PROJECT CANDIDATES list; if unsure, use null.\n\n" +
        "DRAFTING THE RECORD (when recommending any create_* action): write suggestedTitle and " +
        "suggestedDescription in your own words as a formal record, never by copying the email. The " +
        "title is a concise register entry (project context + the matter, e.g. 'Front block paving " +
        "scope dispute — 50% settlement proposal'). The description is 2-5 sentences of neutral " +
        "record-keeping prose stating what is being asked or decided, the key figures, references " +
        "(drawings, VOs, tender documents) and dates from the thread, and what response is needed " +
        "from whom — it must make sense to someone who has not read the email. suggestedRaisedTo is " +
        "the name or email of the party the request is directed at (the ball-in-court person in the " +
        "thread, usually the counterparty, never the Jewel triager). suggestedTrade applies only to " +
        "create_bid_package (e.g. 'Windows', 'Groundworks').\n\n" +
        "OUTPUT — return ONLY a JSON object, no markdown fences:\n" +
        "{\"summary\": string (2-3 sentences: who wants what and why it matters), " +
        "\"recommendedAction\": string (one action key), " +
        "\"projectId\": string|null, " +
        "\"suggestedTitle\": string|null (a concise record title, if creating), " +
        "\"suggestedDescription\": string|null (the drafted record detail, if creating), " +
        "\"suggestedRaisedTo\": string|null, " +
        "\"suggestedTrade\": string|null (only for create_bid_package), " +
        "\"suggestedResponseDue\": string|null (YYYY-MM-DD, only when the thread states or clearly " +
        "implies a response deadline — never invent one), " +
        "\"todoItems\": [string] (only for create_todos, else []), " +
        "\"secondaryActions\": [string] (other action keys, may be []), " +
        "\"urgency\": \"low\"|\"normal\"|\"high\" (high = money, contractual notice, or blocked work), " +
        "\"confidence\": \"low\"|\"medium\"|\"high\", " +
        "\"keyDates\": [{\"date\": \"YYYY-MM-DD\", \"meaning\": string}], " +
        "\"reasoning\": string (one short paragraph for the triager)}";

    private static TriageRecommendation? TryParse(string answer, HashSet<string> validProjectIds)
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

            var action = GetString(root, "recommendedAction") ?? "none";
            if (!ActionKeys.Contains(action)) action = "none";

            // Never trust a project id the model invented.
            var projectId = GetString(root, "projectId");
            if (projectId is not null && !validProjectIds.Contains(projectId)) projectId = null;

            var urgency = GetString(root, "urgency")?.ToLowerInvariant();
            if (urgency is not ("low" or "normal" or "high")) urgency = "normal";
            var confidence = GetString(root, "confidence")?.ToLowerInvariant();
            if (confidence is not ("low" or "medium" or "high")) confidence = "low";

            var keyDates = new List<TriageRecommendationDate>();
            if (root.TryGetProperty("keyDates", out var dates) && dates.ValueKind == JsonValueKind.Array)
                foreach (var item in dates.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        var date = GetString(item, "date");
                        var meaning = GetString(item, "meaning");
                        if (!string.IsNullOrWhiteSpace(date))
                            keyDates.Add(new TriageRecommendationDate(date, meaning ?? ""));
                    }

            return new TriageRecommendation(
                Available: true,
                Summary: GetString(root, "summary") ?? "",
                RecommendedAction: action.ToLowerInvariant(),
                ProjectId: projectId,
                SuggestedTitle: GetString(root, "suggestedTitle"),
                SuggestedDescription: GetString(root, "suggestedDescription"),
                SuggestedRaisedTo: GetString(root, "suggestedRaisedTo"),
                SuggestedTrade: GetString(root, "suggestedTrade"),
                SuggestedResponseDue: GetString(root, "suggestedResponseDue"),
                TodoItems: GetStrings(root, "todoItems"),
                SecondaryActions: GetStrings(root, "secondaryActions")
                    .Where(a => ActionKeys.Contains(a)).Select(a => a.ToLowerInvariant()).ToList(),
                Urgency: urgency,
                Confidence: confidence,
                KeyDates: keyDates,
                Reasoning: GetString(root, "reasoning") ?? "");
        }
        catch
        {
            return null;
        }
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static IReadOnlyList<string> GetStrings(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();
        return array.EnumerateArray()
            .Where(v => v.ValueKind == JsonValueKind.String)
            .Select(v => v.GetString()!)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }
}
