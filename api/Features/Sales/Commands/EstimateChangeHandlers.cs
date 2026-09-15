using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

// Estimates on a lead (2026-09-15): the edits and the moves along the ladder. Opening and
// reading one are EstimateHandlers.

public sealed class UpdateEstimateDetailsHandler : ICommandHandler<UpdateEstimateDetails, LeadEstimate>
{
    private readonly JpmsContext context;
    public UpdateEstimateDetailsHandler(JpmsContext context) { this.context = context; }

    public async Task<LeadEstimate> HandleAsync(UpdateEstimateDetails command, CancellationToken cancellationToken)
    {
        var entity = await EstimateLookup.FindAsync(context.LeadEstimates, command.EstimateId, cancellationToken)
            ?? throw new InvalidOperationException($"Estimate {command.EstimateId} not found.");
        var status = (EstimateStatus)entity.Status;
        if (!status.IsOpen())
            throw new InvalidOperationException($"{entity.Reference} is {status.DisplayName()} — it is history and cannot be edited.");

        entity.Scope = command.Scope.Trim();
        entity.ArchitectName = command.ArchitectName.Trim();
        entity.PriceDueOn = command.PriceDueOn;
        entity.BudgetMentioned = command.BudgetMentioned;
        entity.Total = command.Total;
        entity.Notes = command.Notes.Trim();
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public sealed class MoveEstimateStatusHandler : ICommandHandler<MoveEstimateStatus, LeadEstimate>
{
    private readonly JpmsContext context;
    public MoveEstimateStatusHandler(JpmsContext context) { this.context = context; }

    public async Task<LeadEstimate> HandleAsync(MoveEstimateStatus command, CancellationToken cancellationToken)
    {
        var entity = await EstimateLookup.FindAsync(context.LeadEstimates, command.EstimateId, cancellationToken)
            ?? throw new InvalidOperationException($"Estimate {command.EstimateId} not found.");
        var from = (EstimateStatus)entity.Status;
        if (command.Status == EstimateStatus.Submitted && entity.Total is null)
            throw new InvalidOperationException($"{entity.Reference} has no total yet — price it before it goes to the prospect.");

        var now = DateTimeOffset.UtcNow;
        entity.Status = (int)command.Status;
        entity.StatusChangedAt = now;
        if (command.Status == EstimateStatus.Submitted) entity.SubmittedAt = now;

        EstimateTimeline.Write(context, entity, SummaryFor(entity.Reference, from, command), command.ChangedByEmail, now);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private static string SummaryFor(string reference, EstimateStatus from, MoveEstimateStatus command)
    {
        var move = from == command.Status
            ? $"Estimate {reference} {command.Status.DisplayName()}"
            : $"Estimate {reference} {from.DisplayName()} → {command.Status.DisplayName()}";
        return string.IsNullOrWhiteSpace(command.Note) ? move : $"{move}. {command.Note.Trim()}";
    }
}
