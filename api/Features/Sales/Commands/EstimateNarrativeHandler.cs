using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

/// <summary>Writes the estimate's client-facing narrative (2026-09-18): the three texts the
/// document prints and nothing else, so a details edit can never blank them and a narrative save
/// can never revert the details.</summary>
public sealed class SetEstimateNarrativeHandler : ICommandHandler<SetEstimateNarrative, LeadEstimate>
{
    private readonly JpmsContext context;
    public SetEstimateNarrativeHandler(JpmsContext context) { this.context = context; }

    public async Task<LeadEstimate> HandleAsync(SetEstimateNarrative command, CancellationToken cancellationToken)
    {
        var entity = await EstimateLookup.FindAsync(context.LeadEstimates, command.EstimateId, cancellationToken)
            ?? throw new InvalidOperationException($"Estimate {command.EstimateId} not found.");
        var status = (EstimateStatus)entity.Status;
        if (!status.IsOpen())
            throw new InvalidOperationException($"{entity.Reference} is {status.DisplayName()} — it is history and its document text cannot be changed.");

        // All three together: a text left out of the body arrives null and clears its field —
        // the whole point of the command is that it names the three and nothing else.
        entity.ExecutiveSummary = (command.ExecutiveSummary ?? "").Trim();
        entity.BuildTime = (command.BuildTime ?? "").Trim();
        entity.Exclusions = (command.Exclusions ?? "").Trim();
        await context.SaveChangesAsync(cancellationToken);

        var lines = await context.LeadEstimateLines.AsNoTracking()
            .Where(line => line.EstimateId == entity.EstimateId).ToListAsync(cancellationToken);
        return entity.ToModel(lines);
    }
}
