using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Procurement;
using System.Globalization;
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
        // The total is the breakdown's sum while the breakdown has lines — a typed total cannot
        // contradict it; without lines the typed total stands.
        var lines = await context.LeadEstimateLines.AsNoTracking()
            .Where(line => line.EstimateId == entity.EstimateId).ToListAsync(cancellationToken);
        entity.Total = lines.Count > 0 ? lines.Sum(line => line.Total) : command.Total;
        entity.Notes = command.Notes.Trim();
        entity.ExecutiveSummary = (command.ExecutiveSummary ?? "").Trim();
        entity.BuildTime = (command.BuildTime ?? "").Trim();
        entity.Exclusions = (command.Exclusions ?? "").Trim();
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(lines);
    }
}

public sealed class SetEstimateBreakdownHandler : ICommandHandler<SetEstimateBreakdown, LeadEstimate>
{
    private static readonly CultureInfo Sterling = CultureInfo.GetCultureInfo("en-GB");
    private readonly JpmsContext context;
    public SetEstimateBreakdownHandler(JpmsContext context) { this.context = context; }

    public async Task<LeadEstimate> HandleAsync(SetEstimateBreakdown command, CancellationToken cancellationToken)
    {
        var entity = await EstimateLookup.FindAsync(context.LeadEstimates, command.EstimateId, cancellationToken)
            ?? throw new InvalidOperationException($"Estimate {command.EstimateId} not found.");
        var status = (EstimateStatus)entity.Status;
        if (!status.IsOpen())
            throw new InvalidOperationException($"{entity.Reference} is {status.DisplayName()} — it is history and cannot be re-priced; open a new estimate.");

        // Every cost code given must be in the cost-centre master (the same guard as tendered
        // line items); a blank code is allowed — the estimator picks it later.
        var codes = command.Sections.SelectMany(section => section.Lines)
            .Select(line => (line.CostCode ?? "").Trim())
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        await context.EnsureCostCodesInMasterAsync(codes, cancellationToken);

        // Replaced whole: the lines are the breakdown as written, nothing survives from before.
        context.LeadEstimateLines.RemoveRange(
            await context.LeadEstimateLines.Where(line => line.EstimateId == entity.EstimateId).ToListAsync(cancellationToken));
        var rows = new List<LeadEstimateLineEntity>();
        var sectionOrder = 0;
        foreach (var section in command.Sections)
        {
            var sortOrder = 0;
            foreach (var line in section.Lines)
            {
                rows.Add(new LeadEstimateLineEntity
                {
                    LineId = Guid.NewGuid().ToString("N"),
                    EstimateId = entity.EstimateId,
                    Section = section.Name.Trim(),
                    SectionOrder = sectionOrder,
                    SectionProvisional = section.Provisional,
                    CostCode = (line.CostCode ?? "").Trim(),
                    Description = line.Description.Trim(),
                    Quantity = line.Quantity,
                    Unit = (line.Unit ?? "").Trim(),
                    UnitPrice = line.UnitPrice,
                    Total = line.Total,
                    SortOrder = sortOrder++
                });
            }
            sectionOrder++;
        }
        context.LeadEstimateLines.AddRange(rows);
        if (rows.Count > 0) entity.Total = rows.Sum(row => row.Total);

        var now = DateTimeOffset.UtcNow;
        var summary = rows.Count == 0
            ? $"Estimate {entity.Reference} breakdown cleared"
            : $"Estimate {entity.Reference} breakdown set — {command.Sections.Count(section => section.Lines.Count > 0)} section{(command.Sections.Count == 1 ? "" : "s")}, "
              + $"{rows.Count} line{(rows.Count == 1 ? "" : "s")}, total {entity.Total?.ToString("C0", Sterling)}";
        EstimateTimeline.Write(context, entity, summary, command.ChangedByEmail, now);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(rows);
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
        var lines = await context.LeadEstimateLines.AsNoTracking()
            .Where(line => line.EstimateId == entity.EstimateId).ToListAsync(cancellationToken);
        return entity.ToModel(lines);
    }

    private static string SummaryFor(string reference, EstimateStatus from, MoveEstimateStatus command)
    {
        var move = from == command.Status
            ? $"Estimate {reference} {command.Status.DisplayName()}"
            : $"Estimate {reference} {from.DisplayName()} → {command.Status.DisplayName()}";
        return string.IsNullOrWhiteSpace(command.Note) ? move : $"{move}. {command.Note.Trim()}";
    }
}
