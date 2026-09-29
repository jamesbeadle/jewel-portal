using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;

/// <summary>
/// Asks Claude to rewrite the report's selected days in house language and flag what the report
/// must not say, and stores the answer on the report with every flag open. The raw notes are
/// never touched. Unconfigured, failed or unusable is a refusal the page shows, never a silent
/// report with nothing rewritten — the person asked for a rewrite and must know they have none.
/// </summary>
public sealed class RewriteContractorsReportWeekHandler : ICommandHandler<RewriteContractorsReportWeek, ContractorsReport>
{
    private const string SonnetTier = "sonnet";
    private const int ResponseTokenBudget = 6000;

    private readonly JpmsContext context;
    private readonly ContractorsReportComposer composer;
    private readonly IClaudeClient claude;
    private readonly AnthropicOptions options;
    private readonly ILogger<RewriteContractorsReportWeekHandler> logger;

    public RewriteContractorsReportWeekHandler(
        JpmsContext context, ContractorsReportComposer composer, IClaudeClient claude, AnthropicOptions options,
        ILogger<RewriteContractorsReportWeekHandler> logger)
    {
        this.context = context; this.composer = composer; this.claude = claude; this.options = options; this.logger = logger;
    }

    public async Task<ContractorsReport> HandleAsync(RewriteContractorsReportWeek command, CancellationToken cancellationToken)
    {
        var entity = await context.ContractorsReports.FirstOrDefaultAsync(row => row.ContractorsReportId == command.ContractorsReportId, cancellationToken)
            ?? throw new InvalidOperationException("Contractor's Report not found.");
        if (!claude.IsConfigured)
            throw new InvalidOperationException("The AI isn't connected (no Anthropic key is configured), so the week cannot be rewritten here.");
        var raw = await composer.RawWeekAsync(entity.ContractorsReportId, cancellationToken)
            ?? throw new InvalidOperationException("Contractor's Report not found.");
        if (raw.Days.Count == 0)
            throw new InvalidOperationException("No day in the period has a selected update — there is nothing to rewrite.");

        var requestedAt = DateTimeOffset.UtcNow;
        var response = await claude.CompleteAsync(
            ContractorsReportRewritePrompt.System, ContractorsReportRewritePrompt.User(raw.Header, raw.Days), cancellationToken,
            modelOverride: options.ModelForTier(SonnetTier), maxTokensOverride: ResponseTokenBudget);
        var rewrite = ContractorsReportRewritePrompt.Parse(response, raw.Week, requestedAt);
        if (rewrite is null)
        {
            logger.LogWarning("Contractor's Report {ContractorsReportId}: Claude's rewrite was empty or unusable.", entity.ContractorsReportId);
            throw new InvalidOperationException("Claude didn't give a usable rewrite just now — try again, or write the week by hand.");
        }

        entity.RewriteJson = ContractorsReportJson.WriteOne(rewrite);
        entity.UpdatedAt = requestedAt;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
