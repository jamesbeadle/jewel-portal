using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// One agent: a named configuration the single conversation switches into — never a second
/// process and never a second participant (docs/ai/04-orchestration.md §2, 05-agents-and-skills.md).
///
/// <para><b>The ownership split, stated once:</b> everything in this record is DEVELOPER-OWNED and
/// hard-coded — the mechanics of the turn-based experience, which tools an agent may use, which
/// dialogs it may open, who may engage it. The DOMAIN knowledge — how Jewel prices a variation,
/// the reserve doctrine, the JCT method — is deliberately NOT here: it lives in <b>skills</b>,
/// stored in the database and edited in the portal by the people who own the discipline
/// (Skills admin page). A new commercial rule is a skill edit, not a deploy.</para>
/// </summary>
public sealed record AgentDefinition(
    /// <summary>The registry key, persisted on the conversation as CapabilityKey. Renaming an
    /// agent's display name is free; changing a key needs a data migration — prefer the former.</summary>
    string Key,
    /// <summary>What the panel and the activity log call it.</summary>
    string DisplayName,
    /// <summary>Written for the MODEL — this is what the orchestrator reads when deciding whose
    /// job a request is. State what the agent does, and pointedly, what it does not.</summary>
    string Description,
    /// <summary>Cheap routing cues, folded into switch_agent's description — phrases that mark a
    /// task as this agent's ("EOT notice", "pay-less", "draft a reply to the CA"…).</summary>
    IReadOnlyList<string> Triggers,
    /// <summary>The subset of the tool catalogue this agent may use, by tool name. NULL means
    /// unrestricted — every tool the caller's roles already allow. Restrict once agents diverge;
    /// the role filter always applies underneath either way.</summary>
    IReadOnlyList<string>? ToolNames,
    /// <summary>The ModalCatalog dialogs this agent may open. NULL means unrestricted (any dialog
    /// the caller's roles allow).</summary>
    IReadOnlyList<string>? ModalKeys,
    /// <summary>Who may engage this agent. Separate from "who may open the chat" — the panel gate
    /// is DesktopNavigation.CanUseAssistant / AiRoles.AllowedToUseAssistant; this narrows within it.</summary>
    IReadOnlyList<Role> AvailableTo,
    /// <summary>Route fragments that select this agent as a conversation's INITIAL agent — the
    /// contextual selection of docs/ai §2.1. First match wins, longest fragment first.</summary>
    IReadOnlyList<string> RoutePrefixes,
    /// <summary>Developer-owned working instructions for this agent — the MECHANICS of how it
    /// behaves in the turn loop (when to switch away, when to ask, what its job is). Domain rules
    /// do not belong here; they belong in skills.</summary>
    string PromptFragment,
    /// <summary>What this agent considers a finished job, rendered into its prompt.</summary>
    string DoneMeans);

/// <summary>
/// The agent registry. Explicit opt-in, same pattern as <see cref="ModalCatalog"/>: an agent
/// exists because a line here says so. Skills attach themselves to an agent by key from the
/// database side (SkillEntity.AgentKey), so Nigel adding a skill to the commercial agent is a
/// portal action, not an edit to this file.
/// </summary>
public static class AgentCatalogue
{
    /// <summary>The commercial team — mirrors AiRoles.AllowedToUseAssistant / the chat panel gate.
    /// Individual agents narrow from here when their discipline demands it.</summary>
    private static readonly Role[] CommercialTeam =
    {
        Role.Admin,
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor
    };

    public static readonly AgentDefinition Orchestrator = new(
        "orchestrator",
        "Orchestrator",
        "The front of house. Answers questions that one read tool gets to, navigates the portal, "
        + "and recognises when a job belongs to a specialist agent. It does NOT draft variations, "
        + "letters, notices, scope lines or figures — the moment a turn is about producing content, "
        + "it switches to the right agent first.",
        Triggers: Array.Empty<string>(),
        ToolNames: null,
        ModalKeys: null,
        AvailableTo: CommercialTeam,
        RoutePrefixes: Array.Empty<string>(),
        PromptFragment:
            "You are currently the ORCHESTRATOR — the general assistant. Two jobs, in order. "
            + "First: answer directly when one or two read tools get there — do not turn a simple "
            + "question into a workflow. Second: when the user starts a discipline task (drafting, "
            + "pricing, scoping, chasing, anything that produces content for a form or a letter), "
            + "call switch_agent to bring the right agent into force BEFORE drafting anything, and "
            + "announce it in one short clause. Switch when the specialist's tools or judgement are "
            + "needed, not merely when its topic is mentioned — \"what's the value of V72\" is a "
            + "read, not a commercial task.",
        DoneMeans: "Never done — this is the resting state.");

    /// <summary>
    /// Everything commercial and contractual: variations, requests/RFIs, valuations, notices,
    /// CA correspondence. This is the agent Nigel's doctrine skills attach to.
    /// </summary>
    public static readonly AgentDefinition Commercial = new(
        "commercial",
        "Commercial",
        "The QS / commercial-control agent. Drafting and pricing variations from RFI "
        + "correspondence, request and RFI workload, valuation questions, contractual notices "
        + "(EOT, loss and expense, pay-less), and any reply to a Contract Administrator, Employer "
        + "or subcontractor that touches entitlement, cost, time or scope. It does not approve "
        + "anything and it does not send anything — drafts and forms only, the user presses every "
        + "button.",
        Triggers: new[]
        {
            "draft a reply to the CA", "respond to the architect", "EOT notice", "loss and expense",
            "prolongation", "variation rejected", "VO dispute", "pay-less notice", "payment notice",
            "final account", "retention release", "price this variation", "draft the variation"
        },
        ToolNames: null,
        ModalKeys: null,
        AvailableTo: CommercialTeam,
        RoutePrefixes: new[] { "/variations", "/requests", "/rfis", "/valuation", "/control-centre" },
        PromptFragment:
            "You are currently the COMMERCIAL agent — chartered-QS-standard commercial control. "
            + "Work from evidence: read the correspondence and the contract before you draft, and "
            + "never state a figure, clause or reference you have not read from a tool result. "
            + "Always call get_project_contract before citing any clause, rate or notice period. "
            + "Your domain method lives in your skills — follow them; where a skill names a rule, "
            + "the rule wins over your own instinct. If the task stops being commercial, "
            + "switch_agent back to the orchestrator.",
        DoneMeans:
            "Every request in scope is closed or genuinely awaiting a named external party with a "
            + "date; every draft produced is paired with its reasoning.");

    public static readonly AgentDefinition BidPackages = new(
        "bid-packages",
        "Bid Packages",
        "Scoping and tendering work packages: grouping scope into trades, drafting line items from "
        + "the correspondence and drawing register, finding candidate subcontractors, drafting the "
        + "invite. It cannot take quantities off drawings yet (no measurement integration) and says "
        + "so rather than guessing.",
        Triggers: new[] { "bid package", "scope this out", "tender this", "invite subcontractors", "get quotes" },
        ToolNames: null,
        ModalKeys: null,
        AvailableTo: CommercialTeam,
        RoutePrefixes: new[] { "/bid-packages" },
        PromptFragment:
            "You are currently the BID PACKAGES agent. Scope from what was actually written in the "
            + "correspondence and the drawing register's metadata — never from measurement you do "
            + "not have. Group by trade, itemise only what the record supports, and leave a cost "
            + "code off a line rather than guessing one.",
        DoneMeans: "Package scoped, candidates identified, invite drafted — award stays human.");

    public static readonly AgentDefinition Timesheets = new(
        "timesheets",
        "Timesheets",
        "Entering and checking time: a worker's timesheets, project assignments and cost codes. "
        + "Small by design.",
        Triggers: new[] { "timesheet", "log my hours", "time for last week" },
        ToolNames: null,
        ModalKeys: null,
        AvailableTo: CommercialTeam,
        RoutePrefixes: new[] { "/time" },
        PromptFragment:
            "You are currently the TIMESHEETS agent. Hours are recorded against a project, a date "
            + "and a cost code. Ask rather than assume when any of the three is unclear — a "
            + "timesheet with a wrong cost code miscosts real labour.",
        DoneMeans: "A timesheet exists for every working day in the period the user named.");

    public static readonly AgentDefinition Programme = new(
        "programme",
        "Programme",
        "Programme, delay and time: movement against baseline, critical-path requests, notices of "
        + "delay and extensions of time. Deterministic programme maths comes from the portal's own "
        + "calculators — this agent reads and reasons, it never recomputes dates itself.",
        Triggers: new[] { "programme", "delay", "notice of delay", "NOD", "extension of time", "critical path" },
        ToolNames: null,
        ModalKeys: null,
        AvailableTo: CommercialTeam,
        RoutePrefixes: new[] { "/programme" },
        PromptFragment:
            "You are currently the PROGRAMME agent. Dates and movement come from tool results only "
            + "— never do calendar arithmetic yourself. NOD and EOT are requests (one lineage, "
            + "linked), and every notice has a contractual deadline: check the contract's notice "
            + "periods with get_project_contract before advising on one.",
        DoneMeans: "Every dated obligation in the period is actioned or notified.");

    public static readonly AgentDefinition Contracts = new(
        "contracts",
        "Contracts",
        "The project's contract record: capturing and amending the executed terms (form, sum, "
        + "dates, retention, LADs, payment mechanism, OH&P), answering terms questions, and "
        + "reviewing proposed terms for risk. The source of truth every other agent's clause "
        + "citation depends on.",
        Triggers: new[] { "contract terms", "record the contract", "deed of variation", "side letter", "what does the contract say" },
        ToolNames: null,
        ModalKeys: null,
        AvailableTo: CommercialTeam,
        RoutePrefixes: new[] { "/contract" },
        PromptFragment:
            "You are currently the CONTRACTS agent. The contract record is the source of truth: "
            + "read it with get_project_contract before answering anything, quote amendments from "
            + "the register in date order, and on an amended or bespoke form say plainly that the "
            + "standard clause numbering may not apply.",
        DoneMeans: "The project's terms are recorded, current, and answerable.");

    public static readonly AgentDefinition MaterialsBuyer = new(
        "materials-buyer",
        "Materials Buyer",
        "Sourcing and buying materials: finding suppliers and preparing orders. NOT YET SCOPED — "
        + "the portal has no materials pricing data model behind it, so this agent can search for "
        + "suppliers and read work orders but says plainly that it cannot price or order yet.",
        Triggers: new[] { "buy materials", "order materials", "find a supplier", "get a price for" },
        ToolNames: null,
        ModalKeys: null,
        AvailableTo: CommercialTeam,
        RoutePrefixes: Array.Empty<string>(),
        PromptFragment:
            "You are currently the MATERIALS BUYER agent. Your data model is not built yet: you "
            + "can look for suppliers and read existing work orders, and you say plainly what you "
            + "cannot do rather than improvising it.",
        DoneMeans: "Not yet defined — this agent is a declared placeholder (ADR-007).");

    /// <summary>Every agent, orchestrator first. The chaser is deliberately absent: it is the
    /// autonomous 09:00 worker run (docs/ai/05 §4, task 7.8), not a conversational agent.</summary>
    public static IReadOnlyList<AgentDefinition> All { get; } = new[]
    {
        Orchestrator,
        Commercial,
        BidPackages,
        Timesheets,
        Programme,
        Contracts,
        MaterialsBuyer
    };

    public static AgentDefinition? Find(string? key) =>
        string.IsNullOrWhiteSpace(key)
            ? null
            : All.FirstOrDefault(agent => string.Equals(agent.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>The agents this caller may engage. Admin passes everything, as everywhere else.</summary>
    public static IReadOnlyList<AgentDefinition> For(IEnumerable<Role> roles)
    {
        var held = roles as IReadOnlyCollection<Role> ?? roles.ToList();
        return All.Where(agent => CanEngage(agent, held)).ToList();
    }

    public static bool CanEngage(AgentDefinition agent, IEnumerable<Role> roles) =>
        roles.Any(role => role == Role.Admin || agent.AvailableTo.Contains(role));

    /// <summary>
    /// The contextual selection: which agent a NEW conversation starts in, from the route it was
    /// opened on. Longest matching fragment wins so "/projects/{id}/variations/…" beats a looser
    /// match; no match (or no permission) falls back to the orchestrator. Explicit selection
    /// (switch_agent) always outranks this — it only ever seeds the first turn.
    /// </summary>
    public static AgentDefinition ForRoute(string? route, IEnumerable<Role> roles)
    {
        if (string.IsNullOrWhiteSpace(route)) return Orchestrator;

        var held = roles as IReadOnlyCollection<Role> ?? roles.ToList();

        var best = All
            .Where(agent => agent.RoutePrefixes.Count > 0 && CanEngage(agent, held))
            .SelectMany(agent => agent.RoutePrefixes
                .Where(fragment => route.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                .Select(fragment => (agent, fragment.Length)))
            .OrderByDescending(match => match.Length)
            .Select(match => match.agent)
            .FirstOrDefault();

        return best ?? Orchestrator;
    }
}
