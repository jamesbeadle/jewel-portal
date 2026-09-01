using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Ai;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai.Queries;

public sealed class ListAgentActivityHandler
    : IQueryHandler<ListAgentActivity, IReadOnlyList<AgentActivity>>
{
    private const int MaxTake = 500;

    private readonly JpmsContext context;

    public ListAgentActivityHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<AgentActivity>> HandleAsync(
        ListAgentActivity query, CancellationToken cancellationToken)
    {
        var rows = context.AgentActivity.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.ProjectId))
            rows = rows.Where(row => row.ProjectId == query.ProjectId);

        if (!string.IsNullOrWhiteSpace(query.AgentKey))
            rows = rows.Where(row => row.AgentKey == query.AgentKey);

        if (query.AutonomousOnly == true)
            rows = rows.Where(row => row.IsAutonomous);

        var take = query.Take is > 0 and <= MaxTake ? query.Take : MaxTake;

        var page = await rows
            .OrderByDescending(row => row.OccurredAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return page
            .Select(row => new AgentActivity(
                row.ActivityId,
                row.AgentKey,
                (AgentTrigger)row.Trigger,
                row.ActorEmail,
                row.IsAutonomous,
                row.Action,
                (AgentOutcome)row.Outcome,
                row.Summary,
                row.ConversationId,
                row.ProjectId,
                row.RecordReference,
                row.Route,
                row.ToolsUsed,
                row.DurationMs,
                row.InputTokens,
                row.OutputTokens,
                row.CostPence,
                row.OccurredAt))
            .ToList();
    }
}
