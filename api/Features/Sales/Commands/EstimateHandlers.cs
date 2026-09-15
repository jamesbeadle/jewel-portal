using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

// Estimates on a lead (2026-09-15): opening one and reading one. The moves and the edits are
// EstimateChangeHandlers. Business refusals throw InvalidOperationException, which the endpoints
// read back as 400 (the connector shows the message as-is).

public sealed class CreateEstimateHandler : ICommandHandler<CreateEstimate, LeadEstimate>
{
    private readonly JpmsContext context;
    public CreateEstimateHandler(JpmsContext context) { this.context = context; }

    public async Task<LeadEstimate> HandleAsync(CreateEstimate command, CancellationToken cancellationToken)
    {
        if (!await context.Leads.AnyAsync(lead => lead.LeadId == command.LeadId, cancellationToken))
            throw new InvalidOperationException($"Lead {command.LeadId} not found.");

        // Global sequence like the lead's own number: max + 1, never a row count, so a deleted
        // row never re-issues a reference.
        var nextNumber = (await context.LeadEstimates.MaxAsync(row => (int?)row.Number, cancellationToken) ?? 0) + 1;
        var now = DateTimeOffset.UtcNow;
        var entity = new LeadEstimateEntity
        {
            EstimateId = Guid.NewGuid().ToString("N"),
            LeadId = command.LeadId,
            Number = nextNumber,
            Scope = command.Scope.Trim(),
            ArchitectName = command.ArchitectName.Trim(),
            PriceDueOn = command.PriceDueOn,
            BudgetMentioned = command.BudgetMentioned,
            Total = command.Total,
            Notes = command.Notes.Trim(),
            Status = (int)EstimateStatus.Received,
            StatusChangedAt = now,
            CreatedByEmail = command.CreatedByEmail,
            CreatedAt = now
        };
        context.LeadEstimates.Add(entity);
        EstimateTimeline.Write(context, entity, $"Estimate {entity.Reference} opened", command.CreatedByEmail, now);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(Array.Empty<LeadEstimateLineEntity>());
    }
}

public sealed class GetEstimateHandler : IQueryHandler<GetEstimate, LeadEstimate?>
{
    private readonly JpmsContext context;
    public GetEstimateHandler(JpmsContext context) { this.context = context; }

    public async Task<LeadEstimate?> HandleAsync(GetEstimate query, CancellationToken cancellationToken)
    {
        var entity = await EstimateLookup.FindAsync(context.LeadEstimates.AsNoTracking(), query.EstimateId, cancellationToken);
        if (entity is null) return null;
        var lines = await context.LeadEstimateLines.AsNoTracking()
            .Where(line => line.EstimateId == entity.EstimateId).ToListAsync(cancellationToken);
        return entity.ToModel(lines);
    }
}

// One estimate by its id or by its EST-#### reference (a bare number reads as the reference too).
internal static class EstimateLookup
{
    public static async Task<LeadEstimateEntity?> FindAsync(IQueryable<LeadEstimateEntity> estimates, string key, CancellationToken cancellationToken)
    {
        var trimmed = key.Trim();
        var byId = await estimates.FirstOrDefaultAsync(row => row.EstimateId == trimmed, cancellationToken);
        if (byId is not null) return byId;
        if (!TryReadNumber(trimmed, out var number)) return null;
        return await estimates.FirstOrDefaultAsync(row => row.Number == number, cancellationToken);
    }

    private static bool TryReadNumber(string key, out int number)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            key, "^(?:EST[-\\s]?)?0*(\\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        number = 0;
        return match.Success && int.TryParse(match.Groups[1].Value, out number) && number > 0;
    }
}

// Every estimate event lands on the lead's timeline as an Estimate activity.
internal static class EstimateTimeline
{
    public static void Write(JpmsContext context, LeadEstimateEntity estimate, string summary, string byEmail, DateTimeOffset at) =>
        context.LeadActivities.Add(new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = estimate.LeadId,
            Kind = (int)LeadActivityKind.Estimate,
            Summary = summary,
            OccurredAt = at,
            RecordedByEmail = byEmail
        });
}
