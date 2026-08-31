using System.Text.Json;
using Jewel.JPMS.Api.Features.Ai.Tools.Actions;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The action gateway (2026-08-28): full write parity for the connector through THREE tools
/// instead of two hundred. Every command endpoint in the api is mirrored by an
/// <see cref="AiAction"/> declaration; the model lists what its user may do (list_actions),
/// reads one action's exact argument schema (describe_action), and performs it
/// (perform_action) — which composes the same Authorisation + Validation + command handler the
/// portal's own endpoint runs, actor stamped server-side. Publishing 200+ first-class tools
/// would drown every MCP client's context; three tools with an in-band catalogue keep
/// tools/list small while exposing the whole surface. The role filter is applied at listing
/// AND at execution: an action the caller's roles do not admit is never described (ADR-002)
/// and refused if named blind.
/// </summary>
internal static class AiActionGatewayTools
{
    /// <summary>Everyone signed in may see the gateway — what it CONTAINS is filtered per caller,
    /// and several actions (timesheets, site sign-in) genuinely belong to site and external roles.</summary>
    private static readonly RoleSet AllSignedIn = RoleSet.Of(Enum.GetValues<Role>());

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    /// <summary>What the model is told when an action gates on confirmation — the same wording from
    /// describe_action (so it can plan the confirm turn) and from perform_action's refusal (so a
    /// blind first call learns the protocol). First check the portal for an existing record before
    /// proposing a create — duplicates are the second thing this gate exists to catch.</summary>
    private const string ConfirmationProtocol =
        "Before performing this action: verify it is really needed (for a create, search the portal "
        + "for an existing record first and tell the user what you found), then show the user in "
        + "plain language exactly what will happen — every value you are about to send — and wait "
        + "for their explicit yes in this conversation. Only then call perform_action again with the "
        + "same name and arguments plus confirm: true. Never send confirm: true on a first attempt, "
        + "and never treat an earlier or general instruction as the user's yes.";

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);

    private static IEnumerable<AiAction> Permitted(AiToolContext context) =>
        AiActionRegistry.All.Where(action => action.VisibleTo.IncludesAny(context.User.Roles));

    public static IReadOnlyList<AiTool> Build()
    {
        return new List<AiTool>
        {
            new(
                "list_actions",
                "The portal actions you can PERFORM for this user, beyond the first-class tools: "
                + "creating and editing records (requests, RFIs, variations, work orders, bid "
                + "packages, valuations, invoices, to-dos, calendar events…), progressing statuses, "
                + "approvals, and sending portal email. Returns every action the user's roles allow, "
                + "grouped by area, one line each. Call describe_action next for the one you need — "
                + "do not guess arguments. Optionally filter with area (as returned) or a search "
                + "term matching name/description.",
                AiToolSchema.Object(
                    ("area", "string", "Only this area (exactly as a previous list_actions returned it).", false),
                    ("search", "string", "Case-insensitive term matched against action names and descriptions.", false)),
                AiToolKind.Read,
                AllSignedIn,
                async (context, input, cancellationToken) =>
                {
                    var area = AiToolSchema.Text(input, "area");
                    var search = AiToolSchema.Text(input, "search");
                    var actions = Permitted(context);
                    if (!string.IsNullOrWhiteSpace(area))
                        actions = actions.Where(action => string.Equals(action.Area, area, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrWhiteSpace(search))
                        actions = actions.Where(action =>
                            action.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || action.Description.Contains(search, StringComparison.OrdinalIgnoreCase));

                    // Actions the team has attached doctrine to are marked in their one-liner, so
                    // the model knows describe_action carries guidance it must read, not just a schema.
                    var guided = await AiActionSkillGuidance.TargetsWithGuidanceAsync(context.Db, cancellationToken);

                    var grouped = actions
                        .GroupBy(action => action.Area)
                        .OrderBy(group => group.Key)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(action => new
                            {
                                name = action.Name,
                                summary = FirstSentence(action.Description)
                                    + (guided.Contains(action.Name) || guided.Contains(action.Area)
                                        ? " [team guidance attached — describe_action includes it]"
                                        : "")
                            }).ToList());

                    return Serialise(new
                    {
                        actions = grouped,
                        note = "Every action runs immediately under the user's own account with their portal "
                               + "permissions and is logged. Read describe_action before performing one, and "
                               + "confirm wording/amounts with the user for anything external-facing or financial."
                    });
                }),

            new(
                "describe_action",
                "The full contract for one action from list_actions: what it changes, its side "
                + "effects, prerequisites, the exact JSON schema perform_action expects, and any "
                + "guidance skills the team has attached to it — doctrine you must follow when "
                + "performing it. Always call this before the first perform_action of an action "
                + "in a conversation.",
                AiToolSchema.Object(
                    ("name", "string", "The action name exactly as list_actions returned it.", true)),
                AiToolKind.Read,
                AllSignedIn,
                async (context, input, cancellationToken) =>
                {
                    var name = AiToolSchema.Text(input, "name") ?? "";
                    var action = AiActionRegistry.Find(name);
                    if (action is null || !action.VisibleTo.IncludesAny(context.User.Roles))
                        return Serialise(new
                        {
                            ok = false,
                            error = $"No action named '{name}' is available to this user. Call list_actions."
                        });

                    // The team's attached doctrine rides in WITH the schema — the one road the
                    // model must travel before performing — so following it never depends on the
                    // model deciding to go looking (docs/ai/10-mcp-connector.md; AI Actions page).
                    var guidance = await AiActionSkillGuidance.LoadForAsync(context.Db, action, cancellationToken);

                    return Serialise(new
                    {
                        name = action.Name,
                        area = action.Area,
                        description = action.Description,
                        notes = action.Notes,
                        requiresConfirmation = action.RequiresConfirmation,
                        confirmation = action.RequiresConfirmation ? ConfirmationProtocol : null,
                        guidance = guidance.Count == 0 ? null : guidance,
                        guidanceNote = guidance.Count == 0
                            ? null
                            : "The team attached these skills to this action — read them and follow "
                              + "what they say when performing it. Their reference documents load on "
                              + "demand with load_skill_reference(skill_key, ref_key).",
                        argumentsSchema = AiActionSchema.InputSchema(action)
                    });
                }),

            new(
                "perform_action",
                "PERFORM one portal action from list_actions AS THE SIGNED-IN USER — it executes "
                + "immediately through the same authorisation, validation and handler the portal "
                + "uses, is recorded under their name, and is not previewed or undoable here. For "
                + "actions that approve, pay, delete, or email people outside the team, state "
                + "exactly what you are about to do and get the user's yes first. Actions marked "
                + "requiresConfirmation by describe_action REFUSE their first call: show the user "
                + "what will happen, get their explicit yes, then re-call with confirm: true. "
                + "Arguments must follow describe_action's schema.",
                AiToolSchema.Object(
                    ("name", "string", "The action name from list_actions.", true),
                    ("arguments", "object", "The action's arguments per describe_action's schema.", true),
                    ("confirm", "boolean", "Only for an action describe_action marks requiresConfirmation, "
                        + "and only after the user has seen exactly what will happen and said yes in this "
                        + "conversation: true performs it. Never sent on the first call.", false)),
                AiToolKind.Write,
                AllSignedIn,
                async (context, input, cancellationToken) =>
                {
                    var name = AiToolSchema.Text(input, "name") ?? "";
                    var action = AiActionRegistry.Find(name);
                    if (action is null || !action.VisibleTo.IncludesAny(context.User.Roles))
                        return Serialise(new
                        {
                            ok = false,
                            error = $"No action named '{name}' is available to this user. Call list_actions."
                        });

                    // The confirm-first gate (2026-08-28): creating a party/account or doing anything
                    // irreversible must be a TWO-step act — the server refuses the first call outright,
                    // so a model can never quietly mint a subcontractor, client or portal user (or
                    // delete something with no undo) in one move. The re-call must say confirm: true,
                    // which the model is told to send only after the user's explicit yes.
                    if (action.RequiresConfirmation && AiToolSchema.Flag(input, "confirm") != true)
                        return Serialise(new
                        {
                            ok = false,
                            confirmationRequired = true,
                            action = action.Name,
                            error = $"NOT performed — '{action.Name}' requires the user's explicit "
                                + "confirmation first. " + ConfirmationProtocol
                        });

                    var arguments = input.ValueKind == JsonValueKind.Object && input.TryGetProperty("arguments", out var element)
                        ? element
                        : default;

                    return await AiActionExecutor.RunAsync(action, context, arguments, cancellationToken);
                })
        };
    }

    private static string FirstSentence(string description)
    {
        var stop = description.IndexOf(". ", StringComparison.Ordinal);
        return stop > 0 ? description[..(stop + 1)] : description;
    }
}
