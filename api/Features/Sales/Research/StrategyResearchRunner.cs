using Microsoft.EntityFrameworkCore;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Sales.Commands;

namespace Jewel.JPMS.Api.Features.Sales.Research;

/// <summary>
/// The whole research run, consumed from the queue by the worker (api-shared source): mark the
/// row Running; research (web search → proposed fields + findings); write the findings and fill
/// every definition field the team left blank; draft the approach plan from the lot; mark
/// Complete — or Failed with the reason, which the strategy page shows. Each attempt that fails
/// stamps the row, so a re-delivered message never hides what went wrong; the runner does NOT
/// rethrow, because a research call is not idempotent-cheap — one honest failure beats five.
/// </summary>
public sealed class StrategyResearchRunner
{
    private readonly JpmsContext context;
    private readonly StrategyResearcher researcher;
    private readonly IClaudeClient claude;
    private readonly AnthropicOptions options;
    private readonly ILogger<StrategyResearchRunner> logger;

    public StrategyResearchRunner(
        JpmsContext context, StrategyResearcher researcher, IClaudeClient claude,
        AnthropicOptions options, ILogger<StrategyResearchRunner> logger)
    {
        this.context = context;
        this.researcher = researcher;
        this.claude = claude;
        this.options = options;
        this.logger = logger;
    }

    public async Task RunAsync(StrategyResearchMessage message, CancellationToken ct)
    {
        var entity = await context.SalesStrategies.FirstOrDefaultAsync(row => row.StrategyId == message.StrategyId, ct);
        if (entity is null)
        {
            logger.LogWarning("Strategy research: strategy {StrategyId} not found — message dropped.", message.StrategyId);
            return;
        }

        // host.json's visibilityTimeout re-delivers a message whose first run is still going (a
        // long research can outlast it): a row already Running on a recent request is that first
        // run — leave it alone rather than start a second, paid, run alongside it.
        if (entity.ResearchStatus == (int)StrategyResearchStatus.Running
            && entity.ResearchRequestedAt is { } startedAt && startedAt > DateTimeOffset.UtcNow.AddMinutes(-20))
        {
            logger.LogInformation("Strategy research: {StrategyId} is already running — duplicate delivery ignored.", entity.StrategyId);
            return;
        }

        entity.ResearchStatus = (int)StrategyResearchStatus.Running;
        entity.ResearchError = null;
        await context.SaveChangesAsync(ct);

        try
        {
            var result = await researcher.ResearchAsync(entity.ToModel(), ct);

            // The team's own words win; the research fills only the gaps. Its proposals for the
            // fields they did write are still in the findings.
            if (string.IsNullOrWhiteSpace(entity.TargetArea)) entity.TargetArea = result.TargetArea;
            if (string.IsNullOrWhiteSpace(entity.Hypothesis)) entity.Hypothesis = result.Hypothesis;
            if (string.IsNullOrWhiteSpace(entity.Evidence)) entity.Evidence = result.Evidence;
            if (string.IsNullOrWhiteSpace(entity.Proposition)) entity.Proposition = result.Proposition;
            entity.ResearchFindings = result.Findings;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(ct);

            // Then the plan, from everything now on the record. A plan failure is not a research
            // failure — the findings are saved either way; the page's Generate button retries.
            var plan = await claude.CompleteAsync(
                StrategyPlanPrompt.System,
                StrategyPlanPrompt.User(entity.ToModel(), null),
                ct,
                modelOverride: options.ModelForTier("sonnet"),
                maxTokensOverride: 3000);
            if (!string.IsNullOrWhiteSpace(plan))
            {
                entity.ApproachPlan = plan.Trim();
                entity.PlanGeneratedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                logger.LogWarning("Strategy research: plan draft returned nothing for {StrategyId}; findings saved.", entity.StrategyId);
            }

            entity.ResearchStatus = (int)StrategyResearchStatus.Complete;
            entity.ResearchCompletedAt = DateTimeOffset.UtcNow;
            entity.UpdatedAt = entity.ResearchCompletedAt.Value;
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Strategy research failed for {StrategyId}.", entity.StrategyId);
            entity.ResearchStatus = (int)StrategyResearchStatus.Failed;
            entity.ResearchError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            entity.ResearchCompletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(CancellationToken.None);
        }
    }
}
