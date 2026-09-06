using Jewel.JPMS.Api.Features.Sales;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// The Sales section over the connector (2026-09-06): the lead register and the strategies that
/// feed it. Mirrors Features/Sales — each entry's VisibleTo is the same SalesRoles set its
/// Authorisation class checks, and the stamps copy exactly what SalesLeadEndpoints /
/// SalesStrategyEndpoints stamp server-side. Reads: list_leads, get_lead, list_sales_strategies,
/// get_sales_strategy (AiSalesTools).
/// </summary>
internal sealed class SalesActions : IAiActionSource
{
    private const string Area = "Sales";

    private const string LeadFieldNotes =
        "Fields: contactName, contactEmail, contactPhone, companyName, prospectKind (Homeowner, "
        + "Architect, Developer, Landowner, Business, Other), propertyAddress and postcode (the "
        + "property or site the work would be on), summary (one line on the possible work), notes, "
        + "source (Strategy, Inbound, Referral, Architect, RepeatClient, Manual), strategyId (the "
        + "strategy that found the lead — get it from list_sales_strategies; giving one sets "
        + "source to Strategy), estimatedValue (£, optional), ownerEmail (the portal email of the "
        + "staff member working it — the signed-in user unless they say otherwise).";

    public IEnumerable<AiAction> Build() => new AiAction[]
    {
        new AiAction(
            Name: "capture_lead",
            Area: Area,
            Description: "Adds a lead to the Sales register — a person we might convince to build "
                + "with Jewel and the property or site the work would be on. Every lead lands in "
                + "the one register whatever found it; a lead found by a sales strategy carries "
                + "that strategyId so the strategy's funnel counts it. The LD-#### reference is "
                + "minted server-side. Stage starts at New unless a warmer one is given (an "
                + "inbound enquiry that is already talking to us is Engaged); never Won.",
            CommandType: typeof(CaptureLead),
            ResultType: typeof(Lead),
            AuthorisationType: typeof(CaptureLeadAuthorisation),
            ValidationType: typeof(CaptureLeadValidation),
            VisibleTo: SalesRoles.SalesTeam,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: LeadFieldNotes + " Stage: New, Contacted, Engaged, SiteVisit, Proposal, Nurture. "
                + "Read list_leads first so the same person is not captured twice."),

        new AiAction(
            Name: "update_lead",
            Area: Area,
            Description: "Rewrites a lead's details — who, where, what, how much, who owns it and "
                + "which strategy found it. Sends the whole record: every field is applied as "
                + "supplied. Not the stage (move_lead_stage / win_lead).",
            CommandType: typeof(UpdateLead),
            ResultType: typeof(Lead),
            AuthorisationType: typeof(UpdateLeadAuthorisation),
            ValidationType: typeof(UpdateLeadValidation),
            VisibleTo: SalesRoles.SalesTeam,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Read the lead (get_lead) first and carry forward every field that should not "
                + "change. " + LeadFieldNotes),

        new AiAction(
            Name: "move_lead_stage",
            Area: Area,
            Description: "Moves a lead along the ladder (New → Contacted → Engaged → SiteVisit → "
                + "Proposal) or back down it, parks it in Nurture, or closes it as Lost with a "
                + "reason. Writes a stage-change entry on the lead's timeline with the note if "
                + "given. Won is win_lead — it creates the client and the project. Reopening a "
                + "Lost or Nurture lead (any open stage) clears the lost reason.",
            CommandType: typeof(MoveLeadStage),
            ResultType: typeof(Lead),
            AuthorisationType: typeof(MoveLeadStageAuthorisation),
            ValidationType: typeof(MoveLeadStageValidation),
            VisibleTo: SalesRoles.SalesTeam,
            EmailStamps: new[] { "ChangedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Open stages are the sales team's call; Lost and Nurture are the directors' "
                + "(the gate refuses anyone else). lostReason is required for Lost. A Won lead "
                + "cannot be moved.",
            RequiresConfirmation: true),

        new AiAction(
            Name: "win_lead",
            Area: Area,
            Description: "The lead has chosen Jewel: creates the Client account (the lead's company, "
                + "else the contact's name, with the contact as primary contact) AND the project "
                + "shell (projectReference e.g. JBB-2026-014, projectName; the lead's owner as "
                + "project manager unless projectManagerEmail names another), links both to the "
                + "lead and moves it to Won. Directors only. Runs once — a lead already Won is "
                + "refused, as is a project reference already in use.",
            CommandType: typeof(WinLead),
            ResultType: typeof(LeadWonOutcome),
            AuthorisationType: typeof(WinLeadAuthorisation),
            ValidationType: typeof(WinLeadValidation),
            VisibleTo: SalesRoles.Deciders,
            EmailStamps: new[] { "DecidedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm the project reference and name with the user before running — they "
                + "become the project's identity everywhere (list_projects shows the reference "
                + "pattern in use). The result carries clientId and projectId.",
            RequiresConfirmation: true),

        new AiAction(
            Name: "log_lead_activity",
            Area: Area,
            Description: "Records a touch on a lead — a call, an email, a letter or brochure "
                + "posted, a meeting, a site visit, a proposal sent, or a note — on its timeline. "
                + "Does not move the stage: that is a decision (move_lead_stage).",
            CommandType: typeof(LogLeadActivity),
            ResultType: typeof(LeadActivity),
            AuthorisationType: typeof(LogLeadActivityAuthorisation),
            ValidationType: typeof(LogLeadActivityValidation),
            VisibleTo: SalesRoles.SalesTeam,
            EmailStamps: new[] { "RecordedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "kind: Note, Call, Email, Meeting, SiteVisit, Proposal, Letter. occurredAt is "
                + "ISO 8601 and defaults to now."),

        new AiAction(
            Name: "create_sales_strategy",
            Area: Area,
            Description: "Writes down a new way of finding leads — a sales strategy with its "
                + "justification: name, audience (Homeowners, Architects, Developers, Landowners, "
                + "Referrers, PastClients, Other), targetArea (postcodes / towns), hypothesis "
                + "(why these people, why now), evidence (the data and findings behind it), "
                + "channel (DirectMail, Email, Phone, InPerson, LinkedIn, SocialMedia, Events, "
                + "Partnerships, Website, Mixed) and proposition (what we would say to them). "
                + "Starts as a Draft with no approach plan — generate_strategy_plan drafts one.",
            CommandType: typeof(CreateSalesStrategy),
            ResultType: typeof(SalesStrategy),
            AuthorisationType: typeof(CreateSalesStrategyAuthorisation),
            ValidationType: typeof(CreateSalesStrategyValidation),
            VisibleTo: SalesRoles.SalesTeam,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "ownerEmail is the portal email of whoever runs the strategy — the signed-in "
                + "user unless they say otherwise. Write the hypothesis and evidence in the "
                + "user's words; the plan generator builds on them."),

        new AiAction(
            Name: "update_sales_strategy",
            Area: Area,
            Description: "Rewrites a strategy's definition and its approach plan (markdown). Sends "
                + "the whole record: every field is applied as supplied. Status is "
                + "set_sales_strategy_status.",
            CommandType: typeof(UpdateSalesStrategy),
            ResultType: typeof(SalesStrategy),
            AuthorisationType: typeof(UpdateSalesStrategyAuthorisation),
            ValidationType: typeof(UpdateSalesStrategyValidation),
            VisibleTo: SalesRoles.SalesTeam,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Read the strategy (get_sales_strategy) first and carry forward every field "
                + "that should not change — including approachPlan, which is otherwise blanked."),

        new AiAction(
            Name: "set_sales_strategy_status",
            Area: Area,
            Description: "Sets a strategy's status — Draft, Active (leads are being found under "
                + "it), Paused, Retired (judged and stopped). Directors only. Any move is allowed; "
                + "a retired strategy keeps its leads.",
            CommandType: typeof(SetSalesStrategyStatus),
            ResultType: typeof(SalesStrategy),
            AuthorisationType: typeof(SetSalesStrategyStatusAuthorisation),
            ValidationType: typeof(SetSalesStrategyStatusValidation),
            VisibleTo: SalesRoles.Deciders,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: null,
            RequiresConfirmation: true),

        new AiAction(
            Name: "generate_strategy_plan",
            Area: Area,
            Description: "Asks Claude to draft the strategy's approach plan from its own definition "
                + "— audience, area, hypothesis, evidence, channel, proposition — as markdown: who "
                + "exactly to approach and how to find them, what to say and why it is credible, "
                + "the steps in order, what to measure, and what would show the hypothesis is "
                + "wrong. Replaces the current plan (it stays editable). guidance is an optional "
                + "steer. Takes up to ~30 seconds.",
            CommandType: typeof(GenerateStrategyApproachPlan),
            ResultType: typeof(SalesStrategy),
            AuthorisationType: typeof(GenerateStrategyApproachPlanAuthorisation),
            ValidationType: typeof(GenerateStrategyApproachPlanValidation),
            VisibleTo: SalesRoles.SalesTeam,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "If the user has already written a plan they like, confirm before replacing "
                + "it. The plan uses only what the strategy record says — it does not search the "
                + "web; put research findings in evidence first.",
            RequiresConfirmation: true)
    };
}
