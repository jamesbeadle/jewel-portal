using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// Writes the agent activity log. One call per agent run.
///
/// <para>Failures here are swallowed and logged, never thrown — the same contract as
/// <c>Audit.AuditTrail</c>. A log write must not be able to fail the work it is recording, and an
/// agent run that succeeded but could not be logged is better than one that was rolled back because
/// its log row would not save.</para>
///
/// <para>Call it <em>after</em> the run has finished, so the outcome is known.</para>
/// </summary>
public sealed class AgentActivityLog
{
    /// <summary>The system pseudo-user a scheduled agent runs as. Matches the mailbox worker's
    /// existing convention (<c>MailboxActionWorker</c> stamps the projects mailbox on its audit
    /// rows) so autonomous work is attributable to something rather than blank.</summary>
    public const string SystemActor = "system@jewelbb.co.uk";

    private readonly JpmsContext context;
    private readonly AnthropicOptions options;
    private readonly ILogger<AgentActivityLog> logger;

    public AgentActivityLog(JpmsContext context, AnthropicOptions options, ILogger<AgentActivityLog> logger)
    {
        this.context = context;
        this.options = options;
        this.logger = logger;
    }

    public async Task WriteAsync(
        string agentKey,
        AgentTrigger trigger,
        string actorEmail,
        string action,
        AgentOutcome outcome,
        string summary,
        CancellationToken cancellationToken,
        string? conversationId = null,
        string? projectId = null,
        string? recordReference = null,
        string? route = null,
        IEnumerable<string>? toolsUsed = null,
        int durationMs = 0,
        int inputTokens = 0,
        int outputTokens = 0,
        int cacheWriteTokens = 0,
        int cacheReadTokens = 0)
    {
        try
        {
            var entity = new AgentActivityEntity
            {
                ActivityId = Guid.NewGuid().ToString("N"),
                AgentKey = agentKey,
                Trigger = (int)trigger,
                ActorEmail = string.IsNullOrWhiteSpace(actorEmail) ? SystemActor : actorEmail,
                IsAutonomous = trigger is AgentTrigger.Schedule or AgentTrigger.Queue,
                Action = ClampRequired(action, 128),
                Outcome = (int)outcome,
                Summary = ClampRequired(summary, 1024),
                ConversationId = conversationId,
                ProjectId = projectId,
                RecordReference = Clamp(recordReference, 64),
                Route = Clamp(route, 512),
                ToolsUsed = toolsUsed is null ? null : Clamp(string.Join(", ", toolsUsed), 512),
                DurationMs = durationMs,
                // The stored InputTokens folds the cache figures back in, so the column is the
                // real total input processed (uncached + written + read) rather than the sliver
                // Anthropic labels input_tokens; CostPence prices each slice at its own rate.
                InputTokens = inputTokens + cacheWriteTokens + cacheReadTokens,
                OutputTokens = outputTokens,
                CostPence = options.CostPence(inputTokens, outputTokens, cacheWriteTokens, cacheReadTokens),
                OccurredAt = DateTimeOffset.UtcNow
            };

            context.AgentActivity.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not write the agent activity row for {Agent}/{Action}.", agentKey, action);
        }
    }

    private static string? Clamp(string? value, int length) =>
        string.IsNullOrWhiteSpace(value) ? null
        : value.Length <= length ? value
        : value[..length];

    // Distinct name, not an overload: nullable annotations are not part of a method
    // signature, so Clamp(string?, int) and Clamp(string, int) collide (CS0111).
    private static string ClampRequired(string value, int length) =>
        value.Length <= length ? value : value[..length];
}
