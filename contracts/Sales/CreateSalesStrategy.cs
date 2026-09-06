using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>
/// Writes down a new way of finding leads. The Brief — the idea in the team's own words — plus
/// an audience and a channel is enough; where, the hypothesis, the evidence and the proposition
/// can be left blank for RunStrategyResearch to fill in. Starts as a Draft with no approach
/// plan; GenerateStrategyApproachPlan (or the research run) drafts one. OwnerEmail is the
/// portal email of whoever is running it — the signed-in user unless another is named.
/// </summary>
public sealed record CreateSalesStrategy(
    string Name,
    string Brief,
    SalesAudience Audience,
    string TargetArea,
    string Hypothesis,
    string Evidence,
    SalesChannel Channel,
    string Proposition,
    string OwnerEmail) : ICommand<SalesStrategy>;

/// <summary>Rewrites a strategy's definition and its approach plan; the whole record is applied
/// as supplied. Status is separate (SetSalesStrategyStatus).</summary>
public sealed record UpdateSalesStrategy(
    string StrategyId,
    string Name,
    string Brief,
    SalesAudience Audience,
    string TargetArea,
    string Hypothesis,
    string Evidence,
    SalesChannel Channel,
    string Proposition,
    string ApproachPlan,
    string OwnerEmail) : ICommand<SalesStrategy>;

/// <summary>Draft → Active → Paused / Retired — and back; any move is allowed, the status is a
/// statement about the strategy, not a workflow.</summary>
public sealed record SetSalesStrategyStatus(
    string StrategyId,
    SalesStrategyStatus Status) : ICommand<SalesStrategy>;

/// <summary>
/// Asks Claude to draft the strategy's approach plan from its definition — audience, area,
/// hypothesis, evidence, channel and proposition — as a markdown plan: who exactly to approach,
/// what to say and why it is credible, the steps and their order, what to measure, and what
/// would show the hypothesis is wrong. Guidance is an optional steer ("keep it to post and one
/// follow-up call", "assume a £50k budget"). The plan replaces the current one; it stays
/// editable. Refused when no Anthropic key is configured.
/// </summary>
public sealed record GenerateStrategyApproachPlan(
    string StrategyId,
    string? Guidance) : ICommand<SalesStrategy>;

/// <summary>
/// Sends the strategy to the worker for AI research: Claude reads the brief and whatever else is
/// written, searches the web (house-price trends, planning, infrastructure, practices, whatever
/// the brief calls for), fills in the blank definition fields — where, hypothesis, evidence,
/// proposition — writes its findings with sources into ResearchFindings, and drafts the approach
/// plan. Fields already written by hand are kept. Takes a few minutes; the strategy's
/// ResearchStatus goes Queued → Running → Complete / Failed and the page polls it. Refused while
/// a run is already queued or running, or when the queue / Anthropic key is not configured.
/// </summary>
public sealed record RunStrategyResearch(string StrategyId) : ICommand<SalesStrategy>;
