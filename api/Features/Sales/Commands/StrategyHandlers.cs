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
            Brief = command.Brief.Trim(),
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
        entity.Brief = command.Brief.Trim();
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
