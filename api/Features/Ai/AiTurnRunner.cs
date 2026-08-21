using System.Diagnostics;
using System.Text.Json;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// One hop of a turn: rebuild the transcript, make exactly ONE Claude call, run whatever tools it
/// asked for, persist everything, and hand back what happened.
///
/// <para>Deliberately not a loop. The client pumps, so the panel can say "Looking up V72" while the
/// next hop runs instead of showing a spinner for twenty seconds. It also keeps every request far
/// inside the Static Web Apps gateway's ~45 seconds, which a four-call loop never reliably was.</para>
///
/// <para>The server stays authoritative throughout: the transcript is rebuilt from the database on
/// every hop and the client contributes nothing but its own user message.</para>
/// </summary>
public sealed class AiTurnRunner
{
    /// <summary>Claude round trips per user message, across all hops. Enough for a real multi-step
    /// job — look-ups, a navigation, a draft — and bounded so a confused model cannot spend
    /// indefinitely. Raised from 6 alongside prompt caching (which makes a hop cheap) and the
    /// budget line in the turn context (which lets the model plan its spend); the panel's "Carry
    /// on" chip is the escape hatch when even this is not enough.</summary>
    public const int MaxHops = 10;

    private readonly JpmsContext context;
    private readonly IClaudeConversationClient claude;
    private readonly AgentActivityLog activityLog;
    private readonly IServiceProvider services;

    public AiTurnRunner(
        JpmsContext context, IClaudeConversationClient claude,
        AgentActivityLog activityLog, IServiceProvider services)
    {
        this.context = context;
        this.claude = claude;
        this.activityLog = activityLog;
        this.services = services;
    }

    public async Task<AiTurnResult> RunHopAsync(
        AiConversationEntity conversation,
        SignedInUser user,
        AiScope? scope,
        string? modelTier,
        CancellationToken cancellationToken)
    {
        var clock = Stopwatch.StartNew();
        var rows = await LoadAsync(conversation.ConversationId, cancellationToken);
        var sequence = rows.Count == 0 ? 0 : rows.Max(row => row.Sequence);

        // Hops already spent on THIS user message — derived from the transcript rather than stored,
        // so there is no mid-turn state that can be left stale by a failed request.
        var lastUserIndex = rows.FindLastIndex(row => row.Role == (int)AiChatRole.User);
        var hopsSpent = lastUserIndex < 0
            ? 0
            : rows.Skip(lastUserIndex).Count(row => row.Role == (int)AiChatRole.Assistant);

        var newMessages = new List<AiConversationMessageEntity>();
        var steps = new List<AiStep>();
        var uiActions = new List<AiUiAction>();

        if (!claude.IsConfigured)
        {
            var unavailable = Add(conversation, AiChatRole.Assistant,
                "The assistant is not connected yet — no Anthropic API key is configured on this "
                + "environment. Ask an administrator to set Anthropic__ApiKey.",
                ++sequence, newMessages);
            await SaveAsync(conversation, cancellationToken);
            await LogAsync(conversation, user, scope, AgentOutcome.NotConfigured,
                "No Anthropic API key is configured.", steps, clock, 0, 0, cancellationToken);
            return Result(conversation, AiTurnStatus.Unavailable, newMessages, uiActions, steps, 0);
        }

        if (hopsSpent >= MaxHops)
        {
            var stopped = Add(conversation, AiChatRole.Assistant,
                "I've used up the look-ups I'm allowed for one question. Ask me again and I'll carry on "
                + "from where I got to.",
                ++sequence, newMessages);
            await SaveAsync(conversation, cancellationToken);
            await LogAsync(conversation, user, scope, AgentOutcome.Truncated,
                "Hop budget exhausted.", steps, clock, 0, 0, cancellationToken);
            return Result(conversation, AiTurnStatus.Truncated, newMessages, uiActions, steps, 0);
        }

        var project = string.IsNullOrWhiteSpace(scope?.ProjectId)
            ? null
            : await context.Projects.AsNoTracking()
                .FirstOrDefaultAsync(row => row.ProjectId == scope!.ProjectId, cancellationToken);

        // The agent in force this hop. A conversation stamped with a key the catalogue no longer
        // knows (renamed, retired) degrades to the orchestrator rather than failing the turn; a
        // key the CALLER may not engage degrades the same way — a conversation id is not a
        // capability, and neither is a persisted agent key.
        var agent = AgentCatalogue.Find(conversation.CapabilityKey) ?? AgentCatalogue.Orchestrator;
        if (!AgentCatalogue.CanEngage(agent, user.Roles)) agent = AgentCatalogue.Orchestrator;

        // The agent's skills plus the shared set, fresh from the database every hop — a portal
        // edit is in force on the very next message. Bodies are fetched for PINNED skills only;
        // the rest ride as one menu line each and arrive via load_skill.
        var skills = await LoadSkillsAsync(agent.Key, cancellationToken);

        var systemPrompt = AiSystemPrompt.Build(user, scope, project?.Reference, project?.Name, agent, skills);
        var tools = AiToolCatalogue.For(user, scope, agent)
            .Select(tool => new ClaudeToolSpec(tool.Name, tool.Description, tool.InputSchema))
            .ToList();

        // The volatile facts — where the user is, the dialog's live contents, the look-up budget —
        // ride as a block on the NEWEST message rather than in the system prompt, so the system
        // prompt and the transcript prefix stay byte-stable across hops and cache (see
        // ClaudeConversationClient). Rebuilt every hop, never persisted.
        var turnContext = AiSystemPrompt.BuildTurnContext(
            user, scope, project?.Reference, project?.Name, hopsSpent, MaxHops);

        var reply = await claude.ContinueAsync(
            systemPrompt, BuildTranscript(rows, turnContext), tools, modelTier, cancellationToken);

        if (!reply.Ok)
        {
            var failed = Add(conversation, AiChatRole.Assistant,
                "I could not reach the Claude API just then. Try again in a moment — nothing has been changed.",
                ++sequence, newMessages);
            await SaveAsync(conversation, cancellationToken);
            await LogAsync(conversation, user, scope, AgentOutcome.Failed,
                $"The Claude API call failed ({reply.Error}).", steps, clock, 0, 0, cancellationToken);
            return Result(conversation, AiTurnStatus.Unavailable, newMessages, uiActions, steps, 0);
        }

        // The assistant turn is persisted WITH its tool_use blocks. Without them the tool_result that
        // follows has nothing to pair with and Anthropic rejects the next hop.
        var assistantRow = Add(conversation, AiChatRole.Assistant, reply.Text ?? "", ++sequence, newMessages);
        if (reply.ToolCalls.Count > 0)
        {
            assistantRow.ToolCallsJson = JsonSerializer.Serialize(
                reply.ToolCalls.Select(call => new { id = call.Id, name = call.Name, input = call.ArgumentsJson }));
        }

        var toolContext = new AiToolContext(context, user, scope, services, agent.Key);

        foreach (var call in reply.ToolCalls)
        {
            var label = AiToolLabels.For(call.Name, call.ArgumentsJson);
            var tool = AiToolCatalogue.Find(call.Name);
            string output;
            var ok = true;

            if (tool is null)
            {
                output = Fail($"No tool named {call.Name}.");
                ok = false;
            }
            else if (!tool.VisibleTo.IncludesAny(user.Roles))
            {
                output = Fail("You are not permitted to use that tool.");
                ok = false;
            }
            else if (string.Equals(call.Name, AiToolCatalogue.SwitchAgent, StringComparison.OrdinalIgnoreCase))
            {
                // Executed here, not in the catalogue: it writes the conversation's own
                // CapabilityKey, which no ordinary tool can reach. Takes effect on the NEXT hop —
                // this hop's tools and prompt were assembled under the old agent, and rebuilding
                // them mid-hop would mean a tool list the model was never shown.
                output = SwitchAgent(conversation, user, call.ArgumentsJson, out ok);
            }
            else if (tool.Kind == AiToolKind.Ui)
            {
                // Checked BEFORE handing to the browser. A Ui tool's "ok" used to mean only
                // "posted" — so an open_modal with an invented record id navigated the user to a
                // dead page while the model narrated success. Refusing here puts the failure in
                // front of the model instead, and it corrects course.
                var refusal = await ValidateUiActionAsync(call, scope, cancellationToken);
                if (refusal is not null)
                {
                    output = refusal;
                    ok = false;
                }
                else
                {
                    // open_modal only: complete the arguments the server can KNOW — a record
                    // dialog's project comes from the record's own row, so the client-side route
                    // never depends on which page the user happens to be on. (Live failure
                    // 2026-08-21: work_order_edit opened from the To-dos page — no project in
                    // view, the model omitted project_id, the browser refused AFTER the turn had
                    // ended, and the model narrated success.)
                    var argumentsJson = string.Equals(call.Name, "open_modal", StringComparison.OrdinalIgnoreCase)
                        ? await CompleteOpenModalArgumentsAsync(call.ArgumentsJson, cancellationToken)
                        : call.ArgumentsJson;
                    uiActions.Add(new AiUiAction(call.Name, argumentsJson));
                    output = JsonSerializer.Serialize(new { ok = true, handed_to_browser = true });
                }
            }
            else
            {
                try
                {
                    using var arguments = JsonDocument.Parse(
                        string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson);
                    output = await tool.ExecuteAsync(toolContext, arguments.RootElement, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    output = Fail($"The tool failed: {ex.Message}");
                    ok = false;
                }
            }

            var toolRow = Add(conversation, AiChatRole.Tool, output, ++sequence, newMessages);
            toolRow.ToolName = call.Name;
            toolRow.ToolUseId = call.Id;

            steps.Add(new AiStep(label, call.Name, ok));
        }

        await SaveAsync(conversation, cancellationToken);

        var moreToDo = reply.ToolCalls.Count > 0;
        var remaining = Math.Max(0, MaxHops - (hopsSpent + 1));
        var status = !moreToDo ? AiTurnStatus.Complete
            : remaining == 0 ? AiTurnStatus.Truncated
            : AiTurnStatus.NeedsContinue;

        // One activity row per hop. A turn that took three hops reads as three rows, which is what
        // you want when reconstructing what the assistant actually did.
        await LogAsync(conversation, user, scope,
            status == AiTurnStatus.Truncated ? AgentOutcome.Truncated : AgentOutcome.Ok,
            string.IsNullOrWhiteSpace(reply.Text)
                ? string.Join(", ", steps.Select(step => step.Label))
                : Truncate(reply.Text!, 400),
            steps, clock, reply.InputTokens, reply.OutputTokens, cancellationToken);

        return Result(conversation, status, newMessages, uiActions, steps, remaining, reply.EscalationNote);
    }

    // ---- Ui-action validation ----------------------------------------------------------------

    /// <summary>
    /// Sanity-checks a Ui action the server can verify, returning a refusal payload (or null to
    /// proceed). Today that is open_modal: the dialog must exist, a dialog whose route names a
    /// record must be given a record id, and for variation_draft that id must be a REAL request —
    /// the failure the model must see is "no such request", not a user stranded on a dead page.
    /// </summary>
    private async Task<string?> ValidateUiActionAsync(ClaudeToolCall call, AiScope? scope, CancellationToken ct)
    {
        if (string.Equals(call.Name, "stage_triage_tag", StringComparison.OrdinalIgnoreCase))
            return await ValidateStageTagAsync(call, ct);

        if (string.Equals(call.Name, "stage_triage_todo", StringComparison.OrdinalIgnoreCase))
            return ValidateStageTodo(call);

        if (!string.Equals(call.Name, "open_modal", StringComparison.OrdinalIgnoreCase)) return null;

        string? modalKey = null;
        string? recordId = null;
        string? projectId = null;
        try
        {
            using var arguments = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson);
            if (arguments.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (arguments.RootElement.TryGetProperty("modal_key", out var keyElement)
                    && keyElement.ValueKind == JsonValueKind.String)
                    modalKey = keyElement.GetString();
                if (arguments.RootElement.TryGetProperty("record_id", out var recordElement)
                    && recordElement.ValueKind == JsonValueKind.String)
                    recordId = recordElement.GetString();
                if (arguments.RootElement.TryGetProperty("project_id", out var projectElement)
                    && projectElement.ValueKind == JsonValueKind.String)
                    projectId = projectElement.GetString();
            }
        }
        catch (JsonException)
        {
        }

        var modal = ModalCatalog.Find(modalKey);
        if (modal is null)
        {
            return Fail($"No dialog named {modalKey}. The dialogs are: "
                + string.Join(", ", ModalCatalog.All.Select(candidate => candidate.ModalKey)) + ".");
        }

        // A project dialog the BROWSER cannot place must be refused HERE, in front of the model —
        // the client's own refusal happens after this turn has ended, so the model narrates a
        // dialog that never opened (exactly the 2026-08-21 work_order_edit live failure). Record
        // dialogs are exempt: their project is derived from the record's own row when the action
        // is handed over (CompleteOpenModalArgumentsAsync), which also protects the user who is
        // standing on a DIFFERENT project's page.
        if (modal.RouteTemplate.Contains("{project}", StringComparison.Ordinal)
            && !ProjectDerivableFromRecord(modal.ModalKey)
            && string.IsNullOrWhiteSpace(projectId)
            && string.IsNullOrWhiteSpace(scope?.ProjectId))
        {
            return Fail($"{modal.ModalKey} needs project_id — the user is not on one of that project's "
                + "pages, so the browser cannot tell which project to open it in. Pass the project's id "
                + "(list_projects returns ids); never guess one.");
        }

        var needsRecord = modal.RouteTemplate.Contains("{record}", StringComparison.Ordinal);
        if (!needsRecord) return null;

        if (string.IsNullOrWhiteSpace(recordId))
        {
            return Fail($"{modal.ModalKey} needs record_id — the real id of the record it works from: "
                + "the request id (find_by_reference or list_requests) for variation_draft, the bid "
                + "package id (the record in view, or get_bid_package_context) for bid_package_details, "
                + "the work order id (get_work_order_context, which resolves \"WO-0045\") for "
                + "work_order_edit. Do not invent one.");
        }

        // The record must actually exist before anyone navigates — the failure the model must see
        // is "no such record", not a user stranded on a dead page. Checked against the table the
        // DIALOG works from, per dialog: assuming every record dialog meant a REQUEST is the bug
        // that refused bid package ids on 2026-08-16.
        if (string.Equals(modal.ModalKey, ModalCatalog.VariationDraft.ModalKey, StringComparison.OrdinalIgnoreCase))
        {
            var requestExists = await context.Requests.AsNoTracking()
                .AnyAsync(row => row.RequestId == recordId, ct);
            if (!requestExists)
            {
                return Fail($"No request exists with id \"{recordId}\" — that is not a real record id. Call "
                    + "find_by_reference or list_requests for the actual id. For a variation with no RFI "
                    + "behind it, use manual_variation instead.");
            }
            return null;
        }

        if (string.Equals(modal.ModalKey, ModalCatalog.BidPackageDetails.ModalKey, StringComparison.OrdinalIgnoreCase))
        {
            var packageExists = await context.BidPackages.AsNoTracking()
                .AnyAsync(row => row.BidPackageId == recordId, ct);
            if (!packageExists)
            {
                return Fail($"No bid package exists with id \"{recordId}\" — that is not a real record id. "
                    + "Use the id of the package on the page in view, or the one get_bid_package_context "
                    + "answered for.");
            }
            return null;
        }

        if (string.Equals(modal.ModalKey, ModalCatalog.WorkOrderEdit.ModalKey, StringComparison.OrdinalIgnoreCase))
        {
            var orderExists = await context.WorkOrders.AsNoTracking()
                .AnyAsync(row => row.WorkOrderId == recordId, ct);
            if (!orderExists)
            {
                return Fail($"No work order exists with id \"{recordId}\" — that is not a real record id. "
                    + "get_work_order_context (by reference, e.g. WO-0045) returns the actual id. Do not "
                    + "invent one.");
            }
            return null;
        }

        // tender_reply is anchored to a specific tender EMAIL, which only the bid package page can
        // supply — opening it by navigation would land on the page with no composer showing. When
        // the task is live the composer is already open; there is nothing for open_modal to do.
        if (string.Equals(modal.ModalKey, ModalCatalog.TenderReply.ModalKey, StringComparison.OrdinalIgnoreCase))
        {
            return Fail("tender_reply can't be opened from here — the user opens it from a tender "
                + "extraction's \"Draft supplier reply\", and it is already open beside you when that "
                + "task is running. Use update_open_modal to fill it.");
        }

        // A record dialog this validator doesn't know which table to check — let it through rather
        // than refuse a real id. The client still refuses loudly for anything actually wrong, and
        // a stale id lands on the record page's own not-found handling, never silently nowhere.
        return null;
    }

    /// <summary>The record dialogs whose project the server derives from the record's own row —
    /// keep in step with <see cref="CompleteOpenModalArgumentsAsync"/>.</summary>
    private static bool ProjectDerivableFromRecord(string modalKey) =>
        string.Equals(modalKey, ModalCatalog.VariationDraft.ModalKey, StringComparison.OrdinalIgnoreCase)
        || string.Equals(modalKey, ModalCatalog.BidPackageDetails.ModalKey, StringComparison.OrdinalIgnoreCase)
        || string.Equals(modalKey, ModalCatalog.WorkOrderEdit.ModalKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Completes a validated open_modal's arguments with what the server KNOWS: a record dialog's
    /// project is the record's own ProjectId, so it is stamped in (overwriting whatever the model
    /// sent — the row is the truth, a mistyped project_id would 404 the route). Without this the
    /// client fell back to "the project in view", which is null on whole-company pages (the
    /// To-dos page, the Control Centre) and WRONG when the user is standing on another project.
    /// Best-effort: anything unparseable goes through unchanged and the client's own loud
    /// refusals still apply.
    /// </summary>
    private async Task<string> CompleteOpenModalArgumentsAsync(string argumentsJson, CancellationToken ct)
    {
        try
        {
            if (System.Text.Json.Nodes.JsonNode.Parse(
                    string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson)
                is not System.Text.Json.Nodes.JsonObject root)
                return argumentsJson;

            var modalKey = root["modal_key"] is System.Text.Json.Nodes.JsonValue keyValue
                           && keyValue.TryGetValue<string>(out var keyText) ? keyText : null;
            var recordId = root["record_id"] is System.Text.Json.Nodes.JsonValue recordValue
                           && recordValue.TryGetValue<string>(out var recordText) ? recordText : null;
            if (string.IsNullOrWhiteSpace(modalKey) || string.IsNullOrWhiteSpace(recordId))
                return argumentsJson;

            string? projectId = null;
            if (string.Equals(modalKey, ModalCatalog.VariationDraft.ModalKey, StringComparison.OrdinalIgnoreCase))
            {
                projectId = await context.Requests.AsNoTracking()
                    .Where(row => row.RequestId == recordId)
                    .Select(row => row.ProjectId)
                    .FirstOrDefaultAsync(ct);
            }
            else if (string.Equals(modalKey, ModalCatalog.BidPackageDetails.ModalKey, StringComparison.OrdinalIgnoreCase))
            {
                projectId = await context.BidPackages.AsNoTracking()
                    .Where(row => row.BidPackageId == recordId)
                    .Select(row => row.ProjectId)
                    .FirstOrDefaultAsync(ct);
            }
            else if (string.Equals(modalKey, ModalCatalog.WorkOrderEdit.ModalKey, StringComparison.OrdinalIgnoreCase))
            {
                projectId = await context.WorkOrders.AsNoTracking()
                    .Where(row => row.WorkOrderId == recordId)
                    .Select(row => row.ProjectId)
                    .FirstOrDefaultAsync(ct);
            }
            if (string.IsNullOrWhiteSpace(projectId)) return argumentsJson;

            root["project_id"] = projectId;
            return root.ToJsonString();
        }
        catch (JsonException)
        {
            return argumentsJson;
        }
    }

    /// <summary>stage_triage_todo needs a real title — an empty row would stage nothing and the
    /// model would narrate a to-do that never appears.</summary>
    private static string? ValidateStageTodo(ClaudeToolCall call)
    {
        try
        {
            using var arguments = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson);
            if (arguments.RootElement.ValueKind == JsonValueKind.Object
                && arguments.RootElement.TryGetProperty("title", out var title)
                && title.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(title.GetString()))
            {
                return null;
            }
        }
        catch (JsonException)
        {
        }
        return Fail("stage_triage_todo needs a title — what is to be done, as the to-do list will show it.");
    }

    /// <summary>
    /// stage_triage_tag before it reaches the browser: the type must be one the record-link layer
    /// knows, and where the record lives in a table this side (requests, variations) it must
    /// actually exist — an invented id staged into the System Tags pane would tag a real email to
    /// nothing, and nobody would notice until the record read its mail back and found silence.
    /// </summary>
    private async Task<string?> ValidateStageTagAsync(ClaudeToolCall call, CancellationToken ct)
    {
        string? typeText = null;
        string? recordId = null;
        string? projectId = null;
        try
        {
            using var arguments = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson);
            if (arguments.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (arguments.RootElement.TryGetProperty("record_type", out var typeElement)
                    && typeElement.ValueKind == JsonValueKind.String)
                    typeText = typeElement.GetString();
                if (arguments.RootElement.TryGetProperty("record_id", out var idElement)
                    && idElement.ValueKind == JsonValueKind.String)
                    recordId = idElement.GetString();
                if (arguments.RootElement.TryGetProperty("project_id", out var projectElement)
                    && projectElement.ValueKind == JsonValueKind.String)
                    projectId = projectElement.GetString();
            }
        }
        catch (JsonException)
        {
        }

        if (string.IsNullOrWhiteSpace(typeText) || string.IsNullOrWhiteSpace(recordId)
            || string.IsNullOrWhiteSpace(projectId))
        {
            return Fail("stage_triage_tag needs record_type, record_id AND project_id — all from the "
                + "tool result that found the record. Do not invent any of them.");
        }

        if (!AiRecordTools.TryMapRecordType(typeText, out var recordType))
            return Fail($"\"{typeText}\" is not a taggable record type. Use one of: request, "
                + "bid_package, variation, variation_quote, work_order, todo, lad, scheduling.");

        // Where the record lives in a table this side, verify it exists ON THE PROJECT CLAIMED —
        // a right id with the wrong project is exactly the V80-on-three-projects mistake.
        var recordExists = recordType switch
        {
            RecordType.Request => await context.Requests.AsNoTracking()
                .AnyAsync(row => row.RequestId == recordId && row.ProjectId == projectId, ct),
            RecordType.Variation or RecordType.VariationQuote => await context.VariationOrders.AsNoTracking()
                .AnyAsync(row => row.VariationOrderId == recordId && row.ProjectId == projectId, ct),
            // Other types live behind their own providers — the page's own lookup is the check.
            _ => true
        };

        return recordExists
            ? null
            : Fail($"No {recordType} with id \"{recordId}\" exists on project \"{projectId}\". Use the "
                + "id AND projectId from the same tool result — and remember the record must be on the "
                + "email's own project (the current context names it).");
    }

    // ---- agents and skills -----------------------------------------------------------------

    /// <summary>
    /// The model asked to change hat. Validated exactly like any other capability: the key must
    /// exist and the CALLER's roles must be allowed to engage it — the model cannot talk its way
    /// into an agent the person could not have chosen from a menu.
    /// </summary>
    private string SwitchAgent(
        AiConversationEntity conversation, SignedInUser user, string? argumentsJson, out bool ok)
    {
        ok = false;

        string? key = null;
        try
        {
            using var arguments = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            if (arguments.RootElement.ValueKind == JsonValueKind.Object
                && arguments.RootElement.TryGetProperty("agent", out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                key = value.GetString();
            }
        }
        catch (JsonException)
        {
        }

        var destination = AgentCatalogue.Find(key);
        if (destination is null)
            return Fail($"No agent named {key}. Only the agents listed in the tool description exist.");
        if (!AgentCatalogue.CanEngage(destination, user.Roles))
            return Fail("This user may not engage that agent.");
        if (string.Equals(conversation.CapabilityKey, destination.Key, StringComparison.OrdinalIgnoreCase))
            return Fail($"The {destination.DisplayName} agent is already in force.");

        conversation.CapabilityKey = destination.Key;
        ok = true;
        return JsonSerializer.Serialize(new
        {
            ok = true,
            switched_to = destination.Key,
            note = $"You are now the {destination.DisplayName} agent. Your tools, working rules and "
                   + "skills change from your next step — continue the task under them."
        });
    }

    /// <summary>The agent's skills plus the shared set. Pinned bodies come back whole; unpinned
    /// skills come back as menu lines (key + description) with no body — load_skill fetches those.</summary>
    private async Task<IReadOnlyList<AiSystemPrompt.PromptSkill>> LoadSkillsAsync(
        string agentKey, CancellationToken ct)
    {
        var rows = await context.Skills
            .AsNoTracking()
            .Where(row => row.IsActive
                          && (row.AgentKey == agentKey || row.AgentKey == AiSkillTools.SharedAgentKey))
            .OrderBy(row => row.AgentKey == AiSkillTools.SharedAgentKey ? 0 : 1)
            .ThenBy(row => row.DisplayName)
            .Select(row => new
            {
                row.SkillKey, row.DisplayName, row.Description, row.Pinned, row.Version,
                Body = row.Pinned ? row.Body : null
            })
            .ToListAsync(ct);

        return rows
            .Select(row => new AiSystemPrompt.PromptSkill(
                row.SkillKey, row.DisplayName, row.Description, row.Pinned, row.Version, row.Body))
            .ToList();
    }

    // ---- transcript ------------------------------------------------------------------------

    private Task<List<AiConversationMessageEntity>> LoadAsync(string conversationId, CancellationToken ct) =>
        context.AiConversationMessages
            .AsNoTracking()
            .Where(row => row.ConversationId == conversationId)
            .OrderBy(row => row.Sequence)
            .ToListAsync(ct);

    /// <summary>
    /// Rebuilds the Anthropic messages array. Assistant rows carrying tool_use blocks are replayed
    /// verbatim, and the tool rows that follow are grouped into a single user message of
    /// tool_result blocks — the shape the API requires.
    /// </summary>
    /// <summary>
    /// Rebuilds the messages array WITH the two cost bounds applied first: repeated identical
    /// calls to the big tools replay latest-only, and the whole transcript is stubbed
    /// oldest-tool-first down to <see cref="AiTranscriptBudget.MaxTranscriptChars"/>. Both bounds
    /// touch tool-result bodies only — a user or assistant turn is the conversation itself and is
    /// never rewritten. The logic lives in contracts (AiTranscriptBudget) so the test project can
    /// pin it; this method's job is to feed it and to keep the tool_use/tool_result pairing the
    /// API requires.
    /// </summary>
    private static List<object> BuildTranscript(List<AiConversationMessageEntity> rows, string turnContext)
    {
        // ---- the budget pass -----------------------------------------------------------------
        var bodies = new string[rows.Count];
        for (var i = 0; i < rows.Count; i++) bodies[i] = rows[i].Body ?? "";

        // Image attachments stand in as a short line for the BUDGET only: their base64 is hundreds
        // of KB of characters but ~1,600 tokens of image at most (the composer downscales to
        // ≤1568px), and counted raw it would blow MaxTranscriptChars on its own and stub every
        // tool row in the conversation forever after. The replay below reads the row's real Body.
        for (var i = 0; i < rows.Count; i++)
        {
            if ((AiChatRole)rows[i].Role == AiChatRole.Context && IsImageAttachment(rows[i]))
                bodies[i] = "(image attachment — replays as an image block)";
        }

        // A tool row's identity for the supersede rule is name + the arguments that produced it,
        // and the arguments live on the assistant row's stored tool_use blocks — pair them back up.
        var argumentsByToolUseId = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if ((AiChatRole)row.Role != AiChatRole.Assistant) continue;
            foreach (var call in ReadToolCalls(row.ToolCallsJson))
                argumentsByToolUseId[call.Id!] = call.Input ?? "";
        }

        var toolRows = new List<TranscriptToolRow>();
        for (var i = 0; i < rows.Count; i++)
        {
            if ((AiChatRole)rows[i].Role != AiChatRole.Tool) continue;
            var arguments = rows[i].ToolUseId is { } id && argumentsByToolUseId.TryGetValue(id, out var input)
                ? input
                : null;
            toolRows.Add(new TranscriptToolRow(i, rows[i].ToolName, arguments, rows[i].Sequence));
        }

        AiTranscriptBudget.Apply(bodies, toolRows);

        // ---- the replay ------------------------------------------------------------------------
        // A tool_result may only replay against a tool_use that actually made it into the
        // transcript — an orphan pair is rejected by the API and takes the whole turn down.
        // Ids accumulate as assistant rows are walked (they always precede their tool rows).
        //
        // Content is built as mutable block lists (not anonymous types) because the tail of the
        // transcript is edited after the walk: the newest persisted block gets the moving
        // cache_control breakpoint, and the turn-context block is appended after it.
        var replayedToolUseIds = new HashSet<string>(StringComparer.Ordinal);
        var transcript = new List<Dictionary<string, object?>>();
        // Context rows (a task's carried-over conversation) fold into the NEXT user message as a
        // leading text block — the API wants alternating roles, and context is background to the
        // user turn it precedes, not a turn of its own.
        var pendingContext = new List<Dictionary<string, object?>>();
        var index = 0;

        static Dictionary<string, object?> Text(string text) =>
            new() { ["type"] = "text", ["text"] = text };

        void AddMessage(string role, List<Dictionary<string, object?>> blocks) =>
            transcript.Add(new Dictionary<string, object?> { ["role"] = role, ["content"] = blocks });

        while (index < rows.Count)
        {
            var row = rows[index];

            switch ((AiChatRole)row.Role)
            {
                case AiChatRole.Context:
                    if (IsImageAttachment(row))
                    {
                        // An image attachment: line 1 the human sentence, line 2 the media type,
                        // the base64 after — see AddAiAttachmentHandler. The sentence rides as a
                        // text block so the model knows WHICH file the pixels are, then the image
                        // block itself. Read from row.Body, not bodies[index] — the budget pass
                        // swapped that for a stand-in. A malformed row degrades to its first line
                        // as text rather than taking the turn down.
                        var parts = (row.Body ?? "").Split('\n', 3);
                        pendingContext.Add(Text(parts[0]));
                        if (parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[2]))
                        {
                            pendingContext.Add(new Dictionary<string, object?>
                            {
                                ["type"] = "image",
                                ["source"] = new Dictionary<string, object?>
                                {
                                    ["type"] = "base64",
                                    ["media_type"] = parts[1].Trim(),
                                    ["data"] = parts[2].Trim()
                                }
                            });
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(bodies[index]))
                    {
                        pendingContext.Add(Text(bodies[index]));
                    }
                    index++;
                    break;

                case AiChatRole.User:
                {
                    var blocks = new List<Dictionary<string, object?>>(pendingContext) { Text(bodies[index]) };
                    pendingContext.Clear();
                    AddMessage("user", blocks);
                    index++;
                    break;
                }

                case AiChatRole.Assistant:
                {
                    var blocks = new List<Dictionary<string, object?>>();
                    if (!string.IsNullOrWhiteSpace(bodies[index]))
                        blocks.Add(Text(bodies[index]));

                    foreach (var call in ReadToolCalls(row.ToolCallsJson))
                    {
                        replayedToolUseIds.Add(call.Id!);
                        blocks.Add(new Dictionary<string, object?>
                        {
                            ["type"] = "tool_use",
                            ["id"] = call.Id!,
                            ["name"] = call.Name!,
                            ["input"] = ParseInput(call.Input)
                        });
                    }

                    // An assistant row with neither text nor tool calls would be an empty content
                    // array, which the API rejects. Skip it.
                    if (blocks.Count > 0) AddMessage("assistant", blocks);
                    index++;
                    break;
                }

                case AiChatRole.Tool:
                {
                    var results = new List<Dictionary<string, object?>>();
                    while (index < rows.Count && (AiChatRole)rows[index].Role == AiChatRole.Tool)
                    {
                        var toolRow = rows[index];
                        // Prose fallback covers two cases: legacy rows written before tool_use
                        // blocks were persisted, and rows whose paired tool_use could not be
                        // replayed — a tool_result without its tool_use is rejected by the API.
                        var paired = !string.IsNullOrWhiteSpace(toolRow.ToolUseId)
                                     && replayedToolUseIds.Contains(toolRow.ToolUseId!);
                        results.Add(paired
                            ? new Dictionary<string, object?>
                            {
                                ["type"] = "tool_result",
                                ["tool_use_id"] = toolRow.ToolUseId!,
                                ["content"] = bodies[index]
                            }
                            : Text($"[earlier result from {toolRow.ToolName}]\n{bodies[index]}"));
                        index++;
                    }
                    AddMessage("user", results);
                    break;
                }

                default:
                    index++;
                    break;
            }
        }

        // ---- the tail --------------------------------------------------------------------------
        // The hop always ends on a user-role message (the new user turn, or the tool results). Two
        // edits, in this order:
        //   1. The moving cache breakpoint goes on the newest PERSISTED block, so next hop's replay
        //      is a byte-identical prefix of this one and reads back from cache.
        //   2. The turn context (where the user is, dialog contents, look-up budget) is appended
        //      AFTER the breakpoint — it changes every hop, and sitting outside the cached prefix
        //      is exactly what lets it change for free.
        if (transcript.Count > 0
            && transcript[^1]["content"] is List<Dictionary<string, object?>> { Count: > 0 } tail
            && Equals(transcript[^1]["role"], "user"))
        {
            tail[^1]["cache_control"] = new { type = "ephemeral" };
            tail.Add(Text(turnContext));
        }

        return transcript.Cast<object>().ToList();
    }

    /// <summary>A Context row holding an image attachment (AddAiAttachmentHandler's marker).</summary>
    private static bool IsImageAttachment(AiConversationMessageEntity row) =>
        string.Equals(row.ToolName, "attachment-image", StringComparison.Ordinal);

    private sealed record StoredToolCall(string? Id, string? Name, string? Input);

    /// <summary>ToolCallsJson is written lowercase ({id, name, input}) and System.Text.Json is
    /// case-SENSITIVE by default — without this option every stored call deserialised to nulls,
    /// which broke the tool_use replay on every continuation hop (JPMS-B55A7A).</summary>
    private static readonly JsonSerializerOptions StoredToolCallJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static IReadOnlyList<StoredToolCall> ReadToolCalls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<StoredToolCall>();
        try
        {
            var calls = JsonSerializer.Deserialize<List<StoredToolCall>>(json, StoredToolCallJson)
                        ?? new List<StoredToolCall>();
            // A call with no id or name cannot be replayed as a tool_use block (the API rejects
            // it) and cannot key a lookup. Drop it rather than letting a bad row kill the turn —
            // its tool RESULT still replays, as prose, via the legacy branch below.
            return calls
                .Where(call => !string.IsNullOrWhiteSpace(call.Id) && !string.IsNullOrWhiteSpace(call.Name))
                .ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<StoredToolCall>();
        }
    }

    private static JsonElement ParseInput(string? input)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(input) ? "{}" : input);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            using var empty = JsonDocument.Parse("{}");
            return empty.RootElement.Clone();
        }
    }

    // ---- plumbing --------------------------------------------------------------------------

    private AiConversationMessageEntity Add(
        AiConversationEntity conversation, AiChatRole role, string body, int sequence,
        List<AiConversationMessageEntity> collected)
    {
        var entity = new AiConversationMessageEntity
        {
            MessageId = Guid.NewGuid().ToString("N"),
            ConversationId = conversation.ConversationId,
            Role = (int)role,
            Body = body,
            Sequence = sequence,
            PostedAt = DateTimeOffset.UtcNow
        };
        context.AiConversationMessages.Add(entity);
        collected.Add(entity);
        return entity;
    }

    private async Task SaveAsync(AiConversationEntity conversation, CancellationToken ct)
    {
        conversation.LastMessageAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    private Task LogAsync(
        AiConversationEntity conversation, SignedInUser user, AiScope? scope,
        AgentOutcome outcome, string summary, IReadOnlyList<AiStep> steps,
        Stopwatch clock, int inputTokens, int outputTokens, CancellationToken ct) =>
        activityLog.WriteAsync(
            agentKey: conversation.CapabilityKey,
            trigger: AgentTrigger.Chat,
            actorEmail: user.Email,
            action: "chat.hop",
            outcome: outcome,
            summary: summary,
            cancellationToken: ct,
            conversationId: conversation.ConversationId,
            projectId: scope?.ProjectId,
            route: scope?.Route,
            toolsUsed: steps.Select(step => step.Tool),
            durationMs: (int)clock.ElapsedMilliseconds,
            inputTokens: inputTokens,
            outputTokens: outputTokens);

    private static AiTurnResult Result(
        AiConversationEntity conversation, AiTurnStatus status,
        IEnumerable<AiConversationMessageEntity> messages,
        IReadOnlyList<AiUiAction> uiActions, IReadOnlyList<AiStep> steps, int remaining,
        string? modelNote = null) =>
        new(conversation.ConversationId,
            status,
            messages
                .Where(row => row.Role != (int)AiChatRole.Tool && !string.IsNullOrWhiteSpace(row.Body))
                .Select(row => new AiChatMessage(
                    row.MessageId, (AiChatRole)row.Role, row.Body, row.ToolName, row.PostedAt))
                .ToList(),
            uiActions,
            steps,
            remaining,
            // Post-switch: a switch_agent call in this hop is already on the conversation, so the
            // panel can show the new hat as soon as the hop returns.
            conversation.CapabilityKey,
            modelNote);

    private static string Fail(string message) => JsonSerializer.Serialize(new { ok = false, error = message });

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..length];
}
