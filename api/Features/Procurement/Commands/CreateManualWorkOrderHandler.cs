using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Raises a work order directly, outside the tendering flow. Released immediately with
/// the next sequential per-project number (shared with awarded / variation / seeded
/// orders, so paperwork cross-references hold) — unless the command says SaveAsDraft,
/// in which case the order sits unnumbered (Number 0) at status Draft and the number
/// is only minted when ApproveWorkOrder fires, so a rejected or abandoned draft never
/// leaves a gap in the sequence. The order's value is the sum of its lines; each line
/// lands on a master cost centre so every allocation view can place it.
/// </summary>
public sealed class CreateManualWorkOrderHandler
    : ICommandHandler<CreateManualWorkOrder, WorkOrder>
{
    private readonly JpmsContext context;

    public CreateManualWorkOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<WorkOrder> HandleAsync(CreateManualWorkOrder command, CancellationToken cancellationToken)
    {
        var project = await context.Projects.FindAsync(new object[] { command.ProjectId }, cancellationToken);
        if (project is null) throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var subcontractor = await context.Subcontractors.FindAsync(new object[] { command.SubcontractorId }, cancellationToken);
        if (subcontractor is null) throw new InvalidOperationException("Choose a subcontractor from the directory.");

        // Every line must land on a centre in the master list, so the order can't scatter
        // committed value onto codes the Financials tab treats as legacy.
        var masterCodes = await context.CostCenters
            .Select(centre => centre.Code)
            .ToListAsync(cancellationToken);
        var masterSet = new HashSet<string>(masterCodes, StringComparer.OrdinalIgnoreCase);
        var unknown = command.Lines.Select(line => line.CostCode)
            .FirstOrDefault(code => !masterSet.Contains(code));
        if (unknown is not null)
            throw new InvalidOperationException($"Cost centre {unknown} is not in the cost-code master.");

        // Drafts sit unnumbered at 0 — ApproveWorkOrder mints the sequential number.
        var nextNumber = command.SaveAsDraft
            ? 0
            : (await context.WorkOrders
                .Where(order => order.ProjectId == command.ProjectId)
                .MaxAsync(order => (int?)order.Number, cancellationToken) ?? 0) + 1;

        var now = DateTimeOffset.UtcNow;
        var entity = new WorkOrderEntity
        {
            WorkOrderId = ProcurementIdentifierFactory.NextWorkOrderId(),
            ProjectId = command.ProjectId,
            BidPackageId = null,
            SubcontractorId = command.SubcontractorId,
            Value = command.Lines.Sum(line => line.Amount),
            Scope = command.Scope.Length > 4000 ? command.Scope[..4000] : command.Scope,
            AwardedAt = now,
            AwardedByEmail = command.RaisedByEmail,
            Number = nextNumber,
            Title = command.Title.Length > 256 ? command.Title[..256] : command.Title,
            Status = (int)(command.SaveAsDraft ? WorkOrderStatus.Draft : WorkOrderStatus.Released),
            CreatedAt = now,
            ProgrammeStart = command.ProgrammeStart,
            ScheduledCompletion = command.TargetCompletion,
            ProgrammeNotes = command.ProgrammeNotes.Length > 2000 ? command.ProgrammeNotes[..2000] : command.ProgrammeNotes,
            // Percent never survives without the flag — an untick clears it rather than leaving
            // a stale figure that would print the moment the box is ticked again.
            DepositRequired = command.DepositRequired,
            DepositPercent = command.DepositRequired ? command.DepositPercent : null
        };
        context.WorkOrders.Add(entity);

        var sortOrder = 0;
        foreach (var line in command.Lines)
        {
            // A measured line ("14 m2 @ £54.00") keeps its real quantity, unit and rate for the
            // PO's Qty/Unit and Unit Cost columns; anything else prints the long-standing
            // "1 item" at the line's amount. Quantity and UnitCost only count together — one
            // without the other cannot make a printable rate line.
            var isMeasured = line.Quantity is > 0m && line.UnitCost is not null;
            var unit = string.IsNullOrWhiteSpace(line.Unit) ? "item" : line.Unit.Trim();
            context.WorkOrderLines.Add(new WorkOrderLineEntity
            {
                WorkOrderLineId = ProcurementIdentifierFactory.NextWorkOrderLineId(),
                WorkOrderId = entity.WorkOrderId,
                Title = line.Title.Length > 256 ? line.Title[..256] : line.Title,
                Description = line.Description.Length > 1024 ? line.Description[..1024] : line.Description,
                CostType = "Subcontractor",
                CostCode = line.CostCode,
                Quantity = isMeasured ? line.Quantity!.Value : 1m,
                Unit = isMeasured ? (unit.Length > 32 ? unit[..32] : unit) : "item",
                UnitCost = isMeasured ? line.UnitCost!.Value : line.Amount,
                LineTotal = line.Amount,
                PaidToDate = 0m,
                SortOrder = sortOrder++
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
