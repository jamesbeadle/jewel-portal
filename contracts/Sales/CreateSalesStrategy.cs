using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>
/// Writes down a new way of finding leads: who it targets, where, why (the hypothesis and the
/// evidence behind it), how they are reached and what they are told. Starts as a Draft with no
/// approach plan; GenerateStrategyApproachPlan drafts one from these fields. OwnerEmail is the
/// portal email of whoever is running it — the signed-in user unless another is named.
/// </summary>
public sealed record CreateSalesStrategy(
    string Name,
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
