using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

public sealed class CreateSalesStrategyHandler : ICommandHandler<CreateSalesStrategy, SalesStrategy>
{
    private readonly JpmsContext context;
    public CreateSalesStrategyHandler(JpmsContext context) { this.context = context; }

    public async Task<SalesStrategy> HandleAsync(CreateSalesStrategy command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new SalesStrategyEntity
        {
            StrategyId = Guid.NewGuid().ToString("N"),
            Name = command.Name.Trim(),
            Audience = (int)command.Audience,
            TargetArea = command.TargetArea.Trim(),
            Hypothesis = command.Hypothesis.Trim(),
            Evidence = command.Evidence.Trim(),
            Channel = (int)command.Channel,
            Proposition = command.Proposition.Trim(),
            ApproachPlan = "",
            Status = (int)SalesStrategyStatus.Draft,
            OwnerEmail = command.OwnerEmail.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        context.SalesStrategies.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public sealed class UpdateSalesStrategyHandler : ICommandHandler<UpdateSalesStrategy, SalesStrategy>
{
    private readonly JpmsContext context;
    public UpdateSalesStrategyHandler(JpmsContext context) { this.context = context; }

    public async Task<SalesStrategy> HandleAsync(UpdateSalesStrategy command, CancellationToken cancellationToken)
    {
        var entity = await context.SalesStrategies.FirstOrDefaultAsync(row => row.StrategyId == command.StrategyId, cancellationToken)
            ?? throw new InvalidOperationException($"Strategy {command.StrategyId} not found.");
        entity.Name = command.Name.Trim();
        entity.Audience = (int)command.Audience;
        entity.TargetArea = command.TargetArea.Trim();
        entity.Hypothesis = command.Hypothesis.Trim();
        entity.Evidence = command.Evidence.Trim();
        entity.Channel = (int)command.Channel;
        entity.Proposition = command.Proposition.Trim();
        entity.ApproachPlan = command.ApproachPlan.Trim();
        entity.OwnerEmail = command.OwnerEmail.Trim();
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public sealed class SetSalesStrategyStatusHandler : ICommandHandler<SetSalesStrategyStatus, SalesStrategy>
{
    private readonly JpmsContext context;
    public SetSalesStrategyStatusHandler(JpmsContext context) { this.context = context; }

    public async Task<SalesStrategy> HandleAsync(SetSalesStrategyStatus command, CancellationToken cancellationToken)
    {
        var entity = await context.SalesStrategies.FirstOrDefaultAsync(row => row.StrategyId == command.StrategyId, cancellationToken)
            ?? throw new InvalidOperationException($"Strategy {command.StrategyId} not found.");
        entity.Status = (int)command.Status;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

/// <summary>
/// Drafts the approach plan with Claude from the strategy's own definition — one call, sized to
/// finish inside the Static Web Apps gateway's ~45s ceiling (no web research: that is Phase 2's
/// "Run research", which will feed the Evidence field). The plan replaces the current one and
/// stays editable on the strategy page.
/// </summary>
public sealed class GenerateStrategyApproachPlanHandler : ICommandHandler<GenerateStrategyApproachPlan, SalesStrategy>
{
    private readonly JpmsContext context;
    private readonly IClaudeClient claude;
    private readonly AnthropicOptions options;

    public GenerateStrategyApproachPlanHandler(JpmsContext context, IClaudeClient claude, AnthropicOptions options)
    {
        this.context = context;
        this.claude = claude;
        this.options = options;
    }

    public async Task<SalesStrategy> HandleAsync(GenerateStrategyApproachPlan command, CancellationToken cancellationToken)
    {
        var entity = await context.SalesStrategies.FirstOrDefaultAsync(row => row.StrategyId == command.StrategyId, cancellationToken)
            ?? throw new InvalidOperationException($"Strategy {command.StrategyId} not found.");
        if (!claude.IsConfigured)
            throw new InvalidOperationException("The approach plan can't be drafted: no Anthropic API key is configured on the API.");

        var plan = await claude.CompleteAsync(
            StrategyPlanPrompt.System,
            StrategyPlanPrompt.User(entity.ToModel(), command.Guidance),
            cancellationToken,
            modelOverride: options.ModelForTier("sonnet"),
            maxTokensOverride: 2500);
        if (string.IsNullOrWhiteSpace(plan))
            throw new InvalidOperationException("Claude didn't return a plan just now — try again in a moment.");

        entity.ApproachPlan = plan.Trim();
        entity.PlanGeneratedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = entity.PlanGeneratedAt.Value;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

internal static class StrategyPlanPrompt
{
    public const string System =
        "You are the sales strategist for Jewel Bespoke Build, a high-end residential builder in the UK: "
        + "bespoke new homes and substantial upgrades (extensions, refurbishments, whole-house remodels) for "
        + "private clients, usually working with an architect. You write approach plans for finding leads. "
        + "A plan is a working document the team will follow, not marketing copy. Write in British English, "
        + "plainly, in markdown with short headed sections. Be concrete: name the exact people to approach "
        + "and how to find them, what to say and why it is credible to THEM, the steps in order with rough "
        + "timing, what to measure at each step, and the evidence that would show the hypothesis is wrong "
        + "so the strategy can be stopped early. Where the strategy rests on data (house prices, planning "
        + "applications, infrastructure, company records) say which public sources to pull and what to look "
        + "for in them — do not invent figures. Where the audience is architects, sell what Jewel's project "
        + "portal does for their job (drawings tracked to revision, RFIs and variations with one number "
        + "through every stage, a programme everyone can see, correspondence filed to the record) and the "
        + "errors and chasing it removes. Keep it under 900 words. No preamble, no sign-off — start with the "
        + "first heading.";

    public static string User(SalesStrategy strategy, string? guidance)
    {
        var lines = new List<string>
        {
            $"Strategy: {strategy.Name}",
            $"Audience: {strategy.Audience.DisplayName()}",
            $"Target area: {(string.IsNullOrWhiteSpace(strategy.TargetArea) ? "(not given)" : strategy.TargetArea)}",
            $"Channel: {strategy.Channel.DisplayName()}",
            "",
            "Hypothesis — why these people, why now:",
            string.IsNullOrWhiteSpace(strategy.Hypothesis) ? "(not written yet — propose one and say it is a proposal)" : strategy.Hypothesis,
            "",
            "Evidence and data sources so far:",
            string.IsNullOrWhiteSpace(strategy.Evidence) ? "(none recorded — say what should be gathered first)" : strategy.Evidence,
            "",
            "Proposition — what we would say to them:",
            string.IsNullOrWhiteSpace(strategy.Proposition) ? "(not written yet — draft one)" : strategy.Proposition
        };
        if (!string.IsNullOrWhiteSpace(guidance))
        {
            lines.Add("");
            lines.Add("Steer from the team:");
            lines.Add(guidance.Trim());
        }
        if (!string.IsNullOrWhiteSpace(strategy.ApproachPlan))
        {
            lines.Add("");
            lines.Add("There is an existing plan; write a fresh one rather than editing it, keeping anything in it that is clearly a decision already made:");
            lines.Add(strategy.ApproachPlan.Length > 6000 ? strategy.ApproachPlan[..6000] : strategy.ApproachPlan);
        }
        lines.Add("");
        lines.Add("Write the approach plan.");
        return string.Join("\n", lines);
    }
}
