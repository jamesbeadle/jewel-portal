using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Ganss.Xss;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

/// <summary>
/// Reads one triaged intake email and asks Claude to propose a draft request to pre-fill the
/// "Create new request" form: which project it belongs to (inferred from the live project list),
/// the request type, a register-style title and detail, plus the register fields (ball-in-court,
/// drawing/detail ref, response-due date, related drawing/spec). The result is advisory only —
/// nothing is created until the triager submits CreateRequestFromIntake.
///
/// Degrades gracefully at every step: if Claude is unconfigured, the email is missing, or the model
/// returns anything we can't parse, we hand back RequestSuggestion.Unavailable carrying the plain
/// subject/body so the form still pre-fills exactly as it does today.
/// </summary>
public sealed class SuggestRequestFromIntakeHandler : IQueryHandler<SuggestRequestFromIntake, RequestSuggestion>
{
    private readonly JpmsContext context;
    private readonly IIntakeMessageReader reader;
    private readonly IClaudeClient claude;
    private readonly ILogger<SuggestRequestFromIntakeHandler> logger;

    public SuggestRequestFromIntakeHandler(
        JpmsContext context,
        IIntakeMessageReader reader,
        IClaudeClient claude,
        ILogger<SuggestRequestFromIntakeHandler> logger)
    {
        this.context = context;
        this.reader = reader;
        this.claude = claude;
        this.logger = logger;
    }

    public async Task<RequestSuggestion> HandleAsync(SuggestRequestFromIntake query, CancellationToken cancellationToken)
    {
        var entity = await context.IntakeEmails
            .FirstOrDefaultAsync(e => e.IntakeId == query.IntakeId, cancellationToken);

        if (entity is null)
            return RequestSuggestion.Unavailable("", "");

        // Plain-subject/body fallback used whenever AI is off or anything below fails.
        var fallback = RequestSuggestion.Unavailable(entity.Subject, entity.BodyPreview);

        if (!claude.IsConfigured)
            return fallback;

        // Prefer the full live body; fall back to the stored preview if Graph can't supply it.
        var emailBody = await ResolveBodyTextAsync(entity, cancellationToken);

        var projects = await context.Projects
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectChoice(p.ProjectId, p.Reference, p.Name, p.ClientName))
            .ToListAsync(cancellationToken);

        var knownProjectIds = projects.Select(p => p.ProjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(entity, emailBody, projects);

        var raw = await claude.CompleteAsync(systemPrompt, userPrompt, cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        var suggestion = Parse(raw, knownProjectIds, fallback);
        return suggestion;
    }

    private async Task<string> ResolveBodyTextAsync(Data.Entities.IntakeEmailEntity entity, CancellationToken ct)
    {
        if (entity.GraphMessageId is null)
            return entity.BodyPreview;

        var content = await reader.GetAsync(entity.GraphMessageId, ct);
        if (content is null)
            return entity.BodyPreview;

        var text = content.IsHtml ? HtmlToText(content.Body) : content.Body;
        text = text.Trim();
        return string.IsNullOrEmpty(text) ? entity.BodyPreview : Truncate(text, 12000);
    }

    // The business context that teaches Claude how Jewel Bespoke Build turns site/design correspondence
    // into a tracked request. Kept here (not in source-controlled config) so it can be tuned freely.
    private static string BuildSystemPrompt() =>
        """
        You are a construction project administrator at Jewel Bespoke Build, a super-prime residential
        contractor in Surrey. You triage emails arriving at the projects@ mailbox and turn each one into
        a single tracked "request" in our project management system (JPMS).

        A request is the unit of work that captures something the project needs resolving — most often a
        Request for Information (RFI) raised to the architect, but also requests for quotation, approval,
        change, proposals, and JCT notices. One request may eventually gather several emails, but right
        now you are reading ONE email and proposing how to open the request it implies.

        Decide the following from the email and the list of live projects you are given:

        - project: which project the email concerns. Match on project name, client name, site/address,
          reference, or people involved. If you cannot confidently match exactly one project, leave it
          null — never guess.
        - type: the request type. Use one of: RFI, RFA, RFC, RFQ, RFP, NOD, EOT.
            RFI = a question seeking information/clarification (the common case).
            RFA = seeking approval of a sample, submittal, or material.
            RFC = a change to design/scope, or a comment requiring action.
            RFQ = asking a price/quotation. RFP = asking for a proposal.
            NOD = notice of delay. EOT = request for an extension of time.
          If unsure, default to RFI.
        - title: a short register-style subject line for the request, like an entry in an RFI register.
          Concise, specific, no email cruft ("RE:", "FW:"), no quotes. e.g. "Steelwork connection detail
          at grid B/3" or "Confirmation of selected sanitaryware".
        - detail: a clear, self-contained statement of what is being asked and why, written for our
          register — a few sentences in plain professional English. Capture the actual question/ask and
          any specifics (locations, drawing/detail references, dates). Do not copy the email verbatim or
          include signatures, disclaimers, or quoted reply chains.
        - raisedTo: the ball-in-court party the request is directed to (usually the architect, e.g.
          "PLG Architects"). Infer from the email if clear, otherwise null.
        - drawingRef: any drawing or detail reference the request concerns (e.g. "A-201", "Detail 14"),
          otherwise null.
        - responseDue: a contractual response-due date in YYYY-MM-DD if the email states or clearly
          implies one, otherwise null.
        - relatedDrawingSpec: any related drawing or specification mentioned, otherwise null.
        - rationale: one short sentence explaining your project/type choice (for the triager only).

        Respond with ONLY a single JSON object, no markdown fence and no prose, with exactly these keys:
        {"projectId": string|null, "type": string, "title": string, "detail": string,
         "raisedTo": string|null, "drawingRef": string|null, "responseDue": string|null,
         "relatedDrawingSpec": string|null, "rationale": string|null}

        Use the exact projectId value from the list (not the name). If no confident match, use null.
        """;

    private sealed record ProjectChoice(string ProjectId, string Reference, string Name, string ClientName);

    private static string BuildUserPrompt(
        Data.Entities.IntakeEmailEntity entity,
        string emailBody,
        IReadOnlyList<ProjectChoice> projects)
    {
        var sb = new StringBuilder();
        sb.AppendLine("LIVE PROJECTS (choose at most one; use the projectId exactly):");
        if (projects.Count == 0)
        {
            sb.AppendLine("(none on record)");
        }
        else
        {
            foreach (var p in projects)
                sb.AppendLine($"- projectId={p.ProjectId} | ref={p.Reference} | name={p.Name} | client={p.ClientName}");
        }

        sb.AppendLine();
        sb.AppendLine("EMAIL TO TRIAGE:");
        sb.AppendLine($"From: {entity.FromName} <{entity.FromEmail}>");
        sb.AppendLine($"Received: {entity.ReceivedAt:yyyy-MM-dd}");
        sb.AppendLine($"Subject: {entity.Subject}");
        sb.AppendLine("Body:");
        sb.AppendLine(emailBody);

        return sb.ToString();
    }

    private RequestSuggestion Parse(string raw, ISet<string> knownProjectIds, RequestSuggestion fallback)
    {
        var json = ExtractJsonObject(raw);
        if (json is null)
        {
            logger.LogWarning("AI suggestion: no JSON object found in model output.");
            return fallback;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var projectId = GetString(root, "projectId");
            if (projectId is not null && !knownProjectIds.Contains(projectId))
                projectId = null; // never return a project id we don't recognise

            var kind = MapType(GetString(root, "type"));
            var title = GetString(root, "title");
            var detail = GetString(root, "detail");

            // If the model gave us nothing usable for the core fields, keep the plain fallback text.
            if (string.IsNullOrWhiteSpace(title)) title = fallback.Title;
            if (string.IsNullOrWhiteSpace(detail)) detail = fallback.Description;

            return new RequestSuggestion(
                Available: true,
                ProjectId: projectId,
                Kind: kind,
                Title: title!,
                Description: detail!,
                RaisedTo: GetString(root, "raisedTo"),
                DrawingRef: GetString(root, "drawingRef"),
                ResponseDue: ParseDate(GetString(root, "responseDue")),
                RelatedDrawingSpec: GetString(root, "relatedDrawingSpec"),
                Value: null,
                Rationale: GetString(root, "rationale"));
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "AI suggestion: failed to parse model JSON.");
            return fallback;
        }
    }

    private static RequestType MapType(string? type) => (type ?? "").Trim().ToUpperInvariant() switch
    {
        "RFI" => RequestType.Rfi,
        "RFA" => RequestType.Rfa,
        "RFC" => RequestType.Rfc,
        "RFQ" => RequestType.Rfq,
        "RFP" => RequestType.Rfp,
        "NOD" => RequestType.NoticeOfDelay,
        "NOTICEOFDELAY" => RequestType.NoticeOfDelay,
        "EOT" => RequestType.ExtensionOfTime,
        "EXTENSIONOFTIME" => RequestType.ExtensionOfTime,
        _ => RequestType.Rfi
    };

    private static string? GetString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        var s = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        s = s?.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
            return dt;
        return null;
    }

    // Pull the first balanced {...} JSON object out of the model's text, tolerating any stray prose
    // or markdown fence the model may have added despite instructions.
    private static string? ExtractJsonObject(string raw)
    {
        var start = raw.IndexOf('{');
        if (start < 0) return null;

        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var i = start; i < raw.Length; i++)
        {
            var c = raw[i];
            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
            }
            else
            {
                if (c == '"') inString = true;
                else if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) return raw.Substring(start, i - start + 1);
                }
            }
        }
        return null;
    }

    // Strip an HTML email body down to readable plain text: sanitise, drop tags, collapse whitespace.
    private static string HtmlToText(string html)
    {
        var clean = new HtmlSanitizer().Sanitize(html);
        clean = Regex.Replace(clean, "<(br|/p|/div|/tr|/li)[^>]*>", "\n", RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, "<[^>]+>", " ");
        clean = System.Net.WebUtility.HtmlDecode(clean);
        clean = Regex.Replace(clean, "[ \t]+", " ");
        clean = Regex.Replace(clean, "(\\s*\n\\s*){2,}", "\n\n");
        return clean.Trim();
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
